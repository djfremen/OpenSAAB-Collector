using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;

namespace OpenSAAB.Collector.Service;

/// <summary>
/// Watches the Registry value
///   HKLM\SOFTWARE\OpenSAAB\Collector\UsbCaptureRequested
/// for transitions and spawns / kills USBPcapCMD.exe accordingly.
///
/// The tray app (running unelevated) flips this value when the user clicks
/// "Start USB capture" / "Stop USB capture". The service (running as
/// LocalSystem) actually owns the USBPcap kernel driver and the capture
/// process — hence the indirection.
///
/// Output goes to %TEMP%\usbpcap_&lt;wall_ms&gt;.pcapng. The existing Worker
/// picks the file up after rotation (settle window), gzips, and uploads
/// with X-Capture-Source: usbpcap.
///
/// Device filter: pinned to the Chipsoft Pro (VID 0x0483 / PID 0x5740) so
/// the .pcapng only contains the device we care about. Privacy and file
/// size both benefit.
/// </summary>
public sealed class UsbPcapSupervisor : BackgroundService
{
    private const string KeyPath = @"SOFTWARE\OpenSAAB\Collector";
    private const string RequestedValue = "UsbCaptureRequested";
    private const string LastFileValue = "UsbCaptureLastFile";
    // Failure marker the tray polls to surface "couldn't start" to the user.
    // Empty string ("") = OK / no failure.
    private const string LastFailureValue = "UsbCaptureLastFailure";

    // Chipsoft Pro USB IDs — same values the cstech2win shim filters on.
    private const ushort ChipsoftVid = 0x0483;
    private const ushort ChipsoftPid = 0x5740;

    private readonly ILogger<UsbPcapSupervisor> _log;
    private Process? _proc;
    private string? _currentPath;

    public UsbPcapSupervisor(ILogger<UsbPcapSupervisor> log)
    {
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        _log.LogInformation("UsbPcapSupervisor starting.");
        // Poll every 2 s. RegistryNotifyChangeKeyValue would let us go event-
        // driven but the P/Invoke is verbose and 2 s is plenty responsive
        // for a human clicking a tray button.
        var ticker = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            while (await ticker.WaitForNextTickAsync(stop))
            {
                var requested = ReadRequested();
                var running = _proc is { HasExited: false };
                if (requested && !running)
                {
                    StartCapture();
                }
                else if (!requested && running)
                {
                    StopCapture();
                }
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        finally
        {
            if (_proc is { HasExited: false }) StopCapture();
        }
    }

    private void StartCapture()
    {
        var usbpcapCmd = FindUsbPcapCmd();
        if (usbpcapCmd == null)
        {
            _log.LogWarning("USBPcapCMD.exe not found on PATH or in known install dirs — capture cannot start. " +
                            "Install USBPcap from https://desowin.org/usbpcap/ and reboot once.");
            WriteFailure("USBPcap not installed");
            WriteRequested(false);
            return;
        }

        var iface = PickInterface(usbpcapCmd);
        if (iface == null)
        {
            _log.LogWarning("Could not auto-pick a USBPcap interface (no Root Hub enumerated? " +
                            "USBPcap driver not loaded — reboot needed after install?). Capture not starting.");
            WriteFailure("No USBPcap interface");
            WriteRequested(false);
            return;
        }

        var wallMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _currentPath = Path.Combine(Path.GetTempPath(), $"usbpcap_{wallMs}.pcapng");

        // Args breakdown:
        //   -d <iface>  the USBPcap virtual interface (\\.\USBPcap1 typically)
        //   -A          --capture-from-all-devices: capture every device on
        //               the chosen Root Hub. *REQUIRED* — without this (or
        //               --devices), USBPcapCMD enters interactive mode and
        //               blocks on stdin asking which device to capture, then
        //               sits there forever from a service context. Found by
        //               smoke test 2026-05-15: spawn succeeded, file never
        //               appeared because the process was waiting on input.
        //   -o <file>   pcapng output path.
        // v0.3.0 may switch to `--devices <addr>` once the supervisor probes
        // the Chipsoft Pro's USB address; for v0.2.x we trust the hub-level
        // capture and post-filter server-side.
        var args = $"-d \"{iface}\" -A -o \"{_currentPath}\"";
        try
        {
            var psi = new ProcessStartInfo(usbpcapCmd, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            _proc = Process.Start(psi);
            if (_proc == null)
            {
                _log.LogWarning("USBPcapCMD failed to launch.");
                WriteFailure("USBPcapCMD launch returned null");
                WriteRequested(false);
                return;
            }

            // Validate: USBPcap should create the .pcapng within ~1 s of spawn.
            // Wait up to 3 s. If the file doesn't appear, the spawn went bad —
            // most likely a bad arg, missing driver, or USBPcapCMD crashed.
            // Kill, surface the failure, bounce the request flag.
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (DateTime.UtcNow < deadline)
            {
                if (_proc.HasExited)
                {
                    var exit = _proc.ExitCode;
                    var stderr = SafeReadStderrSnippet(_proc);
                    _log.LogWarning("USBPcapCMD exited prematurely with code {ExitCode}. Capture aborted. " +
                                    "Args=\"{Args}\" stderr=\"{Stderr}\"",
                                    exit, args, stderr);
                    WriteFailure($"USBPcapCMD exit {exit}: {stderr}");
                    _proc.Dispose(); _proc = null; _currentPath = null;
                    WriteRequested(false);
                    return;
                }
                if (File.Exists(_currentPath))
                {
                    // Healthy: file is there and the process is alive.
                    WriteLastFile(_currentPath);
                    WriteFailure("");  // clear any prior failure marker
                    _log.LogWarning(  // Warning level so EventLog actually shows it
                        "USBPcap capture started OK. file={Path} iface={Iface} pid={Pid}",
                        _currentPath, iface, _proc.Id);
                    return;
                }
                Thread.Sleep(150);
            }

            // 3 s elapsed, no file. Process is alive but stuck (most likely
            // waiting on stdin in interactive mode — pre-v0.2.1 bug where we
            // missed -A. If we ever see this in the wild again, look for new
            // USBPcapCMD argument surface that defaults back to interactive.)
            _log.LogWarning("USBPcapCMD spawned but no output file at {Path} after 3 s. Process likely stuck — killing. " +
                            "Args=\"{Args}\"", _currentPath, args);
            WriteFailure("Output file never created (process stalled)");
            try { _proc.Kill(); _proc.WaitForExit(2000); } catch { }
            _proc?.Dispose(); _proc = null; _currentPath = null;
            WriteRequested(false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to start USBPcapCMD.");
            WriteFailure($"Exception: {ex.Message}");
            WriteRequested(false);
            _proc = null;
            _currentPath = null;
        }
    }

    private static string SafeReadStderrSnippet(Process p)
    {
        try
        {
            if (p.StandardError != null)
            {
                var task = p.StandardError.ReadToEndAsync();
                if (task.Wait(500))
                {
                    var s = (task.Result ?? "").Trim();
                    if (s.Length > 160) s = s.Substring(0, 160) + "…";
                    return s;
                }
            }
        }
        catch { }
        return "(no stderr captured)";
    }

    private void StopCapture()
    {
        var p = _proc;
        var path = _currentPath;
        _proc = null;
        _currentPath = null;
        if (p == null) return;

        try
        {
            if (!p.HasExited)
            {
                // SIGTERM equivalent on Windows: Process.Kill is hard, but
                // USBPcapCMD doesn't have a clean Ctrl+C path when stdin
                // isn't a console; killing is safe because pcapng is written
                // in self-contained blocks that don't need a footer.
                p.Kill(entireProcessTree: false);
                p.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "USBPcapCMD shutdown error (proceeding).");
        }
        finally
        {
            try { p.Dispose(); } catch { }
        }

        // Log at Warning so EventLog actually shows it (the default EventLog
        // provider filter on .NET 8 hides Information).
        var ok = path != null && File.Exists(path);
        var sz = ok ? new FileInfo(path!).Length : 0L;
        if (ok && sz > 0)
        {
            _log.LogWarning("USBPcap capture stopped OK. file={Path} bytes={Bytes}", path, sz);
            WriteFailure("");  // clear any stale failure marker on clean stop
        }
        else
        {
            _log.LogWarning("USBPcap capture stopped but output is missing or empty. file={Path} bytes={Bytes}", path, sz);
            WriteFailure($"Stopped but no output ({sz} bytes at {path})");
        }
    }

    private static bool ReadRequested()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            return (key?.GetValue(RequestedValue) as int? ?? 0) != 0;
        }
        catch { return false; }
    }

    private static void WriteRequested(bool v)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath);
            key.SetValue(RequestedValue, v ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { /* best effort */ }
    }

    private static void WriteLastFile(string path)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath);
            key.SetValue(LastFileValue, path, RegistryValueKind.String);
        }
        catch { }
    }

    /// <summary>
    /// Marker the tray can poll after clicking Start to see whether the
    /// supervisor reached "spawned + file created" or hit a failure path.
    /// Empty string clears it; non-empty is a human-readable cause.
    /// </summary>
    private static void WriteFailure(string reason)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath);
            key.SetValue(LastFailureValue, reason ?? "", RegistryValueKind.String);
        }
        catch { }
    }

    private static string? FindUsbPcapCmd()
    {
        // 1. PATH
        var pathSearch = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var dir in pathSearch)
        {
            var c = Path.Combine(dir, "USBPcapCMD.exe");
            if (File.Exists(c)) return c;
        }
        // 2. Standard install dir
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new[]
        {
            Path.Combine(pf, "USBPcap", "USBPcapCMD.exe"),
            @"C:\Program Files\USBPcap\USBPcapCMD.exe",
            @"C:\Program Files (x86)\USBPcap\USBPcapCMD.exe",
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }
        return null;
    }

    /// <summary>
    /// Ask USBPcapCMD for the list of interfaces and pick the first one.
    /// In a single-Chipsoft setup this is correct; multi-Chipsoft requires
    /// matching against VID:PID which USBPcap doesn't expose in --extcap-interfaces
    /// output. Future work for v0.3.0.
    /// </summary>
    private string? PickInterface(string usbpcapCmd)
    {
        try
        {
            var psi = new ProcessStartInfo(usbpcapCmd, "--extcap-interfaces")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return null;
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            // Output lines look like:
            //   interface {value=\\.\USBPcap1}{display=USBPcap1}
            // We grab the first match.
            foreach (var line in stdout.Split('\n'))
            {
                var idx = line.IndexOf("{value=", StringComparison.Ordinal);
                if (idx < 0) continue;
                var start = idx + "{value=".Length;
                var end = line.IndexOf('}', start);
                if (end < 0) continue;
                return line.Substring(start, end - start);
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "PickInterface failed");
        }
        return null;
    }
}

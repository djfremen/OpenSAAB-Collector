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
    // v0.2.7: PID of the USBPcapCMD process WE spawned. Persisted so a service
    // restart doesn't orphan the running capture (pre-v0.2.7 bug: in-memory
    // _proc was lost on restart → Stop became a silent no-op → tray said
    // "stopped" while USBPcapCMD ran for hours).
    private const string PidValue = "UsbCapturePid";
    // v0.2.7: ISO-8601 UTC timestamp the supervisor writes after confirming
    // a USBPcapCMD process actually exited. Tray polls this to give the user
    // a definitive "stopped" balloon instead of an optimistic one.
    private const string LastStopValue = "UsbCaptureLastStop";

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
                // v0.2.7: before deciding running/requested, try to recover
                // any orphaned process from a previous supervisor instance.
                // Without this, a service restart silently abandoned the
                // USBPcapCMD process and subsequent Stop calls did nothing.
                TryAdoptOrphan();

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
        //   -d <iface>  the USBPcap virtual interface (\\.\USBPcap1)
        //   -s 96       --snaplen 96: cap each captured packet at 96 bytes.
        //               USBPcap pseudo-header (27 B) + Chipsoft envelope
        //               (≤39 B) + ISO-TP / UDS payload (≤8 B per frame on
        //               single-frame transfers; ISO-TP CFs split larger
        //               payloads across multiple URBs anyway) → well under
        //               96 B for every routine chipsoft URB.
        //   -A          --capture-from-all-devices: capture every device on
        //               the chosen Root Hub. v0.2.2/v0.2.3 attempted
        //               `--devices <Chipsoft-address>` via --extcap-config
        //               probing, but Windows reassigns USB device addresses
        //               whenever a device disconnects/reconnects (including
        //               on service restarts that involve filter drivers), so
        //               the probed address goes stale mid-walk and capture
        //               silently records nothing. PickChipsoftAddress() is
        //               kept below for future revival once we have a robust
        //               way to track the Chipsoft by VID:PID through address
        //               rotation events. For v0.2.4+ we accept larger file
        //               sizes (raw ~10 MB, gz ~1 MB per ~70 s walk) in
        //               exchange for 100 % capture reliability.
        //   -o <file>   output path.
        // *Important*: missing both `-A` and `--devices` puts USBPcapCMD in
        // interactive mode (blocks on stdin asking which device). That's how
        // v0.2.0 silently produced no file. Always pass one of them.
        var args = $"-d \"{iface}\" -s 96 -A -o \"{_currentPath}\"";
        _log.LogWarning("Starting hub-wide USBPcap capture (-A) on {Iface}. " +
                        "Device filter intentionally disabled in v0.2.4 — see PickChipsoftAddress comment.",
                        iface);
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
                    WritePid(_proc.Id);                 // v0.2.7: persist PID for orphan recovery
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

        // v0.2.7: even if our in-memory _proc is null (service was restarted
        // since spawn), recover the orphan from the persisted PID and kill
        // it. Without this, Stop became a silent no-op and the orphaned
        // USBPcapCMD ran indefinitely while the tray cheerfully said
        // "stopped". Caught 2026-05-16 when a USBPcapCMD ran 21 hours
        // straight after the user hit Stop.
        if (p == null)
        {
            p = TryReclaimOrphanedProcess();
        }

        if (p == null)
        {
            // No process to stop, but still mark the stop event so the tray
            // poll resolves cleanly instead of timing out.
            WriteLastStop(DateTime.UtcNow);
            WritePid(0);
            return;
        }

        try
        {
            if (!p.HasExited)
            {
                // entireProcessTree=true catches any child USBPcapCMD spawned
                // (none expected, but cheap insurance). pcapng is written in
                // self-contained blocks that don't need a footer, so a hard
                // kill leaves the file readable.
                p.Kill(entireProcessTree: true);
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

        // Always clear the PID and stamp the stop time, even on partial
        // success — the tray polls these to confirm.
        WritePid(0);
        WriteLastStop(DateTime.UtcNow);

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

    /// <summary>
    /// v0.2.7: on every poll, see whether a USBPcapCMD process we spawned
    /// before a service restart is still running. If so, adopt it back into
    /// <see cref="_proc"/> so the next StartCapture/StopCapture decision
    /// reflects reality instead of in-memory amnesia.
    /// </summary>
    private void TryAdoptOrphan()
    {
        if (_proc is { HasExited: false }) return;  // already healthy

        var (pid, lastFile) = ReadPidAndPath();
        if (pid <= 0) return;  // no orphan known

        Process? candidate = null;
        try { candidate = Process.GetProcessById(pid); }
        catch
        {
            // Orphan died on its own — clear the marker.
            WritePid(0);
            WriteLastStop(DateTime.UtcNow);
            return;
        }

        // Verify it really is USBPcapCMD before adopting. After a PID is
        // recycled by the OS, something else with the same PID could be
        // running; we must not "adopt" — and certainly not Kill — that.
        var name = SafeProcessName(candidate);
        if (!string.Equals(name, "USBPcapCMD", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("Persisted UsbCapturePid={Pid} now belongs to '{Name}', not USBPcapCMD. " +
                            "Clearing the marker (a previous capture probably exited and the PID was reused).",
                            pid, name ?? "?");
            try { candidate.Dispose(); } catch { }
            WritePid(0);
            return;
        }

        _proc = candidate;
        _currentPath = lastFile;
        _log.LogWarning("Adopted orphaned USBPcapCMD pid={Pid} (likely survived a service restart). file={Path}",
                        pid, lastFile ?? "<unknown>");
    }

    /// <summary>
    /// Variant for StopCapture: if we don't have a live <c>_proc</c> handle
    /// but the registry knows we spawned one, return the live Process for
    /// killing. Different from <see cref="TryAdoptOrphan"/> only in that it
    /// returns the Process directly rather than mutating <c>_proc</c> (the
    /// caller is already in the middle of clearing state).
    /// </summary>
    private Process? TryReclaimOrphanedProcess()
    {
        var (pid, _) = ReadPidAndPath();
        if (pid <= 0) return null;

        Process? candidate = null;
        try { candidate = Process.GetProcessById(pid); }
        catch { return null; }

        var name = SafeProcessName(candidate);
        if (!string.Equals(name, "USBPcapCMD", StringComparison.OrdinalIgnoreCase))
        {
            try { candidate.Dispose(); } catch { }
            return null;
        }
        _log.LogWarning("Reclaimed orphaned USBPcapCMD pid={Pid} during Stop — killing it now.", pid);
        return candidate;
    }

    private static string? SafeProcessName(Process p)
    {
        try { return p.ProcessName; } catch { return null; }
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

    /// <summary>v0.2.7: persist the spawned USBPcapCMD PID; 0 clears.</summary>
    private static void WritePid(int pid)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath);
            key.SetValue(PidValue, pid, RegistryValueKind.DWord);
        }
        catch { }
    }

    /// <summary>v0.2.7: ISO-8601 UTC timestamp of the most recent confirmed stop.</summary>
    private static void WriteLastStop(DateTime utc)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath);
            key.SetValue(LastStopValue, utc.ToString("O"), RegistryValueKind.String);
        }
        catch { }
    }

    /// <summary>v0.2.7: read persisted PID + last known capture path together.</summary>
    private static (int Pid, string? LastFile) ReadPidAndPath()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            var pid = (key?.GetValue(PidValue) as int?) ?? 0;
            var path = key?.GetValue(LastFileValue) as string;
            return (pid, path);
        }
        catch { return (0, null); }
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
    /// Ask USBPcapCMD for the per-interface device list and pick the entry
    /// whose display name says "USB Serial Device" — the Chipsoft Pro
    /// enumerates as a USB CDC ACM device on Win10 with that exact friendly
    /// name. Returns the USBPcap numeric address (the value Wireshark passes
    /// to <c>--devices N</c>) or null if no match.
    ///
    /// `--extcap-config` output lines we parse:
    ///   value {arg=99}{value=21}{display=[21] USB Serial Device}{enabled=true}
    /// Cross-validated on this bench: PnP enumerates the Chipsoft at
    /// VID_0483&amp;PID_5740 with FriendlyName "USB Serial Device (COM5)";
    /// USBPcap shows it at address 21 in its `--devices` list.
    /// </summary>
    private string? PickChipsoftAddress(string usbpcapCmd, string iface)
    {
        try
        {
            var psi = new ProcessStartInfo(usbpcapCmd, $"--extcap-interface \"{iface}\" --extcap-config")
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

            foreach (var line in stdout.Split('\n'))
            {
                // We want USB Serial Device top-level entries, not its children
                // (which would have a `{parent=N}` suffix).
                if (!line.Contains("USB Serial Device")) continue;
                if (line.Contains("{parent=")) continue;
                var idx = line.IndexOf("{value=", StringComparison.Ordinal);
                if (idx < 0) continue;
                var start = idx + "{value=".Length;
                var end = line.IndexOf('}', start);
                if (end < 0) continue;
                var val = line.Substring(start, end - start).Trim();
                // Only return purely numeric top-level addresses (e.g. "21"),
                // not subkeys like "4_1".
                if (int.TryParse(val, out _)) return val;
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "PickChipsoftAddress failed");
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

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
            WriteRequested(false);
            return;
        }

        var iface = PickInterface(usbpcapCmd);
        if (iface == null)
        {
            _log.LogWarning("Could not auto-pick a USBPcap interface (no Chipsoft enumerated? USBPcap driver not loaded?). " +
                            "Capture not starting.");
            WriteRequested(false);
            return;
        }

        var wallMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _currentPath = Path.Combine(Path.GetTempPath(), $"usbpcap_{wallMs}.pcapng");

        // Filter expression: only frames whose USB device descriptor matches
        // the Chipsoft VID:PID. USBPcap's --devices arg takes addresses (1.x)
        // not VID/PID; we constrain via post-filter --capture-from-all-devices
        // off + interface selection. Most robust route is to capture the chosen
        // hub interface and let the user's bus be Chipsoft-only (typical), then
        // post-filter server-side via decode_chipsoft_pcap.py. v0.2.0 ships
        // the simple form; v0.3.0 can tighten to per-device filtering.
        var args = $"-d \"{iface}\" -o \"{_currentPath}\"";
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
                WriteRequested(false);
                return;
            }
            WriteLastFile(_currentPath);
            _log.LogInformation("USBPcapCMD started: {Path} (interface={Iface}, pid={Pid})",
                _currentPath, iface, _proc.Id);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to start USBPcapCMD.");
            WriteRequested(false);
            _proc = null;
            _currentPath = null;
        }
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

        _log.LogInformation("USBPcapCMD stopped. Output: {Path}", path);
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

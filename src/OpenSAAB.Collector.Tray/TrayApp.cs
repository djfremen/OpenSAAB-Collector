using System.Diagnostics;
using Microsoft.Win32;

namespace OpenSAAB.Collector.Tray;

/// <summary>
/// Tray-icon app for the OpenSAAB Collector.
///
/// Menu:
///   - Header: "Captures uploaded: N" (read-only)
///   - Toggle "Upload enabled"
///   - "Open live console" (raw color-coded tail)
///   - "Upload pending logs now" (bypasses the 30 s settle in the service)
///   - "Open log folder"
///   - "Show install GUID"
///   - "About"
///   - "Exit (service keeps running)"
///
/// Decoded console / scapy decoder removed in v0.1.4 — decoding happens
/// server-side on the uploaded logs. The mission is reliable capture + ship.
/// </summary>
internal sealed class TrayApp : ApplicationContext
{
    private const string KeyPath = @"SOFTWARE\OpenSAAB\Collector";
    private const string ServiceName = "OpenSAABCollector";

    private static string AppVersion =>
        typeof(TrayApp).Assembly.GetName().Version?.ToString(3) ?? "?";

    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _versionHeader;
    private readonly ToolStripMenuItem _countHeader;
    private readonly ToolStripMenuItem _toggleUpload;
    private readonly ToolStripMenuItem _startUsbCapture;
    private readonly ToolStripMenuItem _stopUsbCapture;
    private readonly ContextMenuStrip _menu;
    private LogConsoleForm? _consoleForm;

    public TrayApp()
    {
        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => { RefreshCountHeader(); RefreshUsbCaptureItems(); };

        _versionHeader = new ToolStripMenuItem($"OpenSAAB Collector  v{AppVersion}")
        {
            Enabled = false,
        };
        _menu.Items.Add(_versionHeader);

        _countHeader = new ToolStripMenuItem(FormatCountHeader(ReadUploadCount()))
        {
            Enabled = false,
        };
        _menu.Items.Add(_countHeader);
        _menu.Items.Add(new ToolStripSeparator());

        _toggleUpload = new ToolStripMenuItem("Upload enabled", null, OnToggleUpload)
        {
            CheckOnClick = true,
            Checked = ReadUploadEnabled(),
        };
        _menu.Items.Add(_toggleUpload);
        _menu.Items.Add(new ToolStripSeparator());

        // USB capture controls (v0.2.0) — write to Registry; the service's
        // UsbPcapSupervisor polls and spawns/kills USBPcapCMD.exe.
        _startUsbCapture = new ToolStripMenuItem("🟢 Start USB capture", null, (_, _) => SetUsbCapture(true));
        _stopUsbCapture = new ToolStripMenuItem("🔴 Stop USB capture",  null, (_, _) => SetUsbCapture(false));
        RefreshUsbCaptureItems();
        _menu.Items.Add(_startUsbCapture);
        _menu.Items.Add(_stopUsbCapture);
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add("Open live console…", null, (_, _) => OpenLiveConsole());
        _menu.Items.Add("Upload pending logs now", null, (_, _) => _ = UploadNowAsync());
        _menu.Items.Add("Open log folder", null, (_, _) =>
        {
            var path = Path.GetTempPath();
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        });
        _menu.Items.Add("Show install GUID", null, (_, _) =>
        {
            var id = ReadString("InstallId") ?? "(not set — service may not have started)";
            MessageBox.Show($"OpenSAAB Collector v{AppVersion}\nInstall ID:\n\n{id}\n\nKeep this private.",
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("About OpenSAAB", null, (_, _) =>
        {
            Process.Start(new ProcessStartInfo("https://opensaab.com") { UseShellExecute = true });
        });
        _menu.Items.Add("Exit tray (service keeps running)", null, (_, _) => DoExit());
        _menu.Items.Add("Stop service && exit (full shutdown)", null, (_, _) => DoStopAll());

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            ContextMenuStrip = _menu,
            Visible = true,
            Text = $"OpenSAAB Collector v{AppVersion}",
        };
        _icon.DoubleClick += (_, _) => _menu.Show(Cursor.Position);
    }

    private static string FormatCountHeader(int n) =>
        $"Captures uploaded: {n}";

    private void RefreshCountHeader() =>
        _countHeader.Text = FormatCountHeader(ReadUploadCount());

    private static System.Drawing.Icon LoadIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(path)) return new System.Drawing.Icon(path);
        }
        catch { /* fall through */ }
        return SystemIcons.Information;
    }

    private void OnToggleUpload(object? sender, EventArgs e)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath);
            key.SetValue("UploadEnabled", _toggleUpload.Checked ? 1 : 0, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to update setting (admin required?):\n\n" + ex.Message,
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _toggleUpload.Checked = ReadUploadEnabled();
        }
    }

    private void OpenLiveConsole()
    {
        if (_consoleForm == null || _consoleForm.IsDisposed)
        {
            _consoleForm = new LogConsoleForm();
            _consoleForm.Show();
        }
        else
        {
            if (_consoleForm.WindowState == FormWindowState.Minimized)
                _consoleForm.WindowState = FormWindowState.Normal;
            _consoleForm.Activate();
        }
    }

    private async Task UploadNowAsync()
    {
        if (!ReadUploadEnabled())
        {
            _icon.ShowBalloonTip(4000, "OpenSAAB Collector",
                "Upload is disabled. Tick \"Upload enabled\" in the menu first.",
                ToolTipIcon.Warning);
            return;
        }

        var candidates = ManualUploader.FindPendingLogs(Path.GetTempPath());
        if (candidates.Count == 0)
        {
            _icon.ShowBalloonTip(3000, "OpenSAAB Collector",
                "No pending shim logs found in %TEMP%.",
                ToolTipIcon.Info);
            return;
        }

        // Live progress dialog — v0.2.3+. With USBPcap captures landing as
        // multi-MB .pcapng files the user wants visible reassurance that
        // upload is making progress; balloon-only was opaque.
        var progress = new UploadProgressForm(candidates.Count);
        progress.Show();
        Application.DoEvents();

        int ok = 0, skipped = 0, fail = 0;
        try
        {
            var uploader = ManualUploader.FromRegistry();
            for (int i = 0; i < candidates.Count; i++)
            {
                var path = candidates[i];
                long sz = 0;
                try { sz = new FileInfo(path).Length; } catch { }
                progress.Tick(i, candidates.Count, path, sz);

                var result = await uploader.UploadOneAsync(path);
                switch (result)
                {
                    case ManualUploader.UploadResult.Uploaded:
                        ok++;
                        IncrementUploadCount();
                        break;
                    case ManualUploader.UploadResult.LowValueDeleted:
                        skipped++;
                        break;
                    default:
                        fail++;
                        break;
                }
                progress.Tick(i + 1, candidates.Count, path, sz);
            }
        }
        catch (Exception ex)
        {
            progress.Finish();
            _icon.ShowBalloonTip(5000, "OpenSAAB Collector",
                $"Upload error: {ex.Message}",
                ToolTipIcon.Error);
            return;
        }
        progress.Finish();

        RefreshCountHeader();
        var icon = fail == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning;
        var parts = new List<string> { $"Uploaded {ok} log(s)" };
        if (skipped > 0) parts.Add($"skipped {skipped} empty");
        if (fail > 0) parts.Add($"failed {fail}");
        var msg = string.Join(", ", parts) + $". Captures total: {ReadUploadCount()}.";
        _icon.ShowBalloonTip(4000, "OpenSAAB Collector", msg, icon);
    }

    /// <summary>
    /// USB-capture state lives in HKLM\SOFTWARE\OpenSAAB\Collector\UsbCaptureRequested.
    /// Tray writes it (UAC-prompted on first write because HKLM); service polls
    /// it from UsbPcapSupervisor and spawns / kills USBPcapCMD.exe accordingly.
    /// </summary>
    private void SetUsbCapture(bool start)
    {
        // On Start: surface "USBPcap not installed" to the user immediately
        // rather than silently failing service-side and bouncing the flag.
        if (start && !UsbPcapIsInstalled())
        {
            var r = MessageBox.Show(
                "USBPcap doesn't appear to be installed on this machine.\n\n" +
                "USB capture needs the USBPcap kernel driver. Install it from\n" +
                "https://desowin.org/usbpcap/  (free, ~3 MB, one-time reboot required).\n\n" +
                "Open the download page now?",
                "OpenSAAB Collector — USBPcap missing",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo("https://desowin.org/usbpcap/") { UseShellExecute = true });
            }
            return;
        }

        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath))
            {
                // Clear any prior failure marker before requesting; the service
                // sets it again if the new attempt fails.
                key.SetValue("UsbCaptureLastFailure", "", RegistryValueKind.String);
                key.SetValue("UsbCaptureRequested", start ? 1 : 0, RegistryValueKind.DWord);
            }
            RefreshUsbCaptureItems();

            if (start)
            {
                // Validate: the service polls every 2 s and on healthy spawn
                // writes UsbCaptureLastFile within ~3 s. Poll Registry for up
                // to 7 s and surface a precise outcome to the user.
                _ = Task.Run(() => PollUsbCaptureOutcome(starting: true));
            }
            else
            {
                _icon.ShowBalloonTip(3000, "OpenSAAB Collector",
                    "USB capture stopped. The .pcapng will upload after a 30 s settle.",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to update USB capture state (admin required for HKLM write?):\n\n" + ex.Message,
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// After flipping UsbCaptureRequested=1, watch Registry for the supervisor
    /// to confirm spawn-and-file-creation, OR to write a failure marker. Updates
    /// the tray balloon with a precise success or failure message.
    /// </summary>
    private void PollUsbCaptureOutcome(bool starting)
    {
        // 15 s budget is enough for: 2 s supervisor poll cycle, 1-2 s
        // `USBPcapCMD --extcap-config` device-list probe, ProcessStartInfo
        // spawn, plus 3 s file-creation validation. v0.2.1 used 7 s which
        // raced the device-pick probe and yielded false "status unknown"
        // balloons even when the capture started fine.
        var deadline = DateTime.UtcNow.AddSeconds(15);
        string? lastFile = null;
        string lastFailure = "";

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
                lastFile    = key?.GetValue("UsbCaptureLastFile")    as string;
                lastFailure = key?.GetValue("UsbCaptureLastFailure") as string ?? "";
            }
            catch { }

            if (!string.IsNullOrEmpty(lastFailure))
            {
                _icon.ShowBalloonTip(7000, "OpenSAAB Collector — USB capture failed",
                    $"Couldn't start USBPcap. Reason: {lastFailure}\n\n" +
                    "Check Event Viewer → Application → OpenSAABCollector for details.",
                    ToolTipIcon.Error);
                return;
            }
            // If the supervisor wrote a file path AND the file actually exists
            // AND UsbCaptureRequested is still 1 (didn't get bounced), the
            // capture is healthy.
            if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile))
            {
                _icon.ShowBalloonTip(4000, "OpenSAAB Collector",
                    $"USB capture running.\nFile: {Path.GetFileName(lastFile!)}",
                    ToolTipIcon.Info);
                return;
            }
            Thread.Sleep(500);
        }

        _icon.ShowBalloonTip(7000, "OpenSAAB Collector — USB capture status unknown",
            "The service didn't confirm capture start within 7 s. Check Event Viewer → Application → OpenSAABCollector.",
            ToolTipIcon.Warning);
    }

    private static bool UsbPcapIsInstalled()
    {
        // Same probe order the service uses (PATH + standard install dirs).
        var pathSearch = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var dir in pathSearch)
        {
            if (File.Exists(Path.Combine(dir, "USBPcapCMD.exe"))) return true;
        }
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new[]
        {
            Path.Combine(pf, "USBPcap", "USBPcapCMD.exe"),
            @"C:\Program Files\USBPcap\USBPcapCMD.exe",
            @"C:\Program Files (x86)\USBPcap\USBPcapCMD.exe",
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return true;
        }
        return false;
    }

    private static bool ReadUsbCaptureRequested()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            return (key?.GetValue("UsbCaptureRequested") as int? ?? 0) != 0;
        }
        catch { return false; }
    }

    private void RefreshUsbCaptureItems()
    {
        var running = ReadUsbCaptureRequested();
        _startUsbCapture.Enabled = !running;
        _stopUsbCapture.Enabled = running;
    }

    private static bool ReadUploadEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            return (key?.GetValue("UploadEnabled") as int? ?? 0) != 0;
        }
        catch { return false; }
    }

    private static int ReadUploadCount()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            return key?.GetValue("UploadCount") as int? ?? 0;
        }
        catch { return 0; }
    }

    private static void IncrementUploadCount()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                           ?? Registry.LocalMachine.CreateSubKey(KeyPath);
            var current = key.GetValue("UploadCount") as int? ?? 0;
            key.SetValue("UploadCount", current + 1, RegistryValueKind.DWord);
        }
        catch { /* tray runs unelevated; if write fails the service will still count */ }
    }

    private static string? ReadString(string name)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            return key?.GetValue(name) as string;
        }
        catch { return null; }
    }

    /// <summary>
    /// Tear down all visible state and force-terminate. ApplicationContext.ExitThread()
    /// only signals the message loop to exit; it doesn't kill background threads
    /// (HttpClient connection pools, FileSystemWatcher completion ports, in-flight
    /// upload Tasks, the LogConsoleForm's UI thread). When any of those are alive,
    /// the process stays in the background and the only recourse is Task Manager.
    /// "Exit" should mean exit, so we also call Environment.Exit(0).
    /// </summary>
    private void DoExit()
    {
        try { _consoleForm?.Close(); } catch { }
        try { _consoleForm?.Dispose(); } catch { }
        try { _icon.Visible = false; } catch { }   // remove from tray immediately
        try { _icon.Dispose(); } catch { }
        try { _menu.Dispose(); } catch { }
        Application.Exit();
        Environment.Exit(0);
    }

    /// <summary>
    /// "Stop service & exit (full shutdown)". Tray runs unelevated; sc.exe
    /// stop requires admin. Launch via Verb=runas so UAC prompts the user;
    /// if they decline, we still exit the tray (service stays running and
    /// they can stop it manually later from an admin shell).
    /// </summary>
    private void DoStopAll()
    {
        try
        {
            var psi = new ProcessStartInfo("sc.exe", $"stop {ServiceName}")
            {
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(8000);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined UAC, or sc.exe couldn't launch. Exit tray anyway.
        }
        catch { }
        DoExit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon.Dispose();
            _menu.Dispose();
        }
        base.Dispose(disposing);
    }
}

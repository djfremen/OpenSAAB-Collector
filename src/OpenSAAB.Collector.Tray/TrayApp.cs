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
    private readonly ContextMenuStrip _menu;
    private LogConsoleForm? _consoleForm;

    public TrayApp()
    {
        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => RefreshCountHeader();

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

        _icon.ShowBalloonTip(2000, "OpenSAAB Collector",
            $"Uploading {candidates.Count} log(s)…",
            ToolTipIcon.Info);

        int ok = 0, skipped = 0, fail = 0;
        try
        {
            var uploader = ManualUploader.FromRegistry();
            foreach (var path in candidates)
            {
                var result = await uploader.UploadOneAsync(path);
                switch (result)
                {
                    case ManualUploader.UploadResult.Uploaded:
                        ok++;
                        IncrementUploadCount();
                        break;
                    case ManualUploader.UploadResult.LowValueDeleted:
                        // Server rejected as noise; we deleted locally. Not a
                        // failure (don't retry) and not a real upload (don't
                        // count). Surface to the user so they know why the
                        // candidate list shrunk.
                        skipped++;
                        break;
                    default:
                        fail++;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _icon.ShowBalloonTip(5000, "OpenSAAB Collector",
                $"Upload error: {ex.Message}",
                ToolTipIcon.Error);
            return;
        }

        RefreshCountHeader();
        var icon = fail == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning;
        var parts = new List<string> { $"Uploaded {ok} log(s)" };
        if (skipped > 0) parts.Add($"skipped {skipped} empty");
        if (fail > 0) parts.Add($"failed {fail}");
        var msg = string.Join(", ", parts) + $". Captures total: {ReadUploadCount()}.";
        _icon.ShowBalloonTip(4000, "OpenSAAB Collector", msg, icon);
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

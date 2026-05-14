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

    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _countHeader;
    private readonly ToolStripMenuItem _toggleUpload;
    private readonly ContextMenuStrip _menu;
    private LogConsoleForm? _consoleForm;

    public TrayApp()
    {
        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => RefreshCountHeader();

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
            MessageBox.Show($"OpenSAAB Collector install ID:\n\n{id}\n\nKeep this private.",
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("About OpenSAAB", null, (_, _) =>
        {
            Process.Start(new ProcessStartInfo("https://opensaab.com") { UseShellExecute = true });
        });
        _menu.Items.Add("Exit tray (service keeps running)", null, (_, _) => ExitThread());

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            ContextMenuStrip = _menu,
            Visible = true,
            Text = "OpenSAAB Collector",
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

        int ok = 0, fail = 0;
        try
        {
            var uploader = ManualUploader.FromRegistry();
            foreach (var path in candidates)
            {
                var success = await uploader.UploadOneAsync(path);
                if (success)
                {
                    ok++;
                    IncrementUploadCount();
                }
                else
                {
                    fail++;
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
        var msg = fail == 0
            ? $"Uploaded {ok} log(s). Captures total: {ReadUploadCount()}."
            : $"Uploaded {ok} / failed {fail}. Captures total: {ReadUploadCount()}.";
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

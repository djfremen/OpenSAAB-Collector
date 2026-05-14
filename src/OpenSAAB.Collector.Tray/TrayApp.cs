using System.Diagnostics;
using Microsoft.Win32;

namespace OpenSAAB.Collector.Tray;

/// <summary>
/// Minimal tray-icon app for the OpenSAAB Collector.
///
/// Functions exposed in the right-click context menu:
///   - Toggle "Upload enabled" (writes HKLM\SOFTWARE\OpenSAAB\Collector\UploadEnabled)
///   - Open log directory (%TEMP%)
///   - Show install GUID (read-only, for support / manual contact)
///   - About
///   - Exit (closes the tray; service keeps running)
/// </summary>
internal sealed class TrayApp : ApplicationContext
{
    private const string KeyPath = @"SOFTWARE\OpenSAAB\Collector";

    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _toggleUpload;

    public TrayApp()
    {
        var menu = new ContextMenuStrip();
        _toggleUpload = new ToolStripMenuItem("Upload enabled", null, OnToggleUpload)
        {
            CheckOnClick = true,
            Checked = ReadUploadEnabled(),
        };
        menu.Items.Add(_toggleUpload);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open log folder", null, (_, _) =>
        {
            var path = Path.GetTempPath();
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        });
        menu.Items.Add("Show install GUID", null, (_, _) =>
        {
            var id = ReadString("InstallId") ?? "(not set — service may not have started)";
            MessageBox.Show($"OpenSAAB Collector install ID:\n\n{id}\n\nKeep this private.",
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About OpenSAAB", null, (_, _) =>
        {
            Process.Start(new ProcessStartInfo("https://opensaab.com") { UseShellExecute = true });
        });
        menu.Items.Add("Exit tray (service keeps running)", null, (_, _) => ExitThread());

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            ContextMenuStrip = menu,
            Visible = true,
            Text = "OpenSAAB Collector",
        };
        _icon.DoubleClick += (_, _) => menu.Show(Cursor.Position);
    }

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
            // Revert UI to actual state
            _toggleUpload.Checked = ReadUploadEnabled();
        }
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
        if (disposing) _icon.Dispose();
        base.Dispose(disposing);
    }
}

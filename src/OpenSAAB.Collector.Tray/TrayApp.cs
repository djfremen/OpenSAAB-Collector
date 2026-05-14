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
    private LogConsoleForm? _consoleForm;

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
        menu.Items.Add("Open live console…", null, (_, _) => OpenLiveConsole());
        menu.Items.Add("Open decoded console (scapy)…", null, (_, _) => OpenDecodedConsole());
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

    /// <summary>
    /// Launches a CMD console that tail-pipes the freshest shim log
    /// through the bundled scapy-based fremsoft-decoder.exe so the user
    /// sees decoded UDS service names live (ReadDataByIdentifier 0x90,
    /// SecurityAccessSeedRequest level=0x0B, etc.) instead of raw hex.
    ///
    /// Recognises three log families:
    ///   cstech2win_shim_*.log  (Tech2Win + real adapter)
    ///   j2534_shim_*.log       (Trionic / J2534 client + real adapter)
    ///   fremsoft_*.log         (FremSoft playback/standalone — no adapter)
    /// </summary>
    private void OpenDecodedConsole()
    {
        var decoderExe = Path.Combine(AppContext.BaseDirectory, "fremsoft-decoder.exe");
        if (!File.Exists(decoderExe))
        {
            MessageBox.Show(
                $"fremsoft-decoder.exe not found at\n{decoderExe}\n\n" +
                "It's bundled by the installer's decoder build step. Re-install with " +
                "the decoder built (installer\\decoder\\build-decoder.ps1).",
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var freshest = FindFreshestShimLog();
        if (freshest == null)
        {
            MessageBox.Show(
                "No active shim log in %TEMP% yet. Launch Tech2Win or your J2534 " +
                "client first; the shim writes a new log on attach. Re-open this " +
                "menu after the log appears.",
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // PowerShell pipes Get-Content -Wait through the decoder. We launch a
        // fresh CMD window so the user can see the colored output (decoder
        // honors its own ANSI when stdout is a console).
        var ps = string.Format(
            "Get-Content -Wait '{0}' | & '{1}' -",
            freshest.Replace("'", "''"),
            decoderExe.Replace("'", "''"));
        var cmd = $"start \"OpenSAAB decoded console — {Path.GetFileName(freshest)}\" " +
                  $"powershell -NoProfile -ExecutionPolicy Bypass -Command \"{ps}\"";
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", "/c " + cmd)
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to launch decoded console:\n\n" + ex.Message,
                "OpenSAAB Collector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string? FindFreshestShimLog()
    {
        try
        {
            return Directory.EnumerateFiles(Path.GetTempPath(), "*.log")
                .Where(p =>
                {
                    var n = Path.GetFileName(p);
                    return n.StartsWith("cstech2win_shim_", StringComparison.OrdinalIgnoreCase)
                        || n.StartsWith("j2534_shim_", StringComparison.OrdinalIgnoreCase)
                        || n.StartsWith("fremsoft_", StringComparison.OrdinalIgnoreCase);
                })
                .OrderByDescending(p => File.GetLastWriteTimeUtc(p))
                .FirstOrDefault();
        }
        catch { return null; }
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

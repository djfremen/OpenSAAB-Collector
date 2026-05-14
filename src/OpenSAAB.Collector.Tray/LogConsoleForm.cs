using System.Drawing;
using System.Text;

namespace OpenSAAB.Collector.Tray;

/// <summary>
/// Live tail of the freshest shim log in %TEMP%. Auto-switches when a
/// newer cstech2win_shim_*.log or j2534_shim_*.log file appears.
///
/// Color coding:
///   TX / REQ-PDU   → blue
///   RX / RSP-UDS   → green
///   NRC ($7F …)    → red
///   everything else → light gray
///
/// Capped to the last 5000 lines to keep memory bounded.
/// </summary>
internal sealed class LogConsoleForm : Form
{
    private const int MaxLines = 5000;
    private static readonly string[] LogPrefixes = ["cstech2win_shim_", "j2534_shim_"];

    private readonly RichTextBox _box;
    private readonly Label _statusLabel;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly FileSystemWatcher _dirWatcher;

    private FileStream? _activeStream;
    private string? _activePath;
    private long _lastPos;
    private bool _paused;

    public LogConsoleForm()
    {
        Text = "OpenSAAB Collector — Live console";
        Size = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 13, 18);

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Padding = new Padding(10, 8, 10, 0),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.FromArgb(156, 168, 184),
            BackColor = Color.FromArgb(19, 24, 34),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Waiting for shim activity in %TEMP%…",
        };

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(19, 24, 34),
            Padding = new Padding(8, 4, 8, 4),
            FlowDirection = FlowDirection.LeftToRight,
        };
        var pauseBtn = new Button
        {
            Text = "Pause", AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.WhiteSmoke,
            BackColor = Color.FromArgb(26, 33, 46),
        };
        pauseBtn.FlatAppearance.BorderColor = Color.FromArgb(50, 60, 80);
        pauseBtn.Click += (_, _) =>
        {
            _paused = !_paused;
            pauseBtn.Text = _paused ? "Resume" : "Pause";
        };
        var clearBtn = new Button
        {
            Text = "Clear", AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.WhiteSmoke,
            BackColor = Color.FromArgb(26, 33, 46),
        };
        clearBtn.FlatAppearance.BorderColor = Color.FromArgb(50, 60, 80);
        clearBtn.Click += (_, _) => _box.Clear();
        var openDirBtn = new Button
        {
            Text = "Open log folder", AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.WhiteSmoke,
            BackColor = Color.FromArgb(26, 33, 46),
        };
        openDirBtn.FlatAppearance.BorderColor = Color.FromArgb(50, 60, 80);
        openDirBtn.Click += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", Path.GetTempPath()) { UseShellExecute = true });
        toolbar.Controls.AddRange(new Control[] { pauseBtn, clearBtn, openDirBtn });

        _box = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(10, 13, 18),
            ForeColor = Color.FromArgb(220, 228, 240),
            Font = new Font("Consolas", 9.5f),
            BorderStyle = BorderStyle.None,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            DetectUrls = false,
        };

        Controls.Add(_box);
        Controls.Add(toolbar);
        Controls.Add(_statusLabel);

        // Watch the temp dir for new shim logs.
        _dirWatcher = new FileSystemWatcher(Path.GetTempPath())
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
        };
        _dirWatcher.Created += (_, e) => InvokeIfNeeded(MaybeSwitchToFreshest);
        _dirWatcher.Renamed += (_, e) => InvokeIfNeeded(MaybeSwitchToFreshest);

        _pollTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _pollTimer.Tick += (_, _) => Poll();
        _pollTimer.Start();

        MaybeSwitchToFreshest();
    }

    private static bool IsShimLog(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)) return false;
        foreach (var p in LogPrefixes)
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void MaybeSwitchToFreshest()
    {
        try
        {
            var newest = Directory.EnumerateFiles(Path.GetTempPath(), "*.log")
                .Where(p => IsShimLog(Path.GetFileName(p)))
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            if (newest == null) return;
            if (newest.FullName == _activePath) return;
            _activeStream?.Dispose();
            _activeStream = new FileStream(
                newest.FullName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            // Start at end of file — we want LIVE tail, not historical replay.
            _activeStream.Seek(0, SeekOrigin.End);
            _lastPos = _activeStream.Position;
            _activePath = newest.FullName;
            AppendLineColored($"\n=== now following {newest.Name} ===\n", Color.FromArgb(246, 199, 111));
            _statusLabel.Text = $"Following {newest.Name}  ·  poll 250 ms  ·  cap {MaxLines} lines";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error opening log: {ex.Message}";
        }
    }

    private void Poll()
    {
        if (_paused) return;
        if (_activeStream == null || _activePath == null) return;
        try
        {
            // Cheap check for a fresher log every poll — handles Tech2Win
            // restart spawning a new shim file mid-session.
            MaybeSwitchToFreshest();

            var fi = new FileInfo(_activePath);
            if (!fi.Exists)
            {
                _activeStream.Dispose();
                _activeStream = null;
                _activePath = null;
                _statusLabel.Text = "Log file gone — will pick up next one when it appears.";
                return;
            }
            if (fi.Length < _lastPos)
            {
                // Truncated — restart at end.
                _activeStream.Seek(0, SeekOrigin.End);
                _lastPos = _activeStream.Position;
                return;
            }
            if (fi.Length == _lastPos) return;

            var toRead = (int)Math.Min(fi.Length - _lastPos, 256 * 1024);
            var buf = new byte[toRead];
            int n = _activeStream.Read(buf, 0, buf.Length);
            _lastPos = _activeStream.Position;
            if (n <= 0) return;

            var text = Encoding.UTF8.GetString(buf, 0, n);
            foreach (var line in text.Split('\n'))
            {
                if (string.IsNullOrEmpty(line)) continue;
                AppendLineColored(line + "\n", ColorFor(line));
            }
            CapLines();
            _box.SelectionStart = _box.TextLength;
            _box.ScrollToCaret();
        }
        catch (IOException)
        {
            // File temporarily locked by the shim — retry next poll.
        }
    }

    private static Color ColorFor(string line)
    {
        // CSTech2Win shim format includes "REQ-PDU" / "RSP-UDS" tags;
        // j2534 shim format uses "TX[" / "RX[" markers.
        if (line.Contains("|TX") || line.Contains("REQ-PDU")) return Color.FromArgb(122, 162, 247);  // cyan/blue
        if (line.Contains("|RX") || line.Contains("RSP-UDS")) return Color.FromArgb(110, 231, 168);  // green
        if (line.Contains(" 7f ") || line.Contains(" 7F ") || line.Contains("|FATAL|") || line.Contains("|ERROR")) return Color.FromArgb(255, 123, 114);  // red
        if (line.Contains("|INIT") || line.Contains("|EXIT") || line.Contains("|CALL")) return Color.FromArgb(246, 199, 111);  // amber for lifecycle
        return Color.FromArgb(220, 228, 240);  // default
    }

    private void AppendLineColored(string text, Color color)
    {
        _box.SelectionStart = _box.TextLength;
        _box.SelectionLength = 0;
        _box.SelectionColor = color;
        _box.AppendText(text);
    }

    private void CapLines()
    {
        // Cheap line cap — only run when we drift well past the limit so we
        // amortize the cost. Lines.Length is O(n) on RichTextBox.
        if (_box.TextLength < 200_000) return;
        var lines = _box.Lines;
        if (lines.Length <= MaxLines) return;
        _box.Lines = lines.Skip(lines.Length - MaxLines).ToArray();
    }

    private void InvokeIfNeeded(Action a)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(a);
        else a();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _pollTimer.Stop();
        _pollTimer.Dispose();
        _dirWatcher.Dispose();
        _activeStream?.Dispose();
        base.OnFormClosing(e);
    }
}

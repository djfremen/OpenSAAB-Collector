using System.ComponentModel;

namespace OpenSAAB.Collector.Tray;

/// <summary>
/// Lightweight "uploading…" dialog shown while the tray's
/// <see cref="ManualUploader"/> walks pending captures. Single ProgressBar
/// reports completed-files / total-files; a label below shows the current
/// file's name + size. Doesn't expose bytes-uploaded inside a single POST
/// (HttpClient's default content-write path doesn't surface stream-progress
/// events without a custom wrapper); per-file granularity is good enough
/// since pcapng captures already chunk into separate files.
///
/// Form is always-on-top, no close button (closes only when uploads
/// complete) so the user can't accidentally dismiss mid-upload.
/// </summary>
internal sealed class UploadProgressForm : Form
{
    private readonly ProgressBar _bar;
    private readonly Label _status;
    private readonly Label _detail;

    public UploadProgressForm(int totalFiles)
    {
        Text = "OpenSAAB Collector — uploading";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ControlBox = false;          // can't close until done
        TopMost = true;
        ClientSize = new Size(460, 110);
        BackColor = Color.FromArgb(28, 28, 28);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        _bar = new ProgressBar
        {
            Location = new Point(16, 16),
            Size = new Size(428, 22),
            Minimum = 0,
            Maximum = Math.Max(1, totalFiles),
            Value = 0,
            Style = ProgressBarStyle.Continuous,
        };
        _status = new Label
        {
            Location = new Point(16, 46),
            Size = new Size(428, 20),
            Text = $"0 / {totalFiles} uploaded",
            ForeColor = Color.White,
        };
        _detail = new Label
        {
            Location = new Point(16, 70),
            Size = new Size(428, 20),
            Text = "Preparing…",
            ForeColor = Color.LightGray,
            AutoEllipsis = true,
        };

        Controls.Add(_bar);
        Controls.Add(_status);
        Controls.Add(_detail);
    }

    /// <summary>Update the dialog from any thread.</summary>
    public void Tick(int completed, int total, string? currentFile = null, long currentBytes = 0)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Tick(completed, total, currentFile, currentBytes));
            return;
        }
        _bar.Maximum = Math.Max(1, total);
        _bar.Value = Math.Clamp(completed, 0, _bar.Maximum);
        _status.Text = $"{completed} / {total} uploaded";
        if (!string.IsNullOrEmpty(currentFile))
        {
            var name = Path.GetFileName(currentFile);
            _detail.Text = currentBytes > 0
                ? $"Uploading {name}  ({FormatBytes(currentBytes)})"
                : $"Uploading {name}";
        }
    }

    /// <summary>Close + dispose from any thread.</summary>
    public void Finish()
    {
        if (InvokeRequired) { BeginInvoke(Finish); return; }
        Close();
        Dispose();
    }

    private static string FormatBytes(long n)
    {
        if (n < 1024) return $"{n} B";
        if (n < 1024 * 1024) return $"{n / 1024.0:F1} KB";
        return $"{n / 1024.0 / 1024.0:F2} MB";
    }

    protected override CreateParams CreateParams
    {
        get
        {
            // No close X in the title bar — uploads should run to completion.
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x0200;  // CS_NOCLOSE
            return cp;
        }
    }
}

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace OpenSAAB.Collector.Service;

/// <summary>
/// v0.4.0: the DLL-shim model is retired. The Collector now switches on the
/// genuine Chipsoft driver's OWN Boost.Log sink (<c>LogLevel: 0</c> in
/// options.json, applied by <see cref="ChipsoftConfig"/>) and harvests the
/// <c>*.log</c> files the driver writes into
/// <c>C:\ProgramData\CHIPSOFT_J2534\logs\</c>.
///
/// "Ready to upload" detection: the driver keeps its ACTIVE session log open
/// for the whole Tech2Win / J2534 session (a Boost.Log file sink). A previous
/// session's log is closed and can be opened exclusively. So a log is ready
/// when (a) it has settled — untouched for <see cref="SettleSeconds"/> — AND
/// (b) it can be opened with <c>FileShare.None</c>, proving the driver has let
/// go of it. This is strictly more reliable than the old rotation heuristic:
/// a partial in-progress session log can never be uploaded by mistake.
/// </summary>
public sealed class Worker : BackgroundService
{
    private const int SettleSeconds = 30;

    private readonly ILogger<Worker> _log;
    private readonly InstallSettings _settings;
    private readonly Uploader _uploader;
    private readonly ConcurrentDictionary<string, DateTime> _pending = new();
    private FileSystemWatcher? _watcher;

    public Worker(ILogger<Worker> log, InstallSettings settings, Uploader uploader)
    {
        _log = log;
        _settings = settings;
        _uploader = uploader;
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        // Step 1: switch on the driver's native logging. Idempotent.
        ChipsoftConfig.EnsureLogLevelZero(_log);

        var logsDir = InstallSettings.ChipsoftLogsDir;

        try
        {
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
                _log.LogInformation("Created Chipsoft logs dir: {Dir}", logsDir);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not create Chipsoft logs dir {Dir}", logsDir);
        }

        _log.LogInformation(
            "OpenSAAB Collector v{Version} starting. InstallId={InstallId} Watch={Dir} Endpoint={Url}",
            _settings.CollectorVersion, _settings.InstallId, logsDir, _settings.IngestUrl);

        // Pick up logs left behind from before the service started.
        EnqueueExisting(logsDir);

        try
        {
            _watcher = new FileSystemWatcher(logsDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true,
                IncludeSubdirectories = false,
            };
            _watcher.Created += OnFileEvent;
            _watcher.Changed += OnFileEvent;
            _watcher.Renamed += OnRenamed;
            _log.LogInformation("FileSystemWatcher attached to {Dir}", logsDir);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Failed to attach FileSystemWatcher to {Dir} — auto-upload disabled. " +
                "Tray manual 'Upload now' button still works as fallback.",
                logsDir);
            // Don't return — keep the drain loop alive so manual flushes still work.
        }

        // Drain loop — every 5s, flush logs that have settled and closed.
        var ticker = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await ticker.WaitForNextTickAsync(stop))
            {
                await FlushSettledAsync(stop);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
    }

    private void EnqueueExisting(string dir)
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(dir))
            {
                if (IsTargetLog(Path.GetFileName(path)))
                {
                    _pending[path] = File.GetLastWriteTimeUtc(path);
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "EnqueueExisting failed");
        }
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (!IsTargetLog(e.Name ?? "")) return;
        _pending[e.FullPath] = DateTime.UtcNow;
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (IsTargetLog(e.OldName ?? "")) _pending.TryRemove(e.OldFullPath, out _);
        if (IsTargetLog(e.Name ?? "")) _pending[e.FullPath] = DateTime.UtcNow;
    }

    private async Task FlushSettledAsync(CancellationToken stop)
    {
        var now = DateTime.UtcNow;
        foreach (var (path, lastSeen) in _pending.ToArray())
        {
            if ((now - lastSeen).TotalSeconds < SettleSeconds) continue;
            if (!File.Exists(path))
            {
                _pending.TryRemove(path, out _);
                continue;
            }
            // Re-check the file's actual mtime in case events came in late.
            var mtime = File.GetLastWriteTimeUtc(path);
            if ((now - mtime).TotalSeconds < SettleSeconds)
            {
                _pending[path] = mtime;
                continue;
            }
            await ProcessOneAsync(path, stop);
        }
    }

    private async Task ProcessOneAsync(string path, CancellationToken stop)
    {
        if (!_settings.UploadEnabled || string.IsNullOrEmpty(_settings.ConsentVersion))
        {
            _log.LogDebug("Upload disabled or no consent — leaving {Path} local", path);
            _pending.TryRemove(path, out _);
            return;
        }

        byte[] bytes;
        try
        {
            // FileShare.None — succeeds ONLY if no other process holds the
            // file. The Chipsoft driver keeps its active-session log open, so
            // this exclusive open fails for an in-progress session and
            // succeeds once the session has ended and the driver unloaded.
            // That makes it our "the driver is done with this file" gate.
            await using var fs = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.None);
            bytes = new byte[fs.Length];
            int off = 0;
            while (off < bytes.Length)
            {
                int n = await fs.ReadAsync(bytes.AsMemory(off), stop);
                if (n == 0) break;
                off += n;
            }
        }
        catch (IOException)
        {
            // Driver still has the log open (active Tech2Win session) — retry.
            _log.LogDebug("{Path} still held by the Chipsoft driver — will retry", path);
            _pending[path] = DateTime.UtcNow;
            return;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Could not read {Path}", path);
            _pending.TryRemove(path, out _);
            return;
        }

        if (bytes.Length == 0)
        {
            _log.LogDebug("Skipping empty {Path}", path);
            _pending.TryRemove(path, out _);
            try { File.Delete(path); } catch { }
            return;
        }

        try
        {
            byte[] gzipped;
            using (var ms = new MemoryStream())
            {
                using (var gz = new System.IO.Compression.GZipStream(
                           ms, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
                {
                    await gz.WriteAsync(bytes, stop);
                }
                gzipped = ms.ToArray();
            }

            // v0.4.0: every capture is now a genuine Chipsoft driver log.
            var ok = await _uploader.UploadAsync(gzipped, "chipsoft", stop);
            if (ok)
            {
                _log.LogInformation("Uploaded {Path}: {InBytes} → {OutBytes} bytes (gzip)",
                    path, bytes.Length, gzipped.Length);
                IncrementUploadCount();
                _pending.TryRemove(path, out _);
                // File is closed (we held it FileShare.None) — delete is safe.
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "Could not remove {Path} after upload", path);
                }
            }
            else
            {
                _log.LogWarning("Upload failed for {Path} after retries — keeping for next pass", path);
                _pending[path] = DateTime.UtcNow.AddMinutes(5);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ProcessOne unexpected error for {Path}", path);
            _pending.TryRemove(path, out _);
        }
    }

    /// <summary>
    /// True for the driver's own log files — any <c>*.log</c> in the Chipsoft
    /// logs dir (named <c>YYYYMMDD_HHMMSS.log</c> by the Boost.Log sink).
    /// Skips our own <c>.uploaded</c> rename leftovers from older versions.
    /// </summary>
    private static bool IsTargetLog(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name.EndsWith(".log", StringComparison.OrdinalIgnoreCase);
    }

    private static void IncrementUploadCount()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\OpenSAAB\Collector", writable: true);
            if (key == null) return;
            var current = key.GetValue("UploadCount") as int? ?? 0;
            key.SetValue("UploadCount", current + 1, RegistryValueKind.DWord);
        }
        catch { /* counter is best-effort; service must keep running */ }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("OpenSAAB Collector stopping.");
        _watcher?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}

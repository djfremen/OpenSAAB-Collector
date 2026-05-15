using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace OpenSAAB.Collector.Service;

/// <summary>
/// Watches %TEMP% for shim logs (cstech2win_shim_*.log, j2534_shim_*.log).
/// On rotation (file untouched for SettleSeconds), gzips and hands to
/// the Uploader.
///
/// "Rotation" detection: the shims open a fresh log file each time the
/// host process attaches; the previous file is then never written again.
/// We treat any file that hasn't been touched in 30 seconds AND whose
/// process owner has dropped its handle as ready to upload.
/// </summary>
public sealed class Worker : BackgroundService
{
    private const int SettleSeconds = 30;
    // Shim log prefixes (.log) — cstech2win + j2534
    private static readonly string[] LogPrefixes = ["cstech2win_shim_", "j2534_shim_"];
    // USBPcap output prefix (.pcapng) — written by UsbPcapSupervisor
    private const string UsbpcapPrefix = "usbpcap_";

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
        var tempDir = Path.GetTempPath();
        _log.LogInformation("OpenSAAB Collector starting. InstallId={InstallId} Watch={Dir} Endpoint={Url}",
            _settings.InstallId, tempDir, _settings.IngestUrl);

        // Pick up any pre-existing logs that might have been left behind
        // from before the service started.
        EnqueueExisting(tempDir);

        _watcher = new FileSystemWatcher(tempDir)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
        };
        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
        _watcher.Renamed += OnRenamed;

        // Drain loop — every 5s, flush logs that have settled.
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
            // File hasn't been touched in SettleSeconds — try to upload.
            if (!File.Exists(path))
            {
                _pending.TryRemove(path, out _);
                continue;
            }
            // Check the file's actual mtime in case events came in late.
            var mtime = File.GetLastWriteTimeUtc(path);
            if ((now - mtime).TotalSeconds < SettleSeconds)
            {
                _pending[path] = mtime;
                continue;
            }
            await ProcessOneAsync(path, stop);
            _pending.TryRemove(path, out _);
        }
    }

    private async Task ProcessOneAsync(string path, CancellationToken stop)
    {
        if (!_settings.UploadEnabled || string.IsNullOrEmpty(_settings.ConsentVersion))
        {
            _log.LogDebug("Upload disabled or no consent — leaving {Path} local", path);
            return;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(path, stop);
            if (bytes.Length == 0)
            {
                _log.LogDebug("Skipping empty {Path}", path);
                return;
            }

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

            var fname = Path.GetFileName(path);
            string source;
            if (fname.StartsWith("cstech2win_shim_", StringComparison.Ordinal))
                source = "cstech2win";
            else if (fname.StartsWith("j2534_shim_", StringComparison.Ordinal))
                source = "j2534";
            else if (fname.StartsWith(UsbpcapPrefix, StringComparison.Ordinal))
                source = "usbpcap";
            else
                source = "cstech2win";  // unreachable given IsTargetLog gate

            var ok = await _uploader.UploadAsync(gzipped, source, stop);
            if (ok)
            {
                _log.LogInformation("Uploaded {Path}: {InBytes} → {OutBytes} bytes (gzip)",
                    path, bytes.Length, gzipped.Length);
                IncrementUploadCount();
                // Server has it on R2 — drop the local copy so it can never
                // ship twice. If delete fails (e.g. another process holds an
                // open handle), fall back to renaming.
                try
                {
                    File.Delete(path);
                }
                catch (Exception)
                {
                    try
                    {
                        var dest = path + ".uploaded";
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(path, dest);
                    }
                    catch (Exception ex2)
                    {
                        _log.LogDebug(ex2, "Could not remove {Path} after upload", path);
                    }
                }
            }
            else
            {
                _log.LogWarning("Upload failed for {Path} after retries — keeping for next pass", path);
                // Re-enqueue with a fresh deadline so we retry later.
                _pending[path] = DateTime.UtcNow.AddMinutes(5);
            }
        }
        catch (IOException ioex)
        {
            // File is likely still locked by the producing shim — try later.
            _log.LogDebug(ioex, "ProcessOne IO error for {Path}, will retry", path);
            _pending[path] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ProcessOne unexpected error for {Path}", path);
        }
    }

    private static bool IsTargetLog(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        // Shim text logs end in .log
        if (name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var p in LogPrefixes)
            {
                if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        // USBPcap captures end in .pcapng
        if (name.EndsWith(".pcapng", StringComparison.OrdinalIgnoreCase)
            && name.StartsWith(UsbpcapPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
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

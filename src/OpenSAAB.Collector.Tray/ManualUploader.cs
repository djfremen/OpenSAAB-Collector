using System.IO.Compression;
using System.Net.Http;
using Microsoft.Win32;

namespace OpenSAAB.Collector.Tray;

/// <summary>
/// Tray-side "Upload now" path. Reads InstallSettings from the registry,
/// gzips and POSTs each pending shim log, renames successful uploads to
/// .uploaded so the service's FileSystemWatcher skips them on its 30 s
/// settle pass.
///
/// Standalone from the Service's Uploader to keep the tray a single-binary
/// drop-in with no project reference to the Service.
/// </summary>
internal sealed class ManualUploader
{
    private const string KeyPath = @"SOFTWARE\OpenSAAB\Collector";
    private const string DefaultIngest = "https://openSAAB.com/ingest/shim-log";
    private const string CollectorVersion = "0.1.4";

    private static readonly string[] LogPrefixes =
    {
        "cstech2win_shim_", "j2534_shim_",
    };

    private static readonly HttpClient Http = new(new HttpClientHandler { UseProxy = true })
    {
        Timeout = TimeSpan.FromMinutes(2),
    };

    private readonly string _installId;
    private readonly string _ingestUrl;
    private readonly string _consentVersion;
    private readonly string? _vehicleYear;
    private readonly string? _vehicleModel;

    private ManualUploader(string installId, string ingestUrl, string consentVersion,
                           string? vehicleYear, string? vehicleModel)
    {
        _installId = installId;
        _ingestUrl = ingestUrl;
        _consentVersion = consentVersion;
        _vehicleYear = vehicleYear;
        _vehicleModel = vehicleModel;
    }

    public static ManualUploader FromRegistry()
    {
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath)
            ?? throw new InvalidOperationException(
                "HKLM\\SOFTWARE\\OpenSAAB\\Collector not found — is the service installed?");

        var installId = key.GetValue("InstallId") as string ?? string.Empty;
        if (string.IsNullOrEmpty(installId))
            throw new InvalidOperationException(
                "InstallId missing — the service must run once before manual upload.");

        return new ManualUploader(
            installId,
            (key.GetValue("IngestUrl") as string) ?? DefaultIngest,
            (key.GetValue("ConsentVersion") as string) ?? string.Empty,
            key.GetValue("VehicleYear") as string,
            key.GetValue("VehicleModel") as string);
    }

    public static List<string> FindPendingLogs(string tempDir)
    {
        var results = new List<string>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(tempDir, "*.log"))
            {
                var name = Path.GetFileName(path);
                foreach (var p in LogPrefixes)
                {
                    if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip empty + zero-size files (no real content yet).
                        try
                        {
                            if (new FileInfo(path).Length > 0) results.Add(path);
                        }
                        catch { /* skip racing files */ }
                        break;
                    }
                }
            }
        }
        catch { /* swallow — return what we have */ }
        return results;
    }

    public async Task<bool> UploadOneAsync(string path)
    {
        byte[] bytes;
        try
        {
            // FileShare.ReadWrite | Delete: shim and service may have it open.
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var ms = new MemoryStream();
            await fs.CopyToAsync(ms);
            bytes = ms.ToArray();
        }
        catch
        {
            return false;
        }
        if (bytes.Length == 0) return false;

        byte[] gzipped;
        using (var ms = new MemoryStream())
        {
            using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                await gz.WriteAsync(bytes);
            }
            gzipped = ms.ToArray();
        }

        var source = Path.GetFileName(path).StartsWith("cstech2win_shim_", StringComparison.Ordinal)
            ? "cstech2win" : "j2534";

        using var req = new HttpRequestMessage(HttpMethod.Post, _ingestUrl)
        {
            Content = new ByteArrayContent(gzipped),
        };
        req.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        req.Headers.Add("X-Install-ID", _installId);
        req.Headers.Add("X-Capture-Source", source);
        req.Headers.Add("X-Consent-Version", _consentVersion);
        req.Headers.Add("X-Collector-Version", CollectorVersion);
        if (!string.IsNullOrEmpty(_vehicleYear)) req.Headers.Add("X-Vehicle-Year", _vehicleYear);
        if (!string.IsNullOrEmpty(_vehicleModel)) req.Headers.Add("X-Vehicle-Model", _vehicleModel);

        try
        {
            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return false;
        }
        catch
        {
            return false;
        }

        // Rename .log → .uploaded so the service's FileSystemWatcher (pattern
        // *.log) ignores the file on its next settle pass. Best-effort.
        try
        {
            var dest = path + ".uploaded";
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(path, dest);
        }
        catch { /* rename racing with the shim's append is fine — server dedupes */ }

        return true;
    }
}

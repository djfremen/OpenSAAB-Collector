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
    private const string CollectorVersion = "0.2.5";

    /// <summary>Per-file outcome of <see cref="UploadOneAsync"/>.</summary>
    internal enum UploadResult
    {
        /// <summary>Server accepted the upload (2xx); file deleted locally.</summary>
        Uploaded,
        /// <summary>Server rejected as low-value noise (HTTP 422); file deleted locally — don't retry, don't count as an upload.</summary>
        LowValueDeleted,
        /// <summary>Transient or unknown error; file kept for the next pass.</summary>
        Failed,
    }

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

    /// <summary>
    /// Enumerate pending captures across all TEMP locations the Collector can
    /// write to. Shim logs land in the user's %TEMP% (CSTech2Win.dll runs in
    /// Tech2Win's user process); USBPcap captures land in
    /// <c>C:\Windows\SystemTemp</c> because UsbPcapSupervisor runs as LocalSystem
    /// and its <c>Path.GetTempPath()</c> resolves there. We scan both.
    /// </summary>
    public static List<string> FindPendingLogs(string userTempDir)
    {
        var results = new List<string>();
        var dirs = new List<string> { userTempDir };
        // LocalSystem's TEMP (where the supervisor drops pcaps).
        var systemTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SystemTemp");
        if (Directory.Exists(systemTemp)) dirs.Add(systemTemp);

        foreach (var dir in dirs)
        {
            try
            {
                // Shim text logs (cstech2win, j2534) — userTempDir mostly,
                // but cheap to look in both.
                foreach (var path in Directory.EnumerateFiles(dir, "*.log"))
                {
                    var name = Path.GetFileName(path);
                    foreach (var p in LogPrefixes)
                    {
                        if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                        {
                            try { if (new FileInfo(path).Length > 0) results.Add(path); }
                            catch { }
                            break;
                        }
                    }
                }
                // USBPcap captures (.pcapng) — usually SystemTemp.
                foreach (var path in Directory.EnumerateFiles(dir, "usbpcap_*.pcapng"))
                {
                    try { if (new FileInfo(path).Length > 24) results.Add(path); }  // skip SHB-only stubs
                    catch { }
                }
            }
            catch { /* unreadable dir — keep scanning the others */ }
        }
        return results;
    }

    public async Task<UploadResult> UploadOneAsync(string path)
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
            return UploadResult.Failed;
        }
        if (bytes.Length == 0) return UploadResult.Failed;

        // v0.2.5+: pre-upload pcap integrity check. Stops malformed captures
        // from entering the retry loop. Service-side Worker does the same;
        // doubling up because ManualUploader can be invoked when the service
        // hasn't yet run on the file.
        if (path.EndsWith(".pcapng", StringComparison.OrdinalIgnoreCase))
        {
            if (bytes.Length < 24) { try { File.Delete(path); } catch { } return UploadResult.LowValueDeleted; }
            uint magic = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
            uint network = (uint)(bytes[20] | (bytes[21] << 8) | (bytes[22] << 16) | (bytes[23] << 24));
            if (magic != 0xA1B2C3D4u && magic != 0xA1B23C4Du) {
                try { File.Delete(path); } catch { }
                return UploadResult.LowValueDeleted;
            }
            if (network != 249 || bytes.Length == 24) {
                try { File.Delete(path); } catch { }
                return UploadResult.LowValueDeleted;
            }
        }

        byte[] gzipped;
        using (var ms = new MemoryStream())
        {
            using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                await gz.WriteAsync(bytes);
            }
            gzipped = ms.ToArray();
        }

        var fname = Path.GetFileName(path);
        string source;
        if (fname.StartsWith("cstech2win_shim_", StringComparison.Ordinal))
            source = "cstech2win";
        else if (fname.StartsWith("j2534_shim_", StringComparison.Ordinal))
            source = "j2534";
        else if (fname.StartsWith("usbpcap_", StringComparison.Ordinal))
            source = "usbpcap";
        else
            source = "cstech2win";  // unreachable given FindPendingLogs filter

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
        req.Headers.Add("X-Content-SHA256", Sha256Hex(gzipped));
        if (!string.IsNullOrEmpty(_vehicleYear)) req.Headers.Add("X-Vehicle-Year", _vehicleYear);
        if (!string.IsNullOrEmpty(_vehicleModel)) req.Headers.Add("X-Vehicle-Model", _vehicleModel);

        bool acceptedAsLowValue = false;
        try
        {
            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                // Server-side low-value gate (Layer 2): the capture contained
                // no analytically interesting UDS traffic — typically a single
                // TesterPresent frame on $0101. Treat as terminal so we delete
                // locally and don't loop forever uploading the same noise.
                // Server response body looks like:
                //   {"detail":{"reason":"low_value_capture", ...}}
                if (resp.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    if (body.Contains("low_value_capture", StringComparison.Ordinal))
                    {
                        acceptedAsLowValue = true;
                    }
                }
                if (!acceptedAsLowValue) return UploadResult.Failed;
            }
        }
        catch
        {
            return UploadResult.Failed;
        }

        // Server has it (or rejected it as noise) — drop the local copy so the
        // same log can never ship twice. If delete fails (file still held open
        // by an active Tech2Win shim session in %TEMP%), fall back to renaming
        // so the next "Upload now" pass skips it. v0.1.6: server-side
        // persistence is on R2 now, so client deletion is safe.
        try
        {
            File.Delete(path);
        }
        catch
        {
            try
            {
                var dest = path + ".uploaded";
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(path, dest);
            }
            catch { /* worst case: re-upload next pass; server dedupes by wall_ms */ }
        }

        return acceptedAsLowValue ? UploadResult.LowValueDeleted : UploadResult.Uploaded;
    }

    private static string Sha256Hex(byte[] data)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
    }
}

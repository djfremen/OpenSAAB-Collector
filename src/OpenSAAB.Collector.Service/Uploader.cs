using Microsoft.Extensions.Logging;

namespace OpenSAAB.Collector.Service;

/// <summary>
/// HTTP POST a gzipped shim log to the OpenSAAB ingest endpoint with
/// exponential-backoff retry. Returns true on first success, false if
/// all retry attempts failed.
///
/// Failed uploads are NOT queued to disk in this MVP — the caller
/// (Worker) re-enqueues for a later pass.
/// </summary>
public sealed class Uploader
{
    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        // Default; allows the system proxy if the user's behind one.
        UseProxy = true,
    })
    {
        Timeout = TimeSpan.FromMinutes(2),
    };

    private readonly ILogger<Uploader> _log;
    private readonly InstallSettings _settings;

    public Uploader(ILogger<Uploader> log, InstallSettings settings)
    {
        _log = log;
        _settings = settings;
    }

    public async Task<bool> UploadAsync(byte[] gzippedBody, string source, CancellationToken stop)
    {
        const int MaxAttempts = 5;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var req = BuildRequest(gzippedBody, source);
                using var resp = await Http.SendAsync(req, stop);
                if (resp.IsSuccessStatusCode) return true;

                if ((int)resp.StatusCode >= 400 && (int)resp.StatusCode < 500
                    && resp.StatusCode != System.Net.HttpStatusCode.RequestTimeout)
                {
                    // 4xx other than 408 — don't retry; server rejected the payload.
                    _log.LogWarning("Upload rejected by server: {Status} {Reason}",
                        (int)resp.StatusCode, resp.ReasonPhrase);
                    return false;
                }
                _log.LogInformation("Upload attempt {Attempt} got {Status}, will retry",
                    attempt, (int)resp.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _log.LogInformation("Upload attempt {Attempt} threw: {Msg}", attempt, ex.Message);
            }
            catch (TaskCanceledException) when (!stop.IsCancellationRequested)
            {
                _log.LogInformation("Upload attempt {Attempt} timed out", attempt);
            }

            if (attempt < MaxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));  // 2,4,8,16
                await Task.Delay(delay, stop);
            }
        }
        return false;
    }

    private HttpRequestMessage BuildRequest(byte[] body, string source)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, _settings.IngestUrl)
        {
            Content = new ByteArrayContent(body),
        };
        req.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        req.Headers.Add("X-Install-ID", _settings.InstallId);
        req.Headers.Add("X-Capture-Source", source);
        req.Headers.Add("X-Consent-Version", _settings.ConsentVersion);
        req.Headers.Add("X-Collector-Version", _settings.CollectorVersion);
        // v0.2.5+: client-side SHA256 of the gzipped body. Server verifies
        // and uses for dedup (byte-identical re-uploads short-circuit to 200
        // with {"duplicate": true} instead of storing a second R2 object).
        req.Headers.Add("X-Content-SHA256", Sha256Hex(body));
        if (!string.IsNullOrEmpty(_settings.VehicleYear))
            req.Headers.Add("X-Vehicle-Year", _settings.VehicleYear);
        if (!string.IsNullOrEmpty(_settings.VehicleModel))
            req.Headers.Add("X-Vehicle-Model", _settings.VehicleModel);
        return req;
    }

    internal static string Sha256Hex(byte[] data)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
    }
}

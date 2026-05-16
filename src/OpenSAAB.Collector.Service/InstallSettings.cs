using Microsoft.Win32;

namespace OpenSAAB.Collector.Service;

/// <summary>
/// Per-install configuration backed by HKLM\SOFTWARE\OpenSAAB\Collector.
///
/// The InstallId is generated on first load and persists until uninstall.
/// UploadEnabled + ConsentVersion are set by the InnoSetup installer's
/// consent page; the user can flip UploadEnabled later via the tray app.
/// </summary>
public sealed class InstallSettings
{
    private const string KeyPath = @"SOFTWARE\OpenSAAB\Collector";

    public string InstallId { get; init; } = string.Empty;
    public string IngestUrl { get; init; } = "https://relevant-diann-djfremen2-c013cdc3.koyeb.app/ingest/shim-log";
    public bool UploadEnabled { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public string? VehicleYear { get; set; }
    public string? VehicleModel { get; set; }
    public string CollectorVersion => "0.2.2";

    public static InstallSettings Load()
    {
        // Using LocalMachine because the Service runs as LocalSystem.
        // The installer pre-creates the key with admin rights; the service
        // has read access. UploadEnabled / VehicleYear / VehicleModel writes
        // happen from the tray app (per-user UAC if needed).
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                       ?? Registry.LocalMachine.CreateSubKey(KeyPath);

        var installId = key.GetValue("InstallId") as string;
        if (string.IsNullOrEmpty(installId))
        {
            installId = Guid.NewGuid().ToString("D");
            key.SetValue("InstallId", installId);
        }

        return new InstallSettings
        {
            InstallId = installId,
            IngestUrl = (key.GetValue("IngestUrl") as string)
                       ?? "https://openSAAB.com/ingest/shim-log",
            UploadEnabled = (key.GetValue("UploadEnabled") as int? ?? 0) != 0,
            ConsentVersion = (key.GetValue("ConsentVersion") as string) ?? string.Empty,
            VehicleYear = key.GetValue("VehicleYear") as string,
            VehicleModel = key.GetValue("VehicleModel") as string,
        };
    }
}

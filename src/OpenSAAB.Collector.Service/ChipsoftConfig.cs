using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace OpenSAAB.Collector.Service;

/// <summary>
/// v0.4.0: turns ON the genuine Chipsoft driver's own diagnostic logging by
/// setting <c>LogLevel: 0</c> in <c>options.json</c>.
///
/// Background (from Chipsoft_RE static RE — notes/2026-05-05-config-answers.md):
/// the driver's log-sink builder gates on the <c>LogLevel</c> byte. Values
/// <c>0..4</c> create a Boost.Log file sink with that severity threshold;
/// <c>≥5</c> (the shipped default of 10) creates no sink at all. <c>0</c> =
/// trace = maximum verbosity — captures the <c>(W) &gt;&gt;</c> / <c>(R) &lt;&lt;</c>
/// wire markers we want.
///
/// This replaces the old DLL-shim model entirely: nothing is written into the
/// Chipsoft program folder, so there is nothing to back up, restore, or race
/// against a Tech2Win update.
/// </summary>
internal static class ChipsoftConfig
{
    /// <summary>
    /// Ensure <c>options.json</c> has <c>LogLevel: 0</c>. Idempotent — safe to
    /// call on every service start. Existing keys (Lite/Mid/Pro tier objects,
    /// OpenPort2Mode, RemapAUXToPIN, …) are preserved untouched; only LogLevel
    /// is changed. Never throws — logs and returns on any failure so the
    /// service stays alive.
    /// </summary>
    public static void EnsureLogLevelZero(ILogger log)
    {
        var optionsPath = InstallSettings.ChipsoftOptionsJson;
        var logsDir = InstallSettings.ChipsoftLogsDir;

        // Pre-create the logs dir so the Worker's FileSystemWatcher can attach
        // even before the driver has run once. The driver creates it too —
        // this is just a belt-and-braces guard. Non-fatal if it fails.
        try
        {
            Directory.CreateDirectory(logsDir);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not pre-create Chipsoft logs dir {Dir}", logsDir);
        }

        try
        {
            if (File.Exists(optionsPath))
            {
                JsonNode? parsed;
                try
                {
                    parsed = JsonNode.Parse(File.ReadAllText(optionsPath));
                }
                catch (Exception ex)
                {
                    // Don't clobber a config we can't parse — the driver may
                    // still read it, and overwriting could lose tier settings.
                    log.LogError(ex,
                        "Chipsoft options.json at {Path} is not valid JSON — leaving it " +
                        "untouched. Logging may stay off until it is hand-fixed.",
                        optionsPath);
                    return;
                }

                if (parsed is not JsonObject root)
                {
                    log.LogError(
                        "Chipsoft options.json at {Path} is not a JSON object — leaving untouched.",
                        optionsPath);
                    return;
                }

                int? current = TryGetInt(root["LogLevel"]);
                if (current == 0)
                {
                    log.LogInformation("Chipsoft options.json already has LogLevel=0 — no change.");
                    return;
                }

                root["LogLevel"] = 0;
                File.WriteAllText(optionsPath,
                    root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                log.LogInformation(
                    "Patched Chipsoft options.json LogLevel {Old} -> 0 (driver logging enabled).",
                    current?.ToString() ?? "(unset)");
            }
            else
            {
                // No config file — the driver has been running on built-in
                // ptree defaults. Write a minimal file that ONLY sets
                // LogLevel; the driver fills every other key from its own
                // defaults, so we don't risk guessing tier settings wrong.
                Directory.CreateDirectory(InstallSettings.ChipsoftDataDir);
                var root = new JsonObject { ["LogLevel"] = 0 };
                File.WriteAllText(optionsPath,
                    root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                log.LogInformation(
                    "Created Chipsoft options.json with LogLevel=0 at {Path}.", optionsPath);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex,
                "Failed to ensure Chipsoft LogLevel=0 at {Path} — driver logging may stay off.",
                optionsPath);
        }
    }

    /// <summary>
    /// Read a JSON value as an int, tolerating both <c>0</c> and <c>"0"</c>
    /// encodings (boost::ptree round-trips numbers as strings).
    /// </summary>
    private static int? TryGetInt(JsonNode? node)
    {
        if (node is not JsonValue v) return null;
        if (v.TryGetValue(out int i)) return i;
        if (v.TryGetValue(out string? s) && int.TryParse(s, out int parsed)) return parsed;
        return null;
    }
}

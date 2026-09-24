# Architecture

> **v0.4.0** — the DLL-shim model is retired. The Collector switches on the
> genuine Chipsoft driver's own logging instead of intercepting its DLLs.

```
                    ┌──────────────────────────────────────────────┐
                    │            EliteBook (Windows 10)            │
                    │                                              │
   Tech2Win  ──┐    │   genuine Chipsoft J2534 Pro driver          │
   J2534      ─┤──► │   (CSTech2Win.dll / j2534_interface.dll —    │
   clients     │    │    UNMODIFIED)                               │
               │    │                                              │
               │    │   reads C:\ProgramData\CHIPSOFT_J2534\       │
               │    │          options.json   →  LogLevel: 0       │
               │    │                                              │
               └──► │   Boost.Log sink writes each session to      │
                    │   C:\ProgramData\CHIPSOFT_J2534\logs\        │
                    │          <YYYYMMDD>_<HHMMSS>.log             │
                    │                                              │
                    │  ┌─────────────────────────────────────────┐ │
                    │  │   OpenSAABCollector  (Windows Service)  │ │
                    │  │                                         │ │
                    │  │  on start: ChipsoftConfig ensures       │ │
                    │  │            options.json LogLevel = 0    │ │
                    │  │                                         │ │
                    │  │  FileSystemWatcher on …\logs\           │ │
                    │  │   → settle 30s + exclusive-open check   │ │
                    │  │   → gzip → POST /ingest/shim-log        │ │
                    │  │                                         │ │
                    │  │  reads HKLM\SOFTWARE\OpenSAAB\Collector │ │
                    │  │  for InstallId / UploadEnabled / etc    │ │
                    │  └─────────────────────────────────────────┘ │
                    │             │                                │
                    │             │  HTTPS POST gzipped log        │
                    │             │  + headers (X-Install-ID etc)  │
                    │             ▼                                │
                    └─────────────│────────────────────────────────┘
                                  │
                                  ▼
                    ┌──────────────────────────────────────────────┐
                    │   openSAAB.com  (Koyeb / saab-security-api)  │
                    │                                              │
                    │   POST /ingest/shim-log                      │
                    │     → uploads/<install-id>/                  │
                    │         chipsoft_<wall_ms>.log.gz            │
                    │         chipsoft_<wall_ms>.meta.json         │
                    └──────────────────────────────────────────────┘
                                  │
                                  ▼
                    ┌──────────────────────────────────────────────┐
                    │   github.com/djfremen/OpenSAAB               │
                    │     commands/saab/*.yaml  (catalog)          │
                    └──────────────────────────────────────────────┘
```

## Why the shim went away

The shim model replaced `CSTech2Win.dll` with an interception DLL that
forwarded calls to a renamed `CSTech2Win_real.dll` and logged each one.
It worked, but it carried real install friction and fragility:

- A DLL swap inside someone else's program folder, with backup/restore
  logic that had to survive re-installs, uninstalls, and Tech2Win
  updates.
- Restart Manager corner cases when a headless `emulator.exe` kept the
  shim DLL loaded.
- A second DLL (`j2534_interface.dll`) that most contributors never
  exercised.

The Chipsoft driver already has a built-in Boost.Log sink. Static RE of
`j2534_interface.dll` (see `Chipsoft_RE/notes/2026-05-05-config-answers.md`)
showed it is gated purely by the `LogLevel` byte in `options.json`:
`0..4` create the sink, `≥5` (the shipped default of `10`) create
nothing. So `LogLevel: 0` turns on full trace logging with **zero**
files installed into the Chipsoft folder.

## Component responsibilities

### `ChipsoftConfig` (Service)

On every service start, ensures `C:\ProgramData\CHIPSOFT_J2534\options.json`
has `LogLevel: 0`. Patches only that key — any Lite/Mid/Pro tier objects
and their `OpenPort2Mode` / `RemapAUXToPIN` / `SplitReadTimeout` settings
are preserved. If `options.json` doesn't exist, writes a minimal
`{ "LogLevel": 0 }` and lets the driver default everything else.

### Collector Service (this repo)

.NET 8 Windows Service. `BackgroundService` with a `FileSystemWatcher`
on the Chipsoft logs dir. Settle window is 30 seconds.

"Ready to upload" needs two signals: (1) the log has been untouched for
30 s, AND (2) it can be opened with `FileShare.None`. The Chipsoft
driver holds its **active** session log open for the whole session, so
an exclusive open fails until the session ends and the driver unloads.
This makes a partial / in-progress upload impossible — strictly better
than the old rotation heuristic.

### Tray app (this repo)

WinForms tray icon: toggle Upload Enabled, live console (raw tail of the
freshest driver log), "Upload pending logs now", open log folder, show
install GUID, captures counter.

### Server (saab-security-api repo)

FastAPI on Koyeb. `POST /ingest/shim-log` — gzip required, validated
headers, persisted to `uploads/<install-id>/`. The uploaded files are
genuine Chipsoft driver logs (`X-Capture-Source: chipsoft`); the driver's
log lines are obfuscated, decoded with
`Chipsoft_RE/tools/decode_chipsoft_log.py`.

## Why a Service + Tray instead of one tray app

The Service runs as `LocalSystem` so it can write `options.json` under
`C:\ProgramData`, write `HKLM` keys, and watch the logs dir regardless
of the calling user. The tray app runs in the user's session for UI.
Closing the tray icon doesn't stop uploads — the service keeps running.

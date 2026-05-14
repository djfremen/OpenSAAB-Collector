# Architecture

```
                    ┌──────────────────────────────────────────────┐
                    │            EliteBook (Windows 10)            │
                    │                                              │
   Tech2Win  ──┐    │  C:\Program Files (x86)\CHIPSOFT_J2534_…\    │
               ├──► │   CSTech2Win.dll  (← OpenSAAB shim)          │
               │    │   CSTech2Win_real.dll  (← genuine)           │
               │    │                                              │
               └──► │  every D-PDU call logged to                  │
                    │   %TEMP%\cstech2win_shim_<ts>.log            │
                    │                                              │
   TrionicCAN─┐     │   j2534_interface.dll  (← OpenSAAB shim)     │
   Flasher    ├──►  │   j2534_interface_real.dll  (← genuine)      │
   etc.       │     │                                              │
              └──►  │  every PassThru* call logged to              │
                    │   %TEMP%\j2534_shim_<ts>.log                 │
                    │                                              │
                    │  ┌─────────────────────────────────────────┐ │
                    │  │   OpenSAABCollector  (Windows Service)  │ │
                    │  │                                         │ │
                    │  │  FileSystemWatcher → settle 30s         │ │
                    │  │     → gzip → POST /ingest/shim-log      │ │
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
                    │         <source>_<wall_ms>.log.gz            │
                    │         <source>_<wall_ms>.meta.json         │
                    │                                              │
                    │   GET /api/health  → community_captures: N   │
                    │   GET /            → landing page (live N)   │
                    └──────────────────────────────────────────────┘
                                  │
                                  ▼
                    ┌──────────────────────────────────────────────┐
                    │   github.com/djfremen/OpenSAAB               │
                    │     commands/saab/*.yaml  (catalog)          │
                    │     docs/saab_ecu_address_map_*.md           │
                    │     tools/decode_gmw3110_dtc.py              │
                    └──────────────────────────────────────────────┘
```

## Component responsibilities

### Shim DLLs (Chipsoft_RE repo)

Native C, x86. They have to be DLLs because they intercept calls to
`CSTech2Win.dll` and `j2534_interface.dll`. Each call is forwarded to
the genuine renamed DLL (`*_real.dll`) and a log line is written to
`%TEMP%\<shim-name>_<timestamp>.log`.

The CSTech2Win shim is at
[`Chipsoft_RE/shim/cstech2win/`](https://github.com/djfremen/Chipsoft_RE/tree/main/shim/cstech2win).
The j2534 shim is at
[`Chipsoft_RE/shim/j2534/`](https://github.com/djfremen/Chipsoft_RE/tree/main/shim/j2534).

### Collector Service (this repo)

.NET 8 Windows Service. `BackgroundService` with a `FileSystemWatcher`
on `%TEMP%`. Settle window is 30 seconds — once a shim log hasn't
been touched for that long, we treat it as ready to upload.

Why 30 seconds? Tech2Win sometimes pauses for several seconds between
diagnostic operations within one menu (e.g. the engine ECM responsePending
delay observed at ~1.5s). 30s is large enough that we don't upload
mid-flow but small enough that the user doesn't have to wait minutes
to see their upload land.

### Tray app (this repo)

WinForms tray icon. Three functions:
- **Toggle Upload Enabled**: writes `HKLM\SOFTWARE\OpenSAAB\Collector\UploadEnabled`
- **Open log folder**: opens `%TEMP%`
- **Show install GUID**: read-only display for support contact

Service polls registry on the next iteration, so changes take effect
within ~5s.

### Server (saab-security-api repo)

FastAPI on Koyeb. One route added for ingestion:

```python
@app.post("/ingest/shim-log")
```

50 MB cap per upload, gzip required, validated headers, persists to
disk under `uploads/<install-id>/`. Sidecar JSON written next to each
log with full metadata.

## Why a Service + Tray instead of one tray app

The Service runs as `LocalSystem` so it can write to `HKLM` keys and
read all of `%TEMP%` (which the user-mode tray app might not have full
access to depending on how `%TEMP%` was permissioned). The tray app
runs in the user's session for UI affordances.

This split also means closing the tray icon doesn't stop uploads —
the user has to actively toggle the registry flag or uninstall.

## Sync between concurrent shim logs

Both shims write a `wall_clock_ms` column on every log line (FILETIME
since Unix epoch / 10000). Same source on both — directly comparable.
A future merge tool can `cat cstech2win_shim_*.log j2534_shim_*.log
| sort -t '|' -k 2 -n` to get a unified timeline across both APIs.

# Legacy Chipsoft Collector (0.4.1)

Windows installer that switches on the **Chipsoft J2534 Pro driver's own
diagnostic logging** and ships the resulting logs to the OpenSAAB
protocol catalog. While you use Tech2Win or any J2534 client normally,
the genuine driver records every diagnostic call. With your opt-in
consent, captures upload to the OpenSAAB ingestion service and feed
the catalog at <https://github.com/djfremen/OpenSAAB>.

> **Status:** alpha — internal dogfooding on the bench EliteBook only.
> Historical internal installer; use the portable track for new contributors.

> **v0.4.0 — the DLL-shim model is retired.** Earlier versions replaced
> `CSTech2Win.dll` (and optionally `j2534_interface.dll`) with logging
> shims. v0.4.0 no longer touches the Chipsoft folder at all: it just
> sets `LogLevel: 0` in the driver's `options.json` and harvests the
> logs the driver writes itself. Far less to install, nothing to back
> up or restore, and immune to Tech2Win/Chipsoft updates. Upgrading from
> a pre-v0.4.0 install automatically restores the genuine Chipsoft DLLs.

## How it works

1. The Collector service writes `LogLevel: 0` into
   `C:\ProgramData\CHIPSOFT_J2534\options.json`. This enables the
   Chipsoft driver's built-in Boost.Log sink at maximum (trace)
   verbosity. Existing keys in `options.json` are preserved untouched.
2. The genuine driver writes a timestamped log per session into
   `C:\ProgramData\CHIPSOFT_J2534\logs\<YYYYMMDD>_<HHMMSS>.log`.
3. The `OpenSAABCollector` Windows Service watches that folder. When a
   session ends and the driver releases its log file, the service
   gzips it and — if upload consent is given — POSTs it to
   `https://openSAAB.com/ingest/shim-log`.
4. A tray app lets you toggle upload on/off, tail the live log, force a
   flush, open the log folder, and see the captures-uploaded counter.

## What gets installed

1. A Windows Service `OpenSAABCollector` (to `C:\Program Files\OpenSAAB\Collector`).
2. A tray app (same folder; auto-starts at login).
3. A registry-backed install state at `HKLM\SOFTWARE\OpenSAAB\Collector\`
   holding the per-install GUID, opt-in flag, upload counter, and
   (optional) vehicle profile.

Nothing is copied into the Chipsoft program folder. The only thing the
Collector changes outside its own install dir is the single `LogLevel`
key in the driver's `options.json`.

## Requirements

- Windows 10 (x64)
- Chipsoft J2534 Pro adapter installed at the standard path
- Tech2Win or any J2534 client (TrionicCANFlasher, OpenPort, etc.)

## Build (developer)

The Service + Tray are .NET 8 projects. The installer is InnoSetup 6.

```pwsh
# Service + Tray
cd src
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Installer (requires Inno Setup 6 + iscc.exe on PATH)
cd ..\installer
iscc opensaab-collector.iss
# Produces: installer\Output\opensaab-collector-setup-0.4.1.exe
```

Or run the end-to-end build script: `pwsh installer\build-installer.ps1`.

## Uninstall

Three equivalent paths — pick whichever:

1. **Start Menu** → "OpenSAAB Collector" group → "Uninstall OpenSAAB
   Collector."
2. **Settings → Apps → Installed apps** → search for "OpenSAAB
   Collector" → "Uninstall."
3. **Run directly:** `C:\Program Files\OpenSAAB\Collector\unins000.exe`.

The uninstaller stops + deletes the `OpenSAABCollector` service, kills
the tray app, and removes installed files + the
`HKLM\SOFTWARE\OpenSAAB\Collector\` registry tree.

It does **not** revert the driver's `LogLevel`. If you want the Chipsoft
driver to stop logging after uninstall, set `LogLevel` to `10` (or
delete the line) in `C:\ProgramData\CHIPSOFT_J2534\options.json`.

## USBPcap fallback

USBPcap-based raw USB capture was an alternate path in v0.2.x–v0.3.x.
v0.4.0 drops it from the installer — the driver's native logging is the
single capture path. If native logging ever comes back empty on a given
machine, USBPcap remains usable as a **manual** fallback; see
[`docs/usbpcap-fallback.md`](docs/usbpcap-fallback.md).

## Privacy

- **Opt-in.** Logs stay local by default. Upload requires explicit
  per-install consent recorded in the registry.
- **You can pause anytime** via the tray app — the service keeps
  watching but doesn't upload.
- **Preview before send** — the tray app's live console shows you the
  log activity; "Upload pending logs now" is an explicit action.

Read the full disclosure at [`docs/privacy.md`](docs/privacy.md). The
text shown in the installer's consent page is at
[`installer/consent.txt`](installer/consent.txt).

## License

Apache 2.0 — see `LICENSE`.

Not affiliated with SAAB, GM, Tech2Win, or Chipsoft.

# OpenSAAB Collector

Windows installer that drops two DLL shims into your existing Chipsoft
J2534 Pro install. While you use Tech2Win or any J2534 client normally,
the shims log every diagnostic call to `%TEMP%\`. With your opt-in
consent, captures upload to the OpenSAAB ingestion service and feed
the protocol catalog at <https://github.com/djfremen/OpenSAAB>.

> **Status:** alpha — internal dogfooding on the bench EliteBook only.
> No public release yet.

## What gets installed

1. Two shim DLLs into `C:\Program Files (x86)\CHIPSOFT_J2534_Pro_Driver\`:
   - `CSTech2Win.dll` (replaces the genuine; backup at `CSTech2Win_real.dll`)
   - `j2534_interface.dll` (replaces the genuine; backup at `j2534_interface_real.dll`)
2. A Windows Service `OpenSAABCollector` that watches `%TEMP%\` for
   `cstech2win_shim_*.log`, `j2534_shim_*.log`, and `fremsoft_*.log`,
   gzips them on rotation, and POSTs to `https://openSAAB.com/ingest/shim-log`.
3. A tray app: pause/resume upload, open log folder, **live console**
   (raw color-coded), **decoded console** (live scapy dissection),
   show install GUID.
4. `fremsoft-decoder.exe` — PyInstaller-bundled scapy/UDS dissector
   that the tray pipes the freshest log through. **GPL-2.0** (scapy);
   shipped as a separate process — see
   [`installer/decoder/NOTICE.md`](installer/decoder/NOTICE.md) for the
   licensing rationale. The other binaries remain Apache 2.0.
5. A registry-backed install state at `HKLM\SOFTWARE\OpenSAAB\Collector\`
   holding the per-install GUID, opt-in flag, and (optional) vehicle
   profile.

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
# Produces: installer\Output\opensaab-collector-setup-0.1.0.exe
```

The installer expects the shimmed DLLs to be present at
`..\..\Chipsoft_RE\shim\cstech2win\build\CSTech2Win.dll` and
`..\..\Chipsoft_RE\shim\j2534\build\j2534_interface.dll` — build those
in the Chipsoft_RE repo first.

## Privacy

- **Opt-in.** Logs stay local by default. Upload requires explicit
  per-install consent recorded in the registry.
- **You can pause anytime** via the tray app — the service keeps
  watching but doesn't upload.
- **Preview before send** — the tray app shows you the most recent log
  that would be uploaded; you can cancel.
- **Uninstall in two clicks** — restores the genuine Chipsoft DLLs
  byte-for-byte.

Read the full disclosure at [`docs/privacy.md`](docs/privacy.md). The
text shown in the installer's consent page is at
[`installer/consent.txt`](installer/consent.txt).

## License

Apache 2.0 — see `LICENSE`.

Not affiliated with SAAB, GM, Tech2Win, or Chipsoft.

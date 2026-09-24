# OpenSAAB Collector

Local USB capture tools to help document diagnostic adapters for OpenSAAB.

## Start here: contributor captures

Use the [portable capture guide](portable/README.md) for the manual Wireshark
workflow and a local-only PowerShell helper preview. Start recording before
Tech2Win, then capture adapter initialization, VIN, ECM information and DTC reads.
Keep captures private until reviewed; they can contain VINs and device serials.

The helper selects one USB device, keeps full packets, records action timestamps
and validates the saved capture. It does not replace adapter drivers, install a
service, send diagnostic commands or upload files.

**Status:** developer preview. Fifteen fixture checks pass on EliteBook with
Windows 10 / Windows PowerShell 5.1. A Chipsoft bench DTC read produced a device-scoped capture. The full interactive
helper stop flow and Windows 8.1/Mongoose validation remain pending. Use the
manual Wireshark workflow for contributors until that walkthrough is complete.

- [Capture instructions](portable/README.md)
- [Contributor reply](docs/CONTRIBUTOR_REPLY_2026-09-23.md)
- [Validation checkpoint](docs/CAPTURE_TRACK_2026-09-23.md)

## Existing Chipsoft work

The 0.4.1 service/tray implementation is preserved in `src/` and `installer/`.
It uses Chipsoft's native driver logging; it retired the earlier DLL shims.
It targets Windows 10 and .NET 8 and is not the Mongoose contributor package.
See [legacy implementation documentation](docs/LEGACY_CHIPSOFT.md).
Historical installers are retained for maintainers, not recommended downloads.

## Development

Run `tests/portable.Tests.ps1` in PowerShell. Raw captures, local session files
and credentials must never be committed. Keep adapter-specific decoding separate
from the capture layer and verify request/response traffic before claiming success.

License: [Apache 2.0](LICENSE). USBPcap, Wireshark and OEM drivers are separate
projects with their own licenses and are not bundled in the portable package.

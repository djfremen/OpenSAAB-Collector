# OpenSAAB Collector

A simple Windows tool: **set up USBPcap → capture your adapter → stop → review → upload
privately to OpenSAAB when you choose.** No shims, driver DLL changes or background service.
Wireshark is not required.

## Capture and upload

See the **[desktop app guide](desktop/README.md)** and
[release downloads](https://github.com/djfremen/OpenSAAB-Collector/releases).
Use your existing adapter driver and Tech2Win installation. Start with a short
initialization/VIN recording, then ECM information and DTC reads. Select only
your adapter and keep it connected throughout the session.

**Stop capture** saves and validates the recording locally. It never uploads,
including when the recording time/size limit is reached. Use **Open saved files**
to review or edit the capture and metadata, then **Upload** to select a completed
folder and confirm sending it. Upload also retries a failed send. Each upload
rebuilds the ZIP from the current files, so a cached ZIP cannot undo your edits.
Collector does not automatically scrub packet contents. See the
[review instructions](desktop/README.md#review-before-upload).
No Cloudflare passwords or storage keys are in the app.

**Validation:** Windows 10 / Chipsoft Pro capture and private R2 round-trip tested.
Windows 8.1 / Mongoose remains to be tested. See the
[current checkpoint](docs/AUTOMATED_COLLECTOR_2026-09-23.md) for deployment and UI
walkthrough status. The first public 0.5 build is a preview.

- [Contributor instructions](desktop/README.md)
- [Capture privacy notice](docs/CAPTURE_PRIVACY.md)
- [Server deployment](server/README.md)
- [Manual Wireshark fallback](portable/README.md)

## Development and history

`desktop/` builds with the installed Windows .NET Framework compiler.
`server/` contains the upload route and its tests. Adapter-specific decoding stays
separate from capture; a packet count alone does not prove successful diagnostics.
Raw captures and credentials must never be committed.

The previous Chipsoft native-log service is preserved in `src/` and `installer/`
for historical reference. It is not part of the new app. Versions before 0.4 used
DLL shims; 0.4.1 retired those. Historical installers are maintainer-only drafts.
The earlier local-only PowerShell helper remains in `portable/` as a preview;
its console stop problem is superseded by the new desktop capture engine.

License: [Apache 2.0](LICENSE). USBPcap and OEM drivers are separate projects
with their own licenses. Collector downloads USBPcap from its official release;
it does not bundle or modify the vendor driver.

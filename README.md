# OpenSAAB Collector

A simple Windows tool: **set up USBPcap → choose adapter model and USB device → capture → stop → review → upload
privately to OpenSAAB when you choose.** No shims, driver DLL changes or background service.
Wireshark is not required.

[Join the openSAAB Discord](https://discord.gg/FxnFe8vQht) for release updates, help and testing feedback. Share capture summaries in **#testing-feedback**; keep raw captures and private vehicle data in the reviewed private upload flow.

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
The **Sanitize capture** action creates a separate copy and a report of supported
VIN/name/serial text redactions. It does not guarantee anonymity: split identifiers,
other encodings and security exchanges may remain. See the
[review instructions](desktop/README.md#review-before-upload).
No Cloudflare passwords or storage keys are in the app.

**Validation:** Windows 10 / Chipsoft Pro capture and private R2 round-trip were
qualified on 0.5.0. Public 0.5.5 fixes physical-adapter selection behind external
USB hubs, with passing Windows CI and verified public download hashes. The
Windows 8 Pro x32 / MongoosePro GM II contributor upload exposed the bug but
contains only hub controls; a corrected hardware capture remains pending. See the
[hub-fix release notes](docs/HUB_DEVICE_SELECTION_0.5.5.md) and maintained
[Collector progress matrix](docs/COLLECTOR_PROGRESS.md) for exact-build evidence,
prior failures and remaining hardware qualification. Owner-only Nano research
is recorded separately from public features.

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

## Adapter labels and tablet controls (0.5.6)

The new required model dropdown contains **Chipsoft, MDI, Mongoose and Nano**.
It labels saved folders, upload ZIP filenames and capture metadata independently
of the physical USB capture-device picker. Names include the UTC start time and
a random suffix, e.g. `OpenSAAB_MDI_20261004_073025Z_abcd1234.zip`.
Sanitized copies keep the label; legacy captures continue to work without being
relabelled. Larger controls, scrolling and on-screen keyboard buttons improve
tablet use. See [the implementation and validation record](docs/ADAPTER_LABELS_0.5.6.md).
Model selection records what the contributor chose; it is not an adapter-support
claim. Actual tablet touch and per-adapter diagnostics still need hardware tests.

Collector now has **Sanitize capture**: create a separate local copy, review what was masked, then decide whether to upload.

### What changed

- **Stop capture** saves locally; nothing is uploaded automatically.
- **Sanitize capture** masks supported VIN-shaped text and supplied VIN, computer-name and adapter-serial values. Step notes and adapter/device descriptions are removed by default.
- The original capture stays unchanged. The report shows occurrence counts, changed packets and change locations without recording the original identifiers.
- **View report** reopens the report. **Upload** shows it again and requires **Upload this copy** confirmation before sending to OpenSAAB.
- Changes to a sanitized copy invalidate its report; sanitize again before uploading. The report stays local.

### Install and use

Download **OpenSAAB-Collector.exe** below and run it as administrator. Keep your existing adapter driver, Tech2Win and USBPcap installation. No new service or installer is added, and existing captures are retained.

Start capture before opening Tech2Win → record a short initialization/VIN session → **Stop capture** → **Sanitize capture** → review the report → **Upload** only when ready. Check the suggested computer name, especially for captures imported from another PC. A known VIN and adapter serial are optional matching inputs.

### Privacy limits

**This is a limited text sanitizer, not a guarantee of anonymity.** It covers complete contiguous ASCII/UTF-16 strings within individual USB payloads and supported metadata/notes. VIN-shaped matches are candidates, not validated VINs. Identifiers split across packets or diagnostic messages, other encodings, unknown names/serials, other personal data and security seed/key exchanges may remain. Zero matches does not mean there is no private data.

Packet framing and lengths are preserved, but edited diagnostic payloads may no longer be valid or suitable for replay. Review before sharing and keep raw captures out of public posts. See the included guide and privacy notice.

### Validation

The exact release executable was checksum-verified and tested on the EliteBook running Windows 10. All **35 native desktop assertions passed**, including original-file preservation, counts, PCAP/USB header preservation and stale/missing report rejection. The synthetic capture's on-screen counts matched, and the options/report/upload-confirmation dialogs were visually checked. Upload was cancelled; no sample was sent. Windows CI also passed those desktop checks and **10 server tests**.

This release did not add a new live capture-to-sanitized-upload round-trip. Windows 8.1/Mongoose remains untested. Unsigned developer preview; no expanded adapter-compatibility claim.

Executable SHA-256: `9bc988afeac4c41524ac46da5e6a6661cb47e9e683b4fc5da142b69a1200e31a`.
Build source: `fbbc4a4d62017f9443b39a3a55a50baf127dd343`; merged release source: `4a335c33ad3af2805314ae3949487330b9069397` (identical tree).

Public release: https://github.com/djfremen/OpenSAAB-Collector/releases/tag/v0.5.3-preview.1

All four public release assets were downloaded without GitHub authentication and matched their staged hashes. The executable is the exact CI artifact tested on the EliteBook. Source and tag point to the identical merged tree.

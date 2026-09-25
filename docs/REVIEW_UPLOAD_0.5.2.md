Collector now lets you review a capture before sending it.

- **Stop capture** saves locally. It does not upload, including when the recording time or size limit is reached.
- **Open saved files** opens the capture folder for review and editing.
- **Upload** selects a reviewed folder and asks for confirmation. Use it for retries too.
- Every upload builds a new ZIP from the current files, so an old ZIP cannot send information removed after a failed attempt. The PCAP checksum is updated automatically.

Download **OpenSAAB-Collector.exe**, run it as administrator, and keep your existing adapter driver and USBPcap installation. No installer or background service is added. Your existing captures are retained.

### Review and privacy

Edit `usb.pcap`, `session.json` and `actions.jsonl` in the selected folder—not `capture.zip`. You can blank entered descriptions and remove note lines. Raw USB packets can also contain VINs, serials and security data; inspecting or editing those requires a capture-aware tool. **Collector does not automatically scrub personal data.** Structural validation does not certify anonymization. See the attached guide and privacy notice.

Versions 0.5.0 and 0.5.1 automatically attempted upload when recording ended. Use 0.5.2 for the separate review step.

### Validation

Windows CI build and server tests passed. The exact executable below was launched and visually reviewed on the EliteBook running Windows 10. All 12 synthetic bundle checks passed there, including edited packet data, removed metadata/notes, fresh ZIP creation, stable retries and rejection of invalid or missing files. The owner approved the interface for release. A new live capture/redaction/upload round-trip was not independently recorded for 0.5.2; the capture engine and upload endpoint are unchanged from the earlier tested workflow.

Unsigned developer preview. Windows 8.1/Mongoose remains untested. No expanded adapter-compatibility claim.

Executable SHA-256: `b7d13ec9526bafd1a236f5bce6ebf5997f4b847f8f2410e75fbc42eb8b6a61f3`.
Source: merged PR #4. Build source `eaca73766bcd1ded14bf940ccf0f5adaa3bfb77f`; release source `6aec99cb83ea6fd4f9c9ad213ab79243d86b04d7` has the same tree.

Public release: https://github.com/djfremen/OpenSAAB-Collector/releases/tag/v0.5.2-preview.1

The public executable was downloaded without GitHub authentication and matched the tested SHA-256. All four published asset digests match the staged executable, guide, privacy notice and checksum list.

# Collector 0.5.6: adapter provenance and tablet controls

User request, October 4 Pacific: offer only Chipsoft, MDI, Mongoose and Nano in
a model dropdown, then burn that selection into capture filenames so submissions
can be logged by adapter. The Windows 8 Pro x32 tablet report also described
non-functional touch; the scope of that touch failure remains unknown.

## Behavior

- A required fixed model selector is separate from the USB device selector.
  Neither defaults to a guessed hardware identity. Start requires both choices.
- A capture freezes its selected model at start. Its folder and ZIP are named
  `OpenSAAB_<model>_<yyyyMMdd_HHmmss>Z_<8 lowercase hex>`, with `.zip` for the
  package. Time is UTC capture start, not upload time. No person identifier,
  VIN, serial or free-text note enters the filename.
- `session.json` gains paired `adapter_model` and `capture_id` fields. The
  sanitizer retains them when removing optional descriptions and notes.
  Malformed, unknown, partial or model/time-mismatched identities fail validation.
- Old captures remain accepted with `capture.zip`. Changing the current selector
  never renames or relabels an existing capture. Internal ZIP entry names remain
  stable; only the enclosing folder/package carries the friendly filename.
- Retrying an unchanged reviewed package preserves both its friendly filename
  and exact ZIP bytes. Server receipts/storage keys remain `OSCAP-<SHA256>`;
  private object metadata and Content-Disposition retain the adapter filename.
  The server verifies those fields by HEAD before confirming an upload. The
  client also verifies the returned friendly filename for identified uploads.
- Main/sanitizer forms scroll and actions wrap. Larger action and text controls
  and explicit Windows keyboard buttons retain normal mouse/keyboard use.
  Send remains an explicit confirmed action; Enter does not authorize upload.

## Code map

| Responsibility | File |
|---|---|
| Four models, UTC IDs, safe/legacy filenames | `desktop/CaptureIdentity.cs` |
| Freeze selection, package, upload receipt | `desktop/Collector.cs` |
| Scroll/wrap layout and keyboard access | `desktop/CollectorLayout.cs` |
| Sanitizer identity preservation | `desktop/CaptureSanitizer.cs` |
| Sanitizer/review UI | `desktop/SanitizeDialog.cs` |
| Server validation, private naming/read-back | `server/collector_api.py` |
| Server capabilities | `server/collector_entrypoint.py` |

## Validation and rollout

Offline identity/parser checks and C#5/.NET Framework cross-compilation precede
native Windows CI. Native tests cover both selector orders, missing selections,
busy locking, 800×600/360×360 reachability, package identity/byte-stable retries,
legacy packages and sanitizer preservation. Server tests use a fake store and
cover valid models, malformed identities, legacy uploads and storage corruption.
These are synthetic/software checks, not new hardware qualifications.

The upload service must accept the optional identity fields before publishing
the client; the previous validator rejects extra fields. Overlay only the two
Collector Python files onto the verified current immutable service image,
preserving website/application files, runtime identity and environment. Verify
legacy and labelled synthetic upload receipts against private stored bytes and
friendly-name metadata. Keep raw evidence private; no vendor commands or vehicle
traffic are required for this rollout.

Windows 8 Pro x32/Mongoose live capture, actual digitizer input and each adapter's
diagnostic success remain separate pending tests. The existing 0.5.5 hub picker
fix stays in this build. Nano research is not Chipsoft implementation or MDI
qualification. Dropdown membership alone establishes none of these capabilities.

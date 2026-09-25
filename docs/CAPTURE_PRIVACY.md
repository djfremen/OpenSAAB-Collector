# Collector 0.5 capture privacy notice

Applies to the new desktop capture app; consent identifier `collector-capture-v1`.
The older service's policy in `privacy.md` does not describe this app.

In 0.5.2 and later, you select one USB device and acknowledge the capture privacy
notice before recording. Collector does not record until you click Start.
**Stop capture**, a normal worker exit, or reaching the recording time/size limit
saves locally only. Nothing is uploaded automatically. Use **Open saved files**
to review or edit, then **Upload** to select a folder and separately confirm
sending its current files. You can cancel and keep everything local.

Versions 0.5.0 and 0.5.1 used **Stop & upload** and automatically attempted upload
when recording ended. Upgrade before relying on this review step.

No Windows service or background collection is installed. The separate USBPcap
driver remains installed until you remove it.

The upload contains:
- The selected device's USB packet capture, including descriptors, requests and
  replies. This may include VIN, device serials and diagnostic/security exchanges.
- Capture start/stop times, Windows version, Collector version, selected USB hub
  and address, and the adapter/car description you optionally enter.
- Timestamped step notes you choose to add.

Collector does not intentionally capture other USB addresses. Do not select a
keyboard, camera or unrelated peripheral. Keep the selected adapter plugged in;
reconnecting can change its address. Raw captures are not automatically redacted.

Bundles travel over HTTPS to OpenSAAB, then into Cloudflare R2 under the private
`opensaab-capture` bucket. Project maintainers use them to analyze adapter behavior
and build/test support. The public upload endpoint does not allow listing or
reading captures. No Cloudflare credentials are distributed in Collector.

Your local files are retained, including if an upload fails. A confirmed receipt
means storage was verified, not that the recording proves a particular diagnostic
function worked. **Upload** also retries a failed send, with confirmation each
time. It builds a fresh snapshot of `usb.pcap`, `session.json` and `actions.jsonl`,
validates the PCAP structure and updates its checksum. Cached ZIPs are not reused.
The wire consent identifier remains `collector-capture-v1`; upload authorization
is now obtained at the explicit confirmation, not at capture start.

Collector does not automatically find or remove personal information. See the
[review instructions](../desktop/README.md#review-before-upload). Structural
validation is not proof of anonymization. Editing a local copy does not remove
an original that you have already uploaded.

No automatic server retention/deletion period is currently configured. To request
deletion, contact the maintainer through the repository and provide your receipt
identifier privately; do not attach the raw capture or VIN to a public issue.
Raw captures are not published in the repository or forum. Any later sharing of
raw files requires separate permission. Derived protocol documentation should
omit personal identifiers.

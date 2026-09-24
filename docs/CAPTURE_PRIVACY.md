# Collector 0.5 capture privacy notice

Applies to the new desktop capture app; consent identifier `collector-capture-v1`.
The older service's policy in `privacy.md` does not describe this app.

Before recording, you select one USB device and agree that its completed capture
will be uploaded privately to OpenSAAB. Collector does not record until you click
Start. Stop & upload (or the recording time/size limit) finishes the recording
and automatically attempts upload. No Windows service or background collection
is installed. The separate USBPcap driver remains installed until you remove it.

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
function worked. Retry saved upload resends a completed bundle with its existing
consent; byte-identical retries use the same storage key.

No automatic server retention/deletion period is currently configured. To request
deletion, contact the maintainer through the repository and provide your receipt
identifier privately; do not attach the raw capture or VIN to a public issue.
Raw captures are not published in the repository or forum. Any later sharing of
raw files requires separate permission. Derived protocol documentation should
omit personal identifiers.

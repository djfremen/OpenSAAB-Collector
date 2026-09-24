# Contributor capture track checkpoint — 2026-09-23

The existing Collector was recovered from the private GitHub repository. Its
README/specification lag behind its implementation: the service currently uses
hub-wide USB capture with a 96-byte snap length and Chipsoft-specific shims.
The legacy service/installer remains unchanged. It must not be represented as
a generic Mongoose or Windows 8.1 package.

Added a separate portable capture helper and a manual Wireshark guide. The
helper requires an explicit current hub/device address, requests full payloads,
records action timestamps, checks actual process exit, validates classic USBPcap
file structure, reports truncated packets, and calculates a SHA-256. Files remain
local; there is no upload endpoint, service or diagnostic command execution in
this track. Connection descriptors are requested for the selected device.
Reconnects require reselection; no hub-wide fallback is performed.

Validation: 15 checks passed using PowerShell 7 on macOS ARM64, covering interface
and device parsing, invalid-selection rejection, spaced filename quoting, pcap
structure/empty/partial/wrong-link cases, truncated-payload detection and script
syntax. A signed/unsigned comparison issue in the pcap magic check was caught
and fixed. These are fixture tests, not Windows 8.1, USBPcap-driver or Mongoose
hardware qualification. Contributor deployment should begin with the documented
manual workflow until the helper has a real Windows smoke test.

Next: Windows PowerShell 4 smoke test, contributor's initialization/VIN sample,
then ECM information/DTC reads. Device IDs and native transport implementations
remain per-adapter concerns. No automatic upload or security capture is enabled.

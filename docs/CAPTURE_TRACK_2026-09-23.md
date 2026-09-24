# Contributor capture checkpoint — 2026-09-23

Recovered both the old default branch and the newer `v0.4.0-no-shim` branch.
The latter contains the 0.4.1 Chipsoft native-driver logging implementation,
which is now preserved alongside the new portable track. The historical service
is Windows 10/.NET 8 and must not be presented as a Mongoose/Windows 8.1 package.
Legacy installer builds are manual-only to avoid publishing them with portable
updates. Historical installer downloads are retained as maintainer drafts.

The portable helper requires an explicit hub/device address, requests full
payloads and descriptors, records timestamps, checks process exit, validates
classic USBPcap structure, reports truncated packets and computes SHA-256.
It does not upload, install a service, replace DLLs or send diagnostic commands.
Reconnects require reselection; no hub-wide fallback is performed.

## Tests

- PowerShell 7 / macOS ARM64: 15 fixture checks pass.
- EliteBook, Windows 10 Pro 19045 / Windows PowerShell 5.1: the same 15 checks pass.
- Installed USBPcap 1.5.4.0: real interface and device enumeration passes after
  fixing process invocation. Direct PowerShell `&` invocation returned no output
  because USBPcap is a Windows-subsystem executable. The helper now uses an
  explicit process with redirected output, exit-code checking and a 10-second
  query timeout.
- The real device list identifies the adapter at address 4 on USBPcap1. Windows
  PnP identifies VID 0483 / PID 5740. Generic serial names alone are insufficient.
- The test used a process-scoped RemoteSigned policy; no persistent execution
  policy was changed.

Fixture checks cover parsing, invalid selections, filename quoting, pcap format,
empty/partial/wrong-link captures, truncated payloads and syntax. These checks do
not establish diagnostic success. Windows 8.1/PowerShell 4 and Mongoose hardware
remain untested. A contributor should begin with the manual Wireshark workflow.

## Publication review

An all-refs Git bundle and release metadata backup were made before publication.
Gitleaks scanned all Git history with zero findings. No raw capture files are
included in the portable package. The old 0.4 installers remain in historical
Git commits; their source installer lists the project's service/tray and consent
text, not OEM driver binaries. The legacy EXEs were not independently rebuilt
or fully unpacked during this review and are not recommended contributor tools.

## Live EliteBook bench result

The Chipsoft native J2534 read-only probe completed a fresh diagnostic session,
read six DTC records and completed cleanup. USBPcap recorded 450 packets / 22,264
bytes with no truncated packets. All records belonged to selected USB address 4;
control and bulk input/output were present. The DTC request bytes were found in
the capture. Two nonzero USB completion statuses were also present; a structurally
valid capture does not imply every transfer succeeded. Raw files stay private.

This tested the capture backend around the existing Windows DTC probe, **not** a
complete Tech2Win GUI walkthrough or a Mongoose session. The headless SSH test
could not gracefully stop USBPcap via console control and needed process
termination. A second run also completed six-code DTC read/cleanup, but capture
termination was again forced. The helper now creates a separate console wrapper
and instructs the local operator to use USBPcap's documented `q` command. That
interactive stop path still needs a local-desktop walkthrough before releasing a
recommended helper download. No portable binary release is being advertised yet.

The pre-existing Collector service had automatic uploading enabled. It was
stopped for these tests and remains stopped to avoid uploading new diagnostic
logs without review; its startup configuration and upload setting were not
changed. No test capture was uploaded by this workflow.

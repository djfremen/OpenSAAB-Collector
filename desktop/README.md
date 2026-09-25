# OpenSAAB Collector 0.5

A standalone Windows capture-and-upload application. **No shims, driver DLL
changes, background service or diagnostic commands. Wireshark is not required.**

## Contributor workflow

1. Download `OpenSAAB-Collector.exe` from this repository's Releases page.
2. Run it as administrator. If USBPcap is missing, choose **Set up USBPcap**.
   Collector downloads the official USBPcap 1.5.4.0 installer and verifies its
   pinned SHA-256 before launching it. Complete that installer and restart Windows.
3. Reopen Collector. Select your USB adapter from the list. If you are unsure,
   compare the list before and after plugging in the adapter, then refresh.
   Leave your existing Mongoose, Chipsoft or other manufacturer's driver installed.
4. Acknowledge the capture privacy notice, then choose **Start capture** before opening
   Tech2Win. Keep the adapter connected throughout the recording.
5. Use Tech2Win normally: select the adapter, read VIN, ECM information and DTCs.
   **Add step note** records menu/action notes with timestamps. Begin with a short
   initialization/VIN session. No need to clear codes, program or unlock anything.
6. Choose **Stop capture**. Collector closes and validates the capture and saves
   it locally. Nothing is uploaded.
7. Choose **Open saved files** and review the files as described below.
8. Choose **Upload**, select the reviewed capture folder and confirm. Collector
   rebuilds the bundle from the current files and uploads it privately. It shows
   confirmation only after verifying the server receipt matches the sent file.

Files remain in `Documents\OpenSAAB-Captures`. If the network or server is down,
use **Upload** again later. Closing the app is blocked while capturing or
uploading. Each recording is limited to 15 minutes or approximately 60 MiB;
reaching a limit stops and saves locally, without upload.
An interrupted or structurally incomplete capture stays local and is not uploaded.

### Review before upload

Only these three files from the folder you select are packaged:

- `usb.pcap`: raw USB packets. Personal identifiers and security data can be in
  packet payloads and device descriptors, not just readable text. Collector has
  no built-in packet viewer or automatic redaction. Use a capture-aware editor
  to inspect or remove packets; save back as classic USBPcap PCAP, not PCAPNG.
  Removing exchanges can reduce the capture's usefulness for adapter development.
- `session.json`: capture metadata. You can blank the `adapter` and `device_label`
  string values to remove entered descriptions or device serials. Retain the
  JSON field names and capture-format fields. The capture checksum is recalculated
  automatically in the new upload bundle after changes to `usb.pcap`.
- `actions.jsonl`: your notes. Remove individual complete lines, edit their text
  while preserving JSON, or leave the file empty to omit all notes.

Edit these files, **not `capture.zip`**. Every upload builds a fresh ZIP; it never
reuses an old ZIP from a failed attempt. Other local files, including worker
configuration and upload receipts, are not included. Save and close your edits
before choosing Upload. You may copy a capture folder and upload the edited copy.

Validation checks capture structure, not whether personal information remains.
If you are not comfortable sharing the reviewed contents, cancel Upload and keep
it local. A new upload does not delete a previously submitted original; contact
OpenSAAB privately with its receipt if that original needs deletion.

Captures can include VINs, adapter serials and security traffic. Only select your
adapter; do not select other peripherals or share raw captures publicly.
The application contains an HTTPS upload address, **no Cloudflare credentials**.

## Compatibility and validation

- Uses Windows Forms and .NET Framework APIs available in Windows 8.1 (.NET 4.5.1).
  No .NET 8 installation or Wireshark installation is needed.
- Tested capture engine on Windows 10 x64 / EliteBook / Chipsoft Pro: normal stop,
  468 packets, 174 bulk payload packets, no truncated records. Independent bench
  probe returned six DTC records with session cleanup.
- The resulting 4,132-byte ZIP was validated and saved to private R2 through the
  new server endpoint running locally. Public deployment, Windows client upload and the desktop walkthrough also passed;
  details are recorded in `docs/AUTOMATED_COLLECTOR_2026-09-23.md`.
- Windows 8.1 and Mongoose are **not yet hardware-validated**. A preview release
  must not be represented as universal adapter support.

## Build

On Windows, run `desktop\build.ps1` in Windows PowerShell. It uses the installed
.NET Framework C# compiler and creates `desktop\bin\OpenSAAB-Collector.exe`.
`WorkerTest.cs`, `UploadTest.cs` and `BundleTest.cs` are developer harnesses, excluded from the app.
Run `desktop\test.ps1` to check fresh packaging, edited captures and invalid-input rejection.
The release executable is currently unsigned.

## Capture engine

The unmodified USBPcap executable receives an exact device address, descriptor
injection and full packet snap length. Its output goes through a duplex Windows
named pipe into the session file. Closing that pipe uses USBPcap's broken-pipe
shutdown path, including when the adapter is idle. A native pipe writer is used
so USBPcap's overlapped I/O cannot interfere with the .NET completion port.
Forced termination is a failure, never an upload success. Packet counts show
capture content, not successful vehicle diagnostics.

The old Chipsoft service remains in `src/` and `installer/` solely as legacy
source. Stop it before recording with 0.5; it has a separate legacy uploader.

Official USBPcap: <https://desowin.org/usbpcap/>
Source for pipe shutdown: <https://github.com/desowin/usbpcap/blob/1.5.4.0/USBPcapCMD/thread.c>

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
4. Confirm the private upload notice, then choose **Start capture** before opening
   Tech2Win. Keep the adapter connected throughout the recording.
5. Use Tech2Win normally: select the adapter, read VIN, ECM information and DTCs.
   **Add step note** records menu/action notes with timestamps. Begin with a short
   initialization/VIN session. No need to clear codes, program or unlock anything.
6. Choose **Stop & upload**. Collector closes the capture, validates the pcap,
   packages it with session details and notes, then uploads to OpenSAAB's private
   Cloudflare R2 storage. It shows confirmation only after the server verifies
   storage and the client verifies the receipt matches its file.

Files remain in `Documents\OpenSAAB-Captures`. If the network or server is down,
use **Retry saved upload** later. Closing the app is blocked while capturing or
uploading. Each recording is limited to 15 minutes or approximately 60 MiB;
reaching a limit stops and uploads the completed recording automatically.
An interrupted or structurally incomplete capture stays local and is not uploaded.

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
`WorkerTest.cs` and `UploadTest.cs` are developer harnesses, excluded from the app.
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

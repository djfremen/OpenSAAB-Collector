# Automated Collector checkpoint

- New Windows Forms 0.5 capture-and-upload implementation is in `desktop/`.
- No shim, DLL replacement, registry logging changes or new background service.
- USBPcap is downloaded from the official signed-installer release with pinned
  SHA-256. Wireshark is optional for analysis and not needed for collection.
- Existing private R2 credentials authenticated successfully without modification.
  Existing `opensaab-capture` bucket is accessible. No secret values are in source.
- New upload server tests: 10 passing (validation, consent, checksum, unexpected
  files/device, truncation, ZIP expansion limit, rate limits, storage failure and
  retry identity).
- EliteBook Windows 10 / Chipsoft Pro: new pipe-based capture engine stopped with
  exit code 0. Fresh bench DTC read: complete=1, records=6, cleanup=1. Capture:
  468 packets, 23,106 bytes, 174 bulk payload packets, 0 truncated packets.
- That capture was packaged as a 4,132-byte ZIP, accepted by the local deployment
  image, and stored/HEAD-verified in private R2. Raw evidence remains private.
- Public endpoint deployed successfully to the existing website service. Deployment
  `e7093d07-8549-47f1-a35c-510abc3fc83e`, image digest
  `sha256:25701166a81aa96c6f4c418586f33e09eef0cf7ac0ca1c78f7b00656f430e64b`.
  Existing website, health and denied legacy routes retain their behavior.
- Windows uploader sent the same bundle through the public HTTPS endpoint and
  verified the returned receipt. Private R2 read-back matched the source bytes;
  an anonymous S3 read was denied (HTTP 400).
- Desktop walkthrough then passed: select address 4, enter adapter description,
  accept upload notice, Start capture, add step note, run read-only DTC probe,
  Stop & upload. UI showed confirmed upload and retained local files. That fresh
  capture contained 474 packets; separate probe again returned six DTC records.
- USBPcap was already installed on the EliteBook. Official installer download,
  checksum and Authenticode checks were tested separately; a clean driver
  installation/reboot on Windows 8.1 remains pending.
- Windows 8.1 + Mongoose validation remains pending; current tests use Chipsoft.

## Published preview

PR #2 merged into main (`9de8bdb`). The public release is
<https://github.com/djfremen/OpenSAAB-Collector/releases/tag/v0.5.0-preview.1>.
Anonymous download was verified against the tested EliteBook executable:
`6c4bd3ac120f9d11a4082d63554535f7995d9d5eae3ebda8393d87478fa94e11`.
GitHub Actions passed both the Windows build and server tests. No raw bench
captures or stored credential values were committed or included in release assets.
The desktop walkthrough upload contained 474 packets, 177 bulk payload packets,
and a 3,729-byte bundle. A verified receipt was displayed and saved locally.

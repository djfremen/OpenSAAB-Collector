# OpenSAAB Collector — Privacy Policy

**Version:** v1
**Effective:** 2026-05-13

## TL;DR

- Logs stay on **your computer by default**. Upload requires opt-in.
- Each install gets an **anonymous random GUID**. We don't ask for or
  store your name, email, or phone number.
- Captured logs **include your VIN and SecurityAccess seed/key bytes**
  for the duration of each diagnostic exchange. Don't opt in to upload
  if that's not OK with you.
- We **don't redistribute raw logs** publicly. Derived YAMLs (Tech2
  menu wire format, etc.) get published to
  <https://github.com/djfremen/OpenSAAB>.

## What the Collector does

Two DLL shims sit between your J2534-compatible diagnostic software
(Tech2Win, TrionicCANFlasher, etc.) and the genuine Chipsoft adapter
DLLs. Every diagnostic call passes through us; we forward it verbatim
to the genuine DLL and write a log line to `%TEMP%\`.

A Windows Service (`OpenSAABCollector`) watches `%TEMP%\` for these
logs. When a diagnostic session ends and the log file rotates, the
service:

1. Reads the rotated log.
2. Gzips it in memory.
3. If `UploadEnabled` is set in the registry: HTTP POSTs to
   `https://openSAAB.com/ingest/shim-log` with these headers:
   - `X-Install-ID`: your random per-install GUID
   - `X-Capture-Source`: `cstech2win` or `j2534`
   - `X-Consent-Version`: `v1`
   - `X-Vehicle-Year` / `X-Vehicle-Model`: optional, only if you
     entered them
   - `X-Collector-Version`: the build of this Collector

If `UploadEnabled` is not set, nothing is sent over the network. The
log file stays on your computer and is yours to keep, share, or delete.

## What's in a captured log

A shim log records every diagnostic API call your software made:

- The CAN-ID (which ECU is being addressed)
- The UDS service ID (`$1A` ReadDataByIdentifier, `$27` SecurityAccess,
  etc.) and full byte payload
- For J2534 captures: hardware timestamps from the Chipsoft adapter
  (microsecond precision)
- Your vehicle's **VIN** whenever the diagnostic flow reads it
  (typically `$1A 90` returns the 17-byte ASCII VIN)
- **SecurityAccess seeds and computed keys** when an ECM is unlocked
  (for example, the bench engine ECM seed `0xC4DC` and key `0x4EED`)

## What we do with your uploads

If you opted in:

- Logs are stored on the OpenSAAB server (currently a Koyeb instance)
  under `uploads/<your-install-guid>/<source>_<wall_ms>.log.gz`.
- A sidecar JSON next to each log records the headers you sent
  (capture source, consent version, vehicle profile if any).
- Logs are read by maintainers to:
  - Identify new Tech2 menu actions to catalog as
    `commands/saab/<action>.yaml` in the OpenSAAB repo
  - Cross-validate existing YAMLs against more vehicles / ECUs / model
    years
  - Decode DTCs against the [z90.pl SAAB DTC catalog](https://z90.pl/saab/dtc/)
- **Derived YAMLs are published** with sample bytes redacted to a
  generic `vehicle_profile: SAAB 9-3 (bench, 20XX)` — no VIN, no
  seed/key pair tied to a specific vehicle.
- **Raw logs are NOT redistributed publicly** unless we get explicit
  additional consent from the contributor and the VIN is anonymised.

## What we don't do

- We don't sell your data.
- We don't share your install GUID with third parties.
- We don't run analytics, ads, or tracking pixels on the logs.
- We don't keep your personal info because we don't ask for it.
- We don't claim copyright on the bytes the Chipsoft adapter or your
  ECMs produced — those are facts about your car.

## Your rights

- **Pause uploads anytime** via the tray icon. The service keeps
  watching but doesn't send.
- **Stop and delete the install GUID** by uninstalling the Collector.
  The genuine Chipsoft DLLs are restored byte-for-byte.
- **Request deletion of your past uploads** by emailing the maintainer
  with your install GUID. We'll delete everything indexed under that
  GUID.

## Contact

GitHub: <https://github.com/djfremen/OpenSAAB-Collector/issues>

## Changes

We'll bump `Consent Version` (currently `v1`) if the data we collect
or how we use it changes materially. Existing installs will keep
their `v1` consent — we won't auto-promote consent across versions.

> Legacy service policy. For the new 0.5 capture app, see [Capture privacy](CAPTURE_PRIVACY.md).

# OpenSAAB Collector — Privacy Policy

**Version:** v2
**Effective:** 2026-05-22

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

As of v0.4.0 the Collector does **not** modify or intercept any Chipsoft
DLL. It switches on the genuine Chipsoft driver's own built-in logging
by setting `LogLevel: 0` in `C:\ProgramData\CHIPSOFT_J2534\options.json`.
The genuine driver then writes a timestamped log of every diagnostic
session into `C:\ProgramData\CHIPSOFT_J2534\logs\`.

A Windows Service (`OpenSAABCollector`) watches that folder. When a
diagnostic session ends and the driver releases its log file, the
service:

1. Reads the completed log.
2. Gzips it in memory.
3. If `UploadEnabled` is set in the registry: HTTP POSTs to
   `https://openSAAB.com/ingest/shim-log` with these headers:
   - `X-Install-ID`: your random per-install GUID
   - `X-Capture-Source`: `chipsoft`
   - `X-Consent-Version`: `v2`
   - `X-Vehicle-Year` / `X-Vehicle-Model`: optional, only if you
     entered them
   - `X-Collector-Version`: the build of this Collector

If `UploadEnabled` is not set, nothing is sent over the network. The
log file stays on your computer and is yours to keep, share, or delete.

## What's in a captured log

A driver log records every diagnostic API call your software made:

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
  Uninstalling removes the service, tray app, and registry tree. It
  leaves the driver's `LogLevel` setting as-is — to turn the driver's
  logging back off, set `LogLevel` to `10` (or delete the line) in
  `C:\ProgramData\CHIPSOFT_J2534\options.json`.
- **Request deletion of your past uploads** by emailing the maintainer
  with your install GUID. We'll delete everything indexed under that
  GUID.

## Contact

GitHub: <https://github.com/djfremen/OpenSAAB-Collector/issues>

## Changes

We'll bump `Consent Version` (currently `v2`) if the data we collect
or how we use it changes materially. Existing installs will keep
their prior consent version — we won't auto-promote consent across
versions.

**v1 → v2** (2026-05-22): retired the DLL-shim collection model. The
Collector no longer modifies any Chipsoft DLL; it enables the genuine
driver's own logging by setting `LogLevel: 0` in `options.json`. The
data captured is unchanged — same wire bytes, same VIN exposure, same
SecurityAccess seed/key — but the collection mechanism is now strictly
less invasive. `X-Capture-Source` is now `chipsoft` instead of
`cstech2win` / `j2534`.

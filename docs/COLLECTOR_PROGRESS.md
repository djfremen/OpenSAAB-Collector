# OpenSAAB Collector progress

Generated from `COLLECTOR_PROGRESS.json`; edit that register, then run `python3 tools/collector_progress.py`. Never edit this projection directly.

Updated: 2026-10-01 19:54 PDT. Public preview: **0.5.3-preview.1**; owner research: **0.5.4-owner.2**.

Collector capture tooling; diagnostic transports and model advice require separate qualification.

## Adapter matrix

Public columns refer to the current preview unless explicitly labelled earlier build. General synthetic sanitizer checks do not qualify any adapter's live sanitized upload.

| Adapter | Capture | Stop | Live review/sanitize | Upload + read-back | Owner decode | Owner model advice |
|---|---|---|---|---|---|---|
| Chipsoft Pro | Earlier build passed | Earlier build passed | Not tested | Earlier build passed | Not tested | Not tested |
| VCX Nano (exact variant pending) | Not tested | Not tested | Not tested | Not tested | Scoped checks passed | Assessment rejected |
| Mongoose (exact model pending) | Not tested | Not tested | Not tested | Not tested | Not tested | Not tested |
| GM MDI / MDI 2 | Not tested | Not tested | Not tested | Not tested | Not tested | Not tested |
| Other USB adapters | Not tested | Not tested | Not tested | Not tested | Not tested | Not tested |

### Chipsoft Pro

Environment: Windows 10 x64 / EliteBook. Evidence: `chipsoft-050-bench`.

Next test: Run a fresh 0.5.3 initialization, VIN and read-only DTC capture; stop, optionally sanitize a copy, review, explicitly upload and independently verify stored bytes. Record driver and adapter firmware.

Complete when: Exact 0.5.3 digest, selected-device traffic, graceful cleanup, original preservation, matching receipt/read-back and private evidence; no claim of every module working.

### VCX Nano (exact variant pending)

Environment: Windows 10 x64 / EliteBook; x86 VXDIAG driver 0.4.0.4; owner bench. Evidence: `nano-channel-only`, `nano-manager-startup`, `nano-warm-vin`, `nano-owner-reboot`, `nano-model-rejection`, `nano-owner-native`, `nano-owner-gui-preflight`, `nano-owner-gui`.

Next test: Document complete adapter power removal; capture before Manager, followed by a separately bounded original-firmware handoff. Record exact adapter variant and firmware.

Complete when: GUI consent/local summary verified; repeatable cold-start evidence and separately qualified handoff. Public capture/upload and reliable model advice need their own tests.

### Mongoose (exact model pending)

Environment: Windows 8.1 planned; not tested. Evidence: none recorded.

Next test: First check Windows 8.1 dependencies and USBPcap installation/reboot with the exact model and vendor driver; collect a short working-vendor-session recording.

Complete when: Exact build/model/driver/firmware evidence for selection, payload capture, graceful stop, review and confirmed private upload/read-back.

### GM MDI / MDI 2

Environment: Windows USB profile planned; not tested. Evidence: none recorded.

Next test: Create separate MDI and MDI 2 USB profiles with driver/firmware versions and a known working vendor session. Do not apply Nano categories to their packets.

Complete when: Independent per-model capture/stop/review/upload evidence. Network operation requires a separate capture method.

### Other USB adapters

Environment: Case-by-case qualification. Evidence: none recorded.

Next test: Add an exact-model profile before recording; check existing vendor driver, USB address and actual payloads. Start with identification/read-only diagnostics.

Complete when: Each model needs an exact-artifact case. A generic USB capture capability does not establish every adapter or an OpenSAAB diagnostic backend.

## Next actions

- **P1 · done · owner-gui:** Installed owner.2 picker, local summary, consent cancellation, cached rejection and unfinished-capture handling verified on EliteBook; no new model call or vehicle command.
- **P1 · pending · nano-cold:** Record full Nano power removal and initialization before Manager; then a separately bounded firmware-handoff test.
- **P1 · pending · public-roundtrip:** Qualify the public 0.5.3 live Chipsoft capture/review/sanitized-upload/read-back path.
- **P2 · pending · adapter-onboarding:** Collect exact-model Windows/driver/firmware profiles for Mongoose, MDI and other adapters; keep statuses untested until evidence exists.
- **P2 · pending · model-corpus:** Add labelled failures and missing-evidence cases before expanding model advice or public access.

## Evidence by exact artifact

### 0.5.0-preview.1 · public

Source: `9de8bdb`. Executable SHA-256: `6c4bd3ac120f9d11a4082d63554535f7995d9d5eae3ebda8393d87478fa94e11`.

Evidence reference: docs/AUTOMATED_COLLECTOR_2026-09-23.md.

- **chipsoft-050-bench · bench · 2026-09-23 / release September 23 PDT · pass_scoped:** Windows 10 x64 EliteBook: real capture stopped gracefully; 468 records, zero truncation. Separate read-only probe read six DTC records. Public HTTPS upload receipt and private storage read-back verified. Separate GUI recording had 474 records and retained local files. Collector itself sent no diagnostic commands. Gap: Driver and firmware versions not recorded here. USBPcap was already installed; clean driver installation/reboot remains untested.

### 0.5.1-preview.1 · public

Source: `not recorded`. Executable SHA-256: `not recorded`.

Evidence reference: docs/BRANDING_0.5.1.md.

- **branding-051 · ui · 2026-09-23 PDT · pass_scoped:** Branding, icon and voluntary Donate action checked on Windows 10 EliteBook. Gap: No fresh live capture/upload on this exact artifact. Source revision and executable digest need backfill from release evidence.

### 0.5.2-preview.1 · public

Source: `6aec99cb83ea6fd4f9c9ad213ab79243d86b04d7`. Executable SHA-256: `b7d13ec9526bafd1a236f5bce6ebf5997f4b847f8f2410e75fbc42eb8b6a61f3`.

Evidence reference: docs/REVIEW_UPLOAD_0.5.2.md.

- **review-052 · synthetic_ui · 2026-09-24 PDT · pass_scoped:** Twelve synthetic bundle checks and review/upload UI verified on Windows 10 EliteBook; stop saves locally and explicit upload rebuilds current files. Gap: No fresh live capture/review/upload round-trip on 0.5.2.

### 0.5.3-preview.1 · public

Source: `4a335c33ad3af2805314ae3949487330b9069397`. Executable SHA-256: `9bc988afeac4c41524ac46da5e6a6661cb47e9e683b4fc5da142b69a1200e31a`.

Evidence reference: docs/SANITIZE_REVIEW_0.5.3.md.

- **sanitize-053 · synthetic_ui_ci · 2026-09-25 PDT · pass_scoped:** Exact release digest verified on Windows 10 EliteBook. Thirty-five native assertions, UI counts/options/report/upload confirmation, and ten CI server tests passed. Confirmation cancelled; no test capture uploaded. Gap: No live capture-to-sanitized-upload round-trip on 0.5.3. Limited contiguous-text sanitizer can miss identifiers and security payloads; edited data is unsuitable for replay.

### 0.5.4-owner.1 · owner_only

Source: `not recorded`. Executable SHA-256: `930798ad926ab79ef963ca8a8639359c9b577d021e081f8fc5c2c43d625d3974`.

Evidence reference: owner-local: Nano bench and reboot receipts, October 1.

- **nano-channel-only · bench · 2026-10-01 16:59 PDT · pass_scoped:** Channel-only vendor path captured 122 records, 27 paired bulk transfers and 13 matched headers, zero frame errors and ten pending polling reads; graceful stop. No ECU diagnostic transmit calls. Earlier descriptor-only recordings did not establish payload capture. Gap: Owner-reported adapter reboot did not establish complete power removal. First Manager comparison failed launch/preflight and SSH access; no diagnostic or license qualification.
- **nano-manager-startup · bench · 2026-10-01 17:11 PDT (stop) · pass_scoped:** Recovered Manager launch without reinstall or firmware/license changes. Startup capture: 192 records, 63 paired bulk transfers, six matched headers, zero frame errors, ten pending reads; graceful stop. No beep command observed. Gap: License indicator changes not assessed. Opaque startup operations unresolved; matched headers do not establish authorization or ECU success.
- **nano-warm-vin · bench · 2026-10-01 17:12 PDT · pass_scoped:** Installed Windows 0.2.14 VIN-only helper and independent wire check agree on one positive bench VIN reply. Capture: 184 records, 30 paired bulk transfers, 19 matched headers, zero frame errors; cleanup and graceful stop verified. Nine synthetic research-decoder tests passed. Gap: Ten incoming completions lacked matching submissions; ten polling reads pending at stop. Owner-reported ECM model and power removal unverified. No full GUI/firmware handoff, security access or public Nano support.
- **nano-owner-reboot · bench · 2026-10-01 18:12 PDT · pass_scoped:** Fresh Windows reboot independently established. Manager absent during capture. Candidate assembly methods produced one matching positive bench VIN exchange and cleanup. Capture: 198 records, 40 paired bulk transfers, 19 matched headers, zero frame errors, ten pending polling reads at stop. Gap: Headless candidate route, not full emulator handoff. Adapter power removal and boot enumeration not captured; ECM model is owner reported. Driver, firmware and exact Nano variant remain unrecorded in this register.
- **nano-model-rejection · model_evaluation · 2026-10-01 PDT · rejected:** One counts-only JEV call inferred unsupported ECU success, Manager dependency and full diagnostics. Whole assessment rejected. Local VIN wire evidence is assessed separately. Gap: No accuracy qualification. Scores are not calibrated probabilities; labelled success/failure/missing-evidence corpus remains pending.

### 0.5.4-owner.2 · owner_only

Source: `593dcc0`. Executable SHA-256: `f8dce6be96b7bca5256b1b0395ecadae04ac340a6e0aa7436a51569a5c4fe814`.

Evidence reference: owner-local: WINDOWS_OWNER_CATEGORY_RECEIPT_2026-10-01.json.

- **nano-owner-native · synthetic_and_saved_capture · 2026-10-01 18:47 PDT · pass_scoped:** Installed artifact digest verified. Fifty-one existing plus 65 native category/response checks and 27 Python tests passed. Nineteen synthetic cross-language cases and the saved reboot capture matched. Native client rejected saved unsupported model assessment; zero new model requests or vehicle commands. Public-mode build excludes owner classes. Gap: GUI button walk-through pending Mini unlock. Categories remain provisional; command return status and ECU outcome decoding absent. Original capture unchanged; analysis does not sanitize it.
- **nano-owner-gui-preflight · installed_cli_preflight · 2026-10-01 19:16 PDT · pass_scoped:** Mini UI unlocked and SSH reachable. Installed owner.2 digest verified; actual analyzer reproduced saved capture summary and rejected a recording-state fixture. Exact summary has a complete cached rejected JEV result, so a later GUI check can avoid a new model call. Installed x86 VXDIAG / ALLScanner VCXPT32.dll file version 0.4.0.4 recorded. No new model request, vehicle command or raw upload. Gap: EliteBook remains at Windows password screen. Folder selection, visible summary, cancellation and cached-result GUI checks remain untested. Adapter firmware and exact Nano variant remain unknown.
- **nano-owner-gui · installed_gui_saved_capture · 2026-10-01 19:54 PDT · pass_scoped:** Actual installed owner.2 picker cancellation, displayed local summary, explicit consent cancellation, cached rejected-result display and unfinished-capture rejection passed. Disabled request button remained inert. Original capture and executable hashes, provider budget and request inventory unchanged; zero new provider calls, raw uploads or vehicle commands. Gap: Saved-capture GUI verification only. Cached rejection is not model accuracy. Category mappings provisional; return-status and ECU outcome decoding absent. Full adapter power removal and original-firmware handoff remain unqualified.

## Limits

- Capture, diagnostic communication, sanitizer coverage, upload storage and model accuracy are separate qualifications.
- Public 0.5.3 has no JEV feature. Owner.2 is private research; its bridge needs the Mini and SSH tunnel. No model controls the adapter or CAN loop.
- USBPcap captures USB traffic only. Bluetooth and network paths require other capture mechanisms.
- No raw captures, VINs, seeds, credentials, contact details, notes or device serials belong in this register or dashboard. Private receipts and payload evidence stay private.
- No field-user success is claimed by these bench and synthetic tests. Capture totals are not people, unique cars or full diagnostic success.
- Historical shim/native-log implementations remain archived and are not features of the current public USBPcap application.

## Maintaining the record

Append a case for every meaningful build, test, failed attempt or field receipt. Keep prior results and gaps; never replace a failed model evaluation with a later retry. A new release begins unqualified until exact-artifact evidence exists. Record unknown driver/firmware values as unknown, not inferred from a model name.

For each adapter record exact make/model/variant, Windows edition/build/architecture, vendor driver and firmware version, Collector version/source/executable digest, USB transport and selected address, bench/field/synthetic context, Pacific arrival/test time, operation and failure stage, capture integrity/pairing, cleanup/stop, review/consent, private upload receipt and read-back, local decoding/model result, gaps, next test and completion criteria. Keep identifiers and raw evidence in a private receipt; this register contains only reviewed safe summaries.

One family can have several profiles: do not collapse MDI 1/2 or Nano variants into a universal support claim. A new adapter starts with every stage untested. Owner-only tests cannot qualify public builds. A successful capture is useful evidence even when diagnostics failed.

After editing, run the renderer and inspect the diff. On the Mini, run `/Users/mini4/Documents/Projects/djfremen-dashboard/.venv/bin/python /Users/mini4/Documents/Projects/djfremen-dashboard/tools/sync_local_dashboard.py` to regenerate the dedicated Collector card and dashboard JSON from this same register. This is an explicit maintenance step; it does not upload captures, publish a release, or run background model requests.

Earlier retired shim/native-log progress is preserved in the repository history and dated documentation. It does not qualify the current USBPcap application.

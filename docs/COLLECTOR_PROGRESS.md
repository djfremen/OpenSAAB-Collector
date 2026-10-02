# OpenSAAB Collector progress

Generated from `COLLECTOR_PROGRESS.json`; edit that register, then run `python3 tools/collector_progress.py`. Never edit this projection directly.

Updated: 2026-10-01 23:29 PDT. Public preview: **0.5.3-preview.1**; owner research: **0.5.4-owner.2**.

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

Environment: Windows 10 x64 / EliteBook; x86 driver 0.4.0.4, adapter-reported firmware 1.9.4.2; owner bench. Evidence: `nano-channel-only`, `nano-manager-startup`, `nano-warm-vin`, `nano-owner-reboot`, `nano-model-rejection`, `nano-owner-native`, `nano-owner-gui-preflight`, `nano-owner-gui`, `nano-acceptance-blindspot`, `nano-warm-firmware-handoff`, `nano-jev-structure`, `nano-jev-priority`, `nano-validity-live-trace`, `nano-paired-recorder`, `nano-coverage-tooling`, `nano-coverage-model-review`, `nano-coverage-model-routing`, `nano-beep-reference`, `nano-direct-beep`.

Next test: Confirm audible outcome and documented full power removal; repeat own-transport identification/beep before vendor applications. Continue receive-pool and changing startup-prelude analysis independently, then qualify bounded own CAN channel and read-only ECM identification.

Complete when: Repeatable call-boundary mapping is recorded; payload/status interpretation still needs independent labels and controls. Cold-start, full firmware handoff, public capture/upload and model accuracy require separate evidence.

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

- **P1 · done · nano-direct-beep:** Own sender verified Nano identity and two beep success replies with no VCX library loaded; original Collector captures independently validate exchange. Audibility and cold start are still unverified.
- **P1 · pending · nano-read-pool-coverage:** Seven-recording comparison and JEV routing favor receive-pool lifecycle inspection. Ten pending IN reads occur even with fully paired completions; do not treat them as proof of loss/crash. Whole-vehicle reference and loss counters still required for bounded CAN coverage.
- **P1 · pending · nano-validity-semantics:** Reusable paired USB/SDK recorder repeats all five startup exchanges inside three successful validity calls. Twenty-five export hooks reveal no send/receive-export event inside them; next locate a reviewed lower transport boundary and determine input/reply handling without changing vendor checks.
- **P1 · done · owner-gui:** Installed owner.2 picker, local summary, consent cancellation, cached rejection and unfinished-capture handling verified on EliteBook; no new model call or vehicle command.
- **P1 · pending · nano-firmware-policy:** Warm firmware startup and positive ECM VIN verified; engine entry blocked by local read-only command policy. Review data-definition semantics before any policy extension and separately test ECM information/DTC.
- **P1 · pending · nano-cold:** Confirm complete USB/bench power removal and capture before any vendor application. First repeat own-transport identity/beep, then separately qualify diagnostic initialization; warm direct beep does not settle cold-start dependency.
- **P1 · pending · public-roundtrip:** Qualify the public 0.5.3 live Chipsoft capture/review/sanitized-upload/read-back path.
- **P2 · pending · adapter-onboarding:** Collect exact-model Windows/driver/firmware profiles for Mongoose, MDI and other adapters; keep statuses untested until evidence exists.
- **P2 · pending · model-corpus:** Model fact assessments remain rejected, including unsupported dropped-CAN inference. Typed experiment routing succeeds narrowly. Preserve these failures and build labelled missing-evidence/error cases before public advice or accuracy claims.

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
- **nano-acceptance-blindspot · source_review_and_synthetic · 2026-10-01 PDT · pass_scoped:** New regression proves different hypothetical command reply status bytes produce identical schema-2 category summaries; acceptance and ECU outcome remain unassessed. All 28 owner Python checks pass. Retained source review identifies separate vendor SDK return checks, without establishing wire status semantics. Test/plan commit 8d68a72; installed executable unchanged. Gap: This verifies the current summary limitation, not a reply-status decoder or model accuracy. Owner is off-site; adapter power state unchanged. Physical cold-start and firmware handoff remain pending.
- **nano-warm-firmware-handoff · bench_gui_and_independent_wire · 2026-10-01 20:19–20:23 PDT · pass_scoped:** Exact private Windows 0.2.16 candidate reached original SAAB NAO 9.250 Welcome/Main Menu, accepted arrow/Enter navigation and selected the 2004 NG 9-3 profile. Fresh launcher VIN and a separate positive ECM VIN exchange inside firmware independently matched the saved observation. Firmware showed key-position OK, then its data-definition command was stopped by the local read-only policy before transmission. Owner.2 capture: 6,628 packets, zero truncation/frame errors/USB bulk errors. Driver cleanup, released exclusive lock, cleared display and graceful capture stop verified; no forced termination, provider call or upload. Adapter-reported firmware 1.9.4.2; x86 driver 0.4.0.4. Gap: Engine entry did not complete; ECM information and DTC reading unqualified. Ten incoming completions lack matching submissions and ten reads remain pending at stop. Warm bench only; owner-reported ECM identity, exact adapter variant and full power removal unverified. No public Nano support or model accuracy qualification.
- **nano-validity-live-trace · bench_instrumentation_and_independent_usb · 2026-10-01 20:54–20:55 PDT · pass_scoped:** Two fresh owner.2 channel-only captures correlate observed SDK entry/exit with USB: ordinal 23 contains A0 then 84; ordinal 50 contains A0 then A1; ordinal 51 contains A2; CAN open follows. All three SDK returns and channel open return zero. Existing Frida 17.11.0 records no arguments/buffers and does not change returns. Each capture: 108 packets, 13 matched headers, zero frame/USB bulk errors; connect, disconnect, close, released adapter lock and graceful Collector stop verified. Exact Collector/driver/SDK hashes rechecked. No diagnostic transmit API, provider request or raw upload during tracing. Gap: Warm bench only. Ten unmatched incoming completions and ten pending reads per recording. Millisecond correlation maps call boundaries, not payload semantics or wire return-status decoding. First file invocation was blocked before collection; normal permitted command invocation then succeeded without policy change. Physical cold start and full diagnostics remain unqualified.

### research-2026-10-01 · owner_only

Source: `7b686fe`. Executable SHA-256: `not recorded`.

Evidence reference: owner-local: STARTUP_STRUCTURE_JEV_2026-10-01.md.

- **nano-jev-structure · model_evaluation_saved_capture · 2026-10-01 20:48 PDT · rejected:** Compared six startup sequences across six stopped recordings; order and lengths repeat, but every request body differs. New allowlisted schema exports structure and equality counts, with reviewed static/bench facts. One actual JEV call used 2,894 input / 180 output tokens and reported $0.000121548; four evidence guards conflicted, so the assessment was rejected. No raw payload, identifier, capture hash, vendor code or credential was sent. Gap: Pre-trace baseline only; exact payload semantics and cold-start state unresolved. Numeric model scores are not calibrated here. Existing installed Collector and loopback upload schema unchanged.
- **nano-jev-priority · model_routing_and_synthetic · 2026-10-01 20:51 PDT · pass_scoped:** One actual typed-choice JEV request selected local validity-boundary tracing as the next feasible off-site experiment. Returned score 0.99 for tracing and 0.01 for beep investigation; no prohibited replay/support declaration selected. 1,623 input / 78 output tokens; reported cost $0.000068166. Forty-one owner tests passed for bounds, privacy rejection, caches, uncertainty and prohibited choices. Shared request budget/cache retained. Gap: Successful bounded routing is advisory, not protocol discovery or model accuracy qualification. Initial structured-fact assessment remains rejected. No public JEV release or automatic model control of CAN.

### paired-research-2026-10-01 · owner_only

Source: `6e04d35`. Executable SHA-256: `not recorded`.

Evidence reference: owner-local: PAIRED_USB_SDK_FINDINGS_2026-10-01.md.

- **nano-paired-recorder · bench_instrumentation_and_independent_usb · 2026-10-01 21:38 PDT · pass_scoped:** Reusable owner recorder starts unchanged Collector.2 before device open and observes 25 SDK export boundaries afterward. Fresh bench channel-only run: 110 USB records, 27 bulk payload records, zero truncation/frame/USB bulk errors, 13 matched headers. Ten SDK calls returned zero, all 20 entry/exit events complete. Repeated ordinal 23 -> A0/84, ordinal 50 -> A0/A1, ordinal 51 -> A2; channel setup and cleanup also observed. Helper exit, released adapter lock and graceful Collector stop verified. Forty-eight owner tests pass for strict event privacy, gaps, ambiguity and preservation. No diagnostic transmit API, provider call or raw upload. Installed executable unchanged. Gap: Warm bench only; ten incoming pairing gaps and ten pending polling reads. Four control frames outside traced calls; two have multiple candidate millisecond boundaries. SDK send/receive exports were not observed inside validity calls; internal transport and payload/reply semantics unresolved. SDK opening-device entry/exit untraced, though its USB traffic is recorded. Instrumentation may affect timing. This is owner tooling, not a shipped public GUI feature or all-network CAN recording.

### coverage-research-2026-10-01 · owner_only

Source: `80286c1`. Executable SHA-256: `not recorded`.

Evidence reference: owner-local: CAN_COVERAGE_JEV_2026-10-01.md.

- **nano-coverage-tooling · saved_capture_and_synthetic · 2026-10-01 22:30 PDT · pass_scoped:** New private CLI compares seven distinct stopped Collector recordings and optional SDK evidence. All seven end with ten pending IN reads; three have zero orphan input completions, four have ten. In the paired trace, six orphan read keys reappear in later submissions and five remain pending, consistent with a read-pool boundary explanation without proving it. Zero frame/USB bulk errors in these recordings. Sixty-three tests pass for privacy, missing SDK evidence, truncated input, duplicate recording weighting, pointer reuse, immutable tool facts, model guards and cache/uncertainty. Originals preserved. Gap: Request IDs are reused pointers; no durable identity or missing history is established. No capture-drop counter, independent bus reference, qualified CAN decoder or whole-vehicle coverage. This is private Mini analysis of saved Collector files; installed Windows button is unchanged. Other adapters remain untested.
- **nano-coverage-model-review · model_evaluation_saved_capture · 2026-10-01 22:24 PDT · rejected:** One actual JEV fact review selected read-pool investigation, but assigned 0.60 to dropped CAN being proven despite missing reference/drop evidence; deterministic guard rejected the full assessment. 2,344 input / 148 output tokens; reported cost $0.000098448. No raw bytes, IDs, paths, code or credentials in model state. Gap: Model fact-classification reliability remains unqualified. Rejected result is preserved; a sensible experiment choice does not validate its causal claims.
- **nano-coverage-model-routing · model_routing_saved_capture · 2026-10-01 22:26 PDT · pass_scoped:** Default workflow now fixes evidence facts in local tooling and asks JEV only to rank authored experiments. Actual typed-choice call selected read-pool inspection (0.79), followed by receive/filter audit (0.18); prohibited claims scored zero. 2,293 input / 89 output tokens; reported cost $0.000096306. Both coverage calls combined $0.000194754. Existing key cap/expiry, daily request bound, cache and uncertain-send policy retained; no automatic test execution or live CAN control. Gap: Scores are uncalibrated. Useful routing is not accuracy, a proven read-pool cause, recovered traffic, public support or completed vehicle CAN coverage. The prior fact assessment remains rejected.

### direct-beep-research-2026-10-01 · owner_only

Source: `c29f0a8`. Executable SHA-256: `not recorded`.

Evidence reference: owner-local: DIRECT_NANO_BEEP_2026-10-01.md.

- **nano-beep-reference · bench_adapter_only · 2026-10-01 22:46 PDT · pass_scoped:** Hash-qualified SDK open, two Manager-equivalent beep calls and close returned zero. Collector retained 68 records / 3347 bytes, three matching replies and zero frame/USB bulk errors. OS serial configuration measured at 921600 baud, 8-N-1, DTR/RTS enabled; graceful stop. No CAN channel or diagnostic transmit. Gap: Vendor-dependent reference. Three unpaired incoming USB completions and three pending reads; audibility and physical cold state not observed.
- **nano-direct-beep · bench_adapter_only · 2026-10-01 22:48 PDT · pass_scoped:** Own Windows serial sender generated framing/checksums, verified echo and Nano identity, and received both beep success markers. Zero VCX modules loaded in sender; CH343 OS driver remains. Collector retained 52 records / 2640 bytes, four matching replies and zero frame/USB bulk errors. Port closed, adapter lock released, capture graceful. Seventy Python and five actual C# encoder checks passed; no provider call or ECU transmit. Gap: Audible sound unconfirmed; two calls do not establish two sounds. Preceding vendor reference may have conditioned adapter. Five unpaired incoming USB completions and five pending reads. Cold initialization, changing prelude, CAN channel and full vehicle coverage unqualified; installed owner/public apps unchanged.

### android-beep-owner-0.1 · owner_only

Source: `b223c43`. Executable SHA-256: `ab1c51523c4164300350513a490d5f91ee2523ee1f298b630aa7324f5d17c496`.

Evidence reference: owner-local: ANDROID_NANO_BEEP_2026-10-01.md.

- **nano-pixel-direct-beep · bench_adapter_only · 2026-10-01 23:29 PDT · pass_scoped:** Separate Pixel owner probe received echo, validated Nano identity and two beep success markers through Android USB Host APIs. Four control returns zero; eight valid frames/four ordered exchanges. USB closed and cleanup passed; diagnostic transmit zero. Seventeen JVM framing checks passed, activity compiled and APK verified. Existing preview.30 preserved; no vendor libraries or provider call. Gap: Audibility and independent full adapter power removal unobserved. Application transfer recording is not whole-bus USBPcap or CAN reference. Identity reply took 2.309 seconds against 2.5-second bound; cause/reliability unqualified. License activation, CAN channel, ECM/VIN and public support not proven.

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

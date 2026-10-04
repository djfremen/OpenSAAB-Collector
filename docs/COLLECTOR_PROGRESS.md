# OpenSAAB Collector progress

Generated from `COLLECTOR_PROGRESS.json`; edit that register, then run `python3 tools/collector_progress.py`. Never edit this projection directly.

Updated: 2026-10-04 01:03 PDT. Public preview: **0.5.6-preview.1**; owner research: **0.5.4-owner.2**.

Collector 0.5.6-preview.1 is published with required Chipsoft/MDI/Mongoose/Nano provenance, model-labelled UTC folders/ZIPs and metadata, compatible live private upload validation and tablet layout. Native Windows software checks and legacy/labelled synthetic server read-backs passed. Actual tablet touch, on-screen keyboard launching and current-build per-adapter hardware qualification remain separate pending tests. Nano owner research retains its existing evidence and gaps.

## Adapter matrix

Public columns refer to the current preview unless explicitly labelled earlier build. General synthetic sanitizer checks do not qualify any adapter's live sanitized upload.

| Adapter | Capture | Stop | Live review/sanitize | Upload + read-back | Owner decode | Owner model advice |
|---|---|---|---|---|---|---|
| Chipsoft Pro | Earlier build passed | Earlier build passed | Not tested | Earlier build passed | Not tested | Not tested |
| VCX Nano (exact variant pending) | Not tested | Not tested | Not tested | Not tested | Scoped checks passed | Assessment rejected |
| MongoosePro GM II | Not tested | Not tested | Not tested | Not tested | Not tested | Not tested |
| GM MDI / MDI 2 | Not tested | Not tested | Not tested | Not tested | Not tested | Not tested |
| Other USB adapters | Not tested | Not tested | Not tested | Not tested | Not tested | Not tested |

### Chipsoft Pro

Environment: Windows 10 x64 / EliteBook. Evidence: `chipsoft-050-bench`.

Next test: Run a fresh 0.5.3 initialization, VIN and read-only DTC capture; stop, optionally sanitize a copy, review, explicitly upload and independently verify stored bytes. Record driver and adapter firmware.

Complete when: Exact 0.5.3 digest, selected-device traffic, graceful cleanup, original preservation, matching receipt/read-back and private evidence; no claim of every module working.

### VCX Nano (exact variant pending)

Environment: Windows 10 x64 / EliteBook; x86 driver 0.4.0.4, adapter-reported firmware 1.9.4.2; owner bench. Evidence: `nano-channel-only`, `nano-manager-startup`, `nano-warm-vin`, `nano-owner-reboot`, `nano-model-rejection`, `nano-owner-native`, `nano-owner-gui-preflight`, `nano-owner-gui`, `nano-acceptance-blindspot`, `nano-warm-firmware-handoff`, `nano-jev-structure`, `nano-jev-priority`, `nano-validity-live-trace`, `nano-paired-recorder`, `nano-coverage-tooling`, `nano-coverage-model-review`, `nano-coverage-model-routing`, `nano-beep-reference`, `nano-direct-beep`, `nano-pixel-vin-channel-rejected`, `nano-init-comparison`, `nano-windows-direct-warm-r4`, `nano-direct-adapter-reboot-r4`, `nano-direct-post-reboot-rejection-r4`, `nano-direct-physical-cold-rejection-r4`, `nano-original-startup-restores-direct-r4`, `nano-startup-observer-and-ui-failures`, `nano-manager-and-load-dll-negative`, `nano-observed-original-warm-recovery`, `nano-independent-readiness-boundary-analysis`.

Next test: Recover ordinary fresh startup request generation and reply validation, then implement them independently in shared Nano code without Tech2Win, Manager or vendor DLLs. A complete-car ignition-on comparison is proposed, not performed; confirm placement and keep it separate from bench evidence.

Complete when: Same exact native artifact starts before vendor software after independently documented loss of both USB and OBD power, reads a wire-verified VIN, cleans up, reopens under the powered state, and repeats from another cold start. Full firmware/info/DTC and public capture/upload retain separate gates.

### MongoosePro GM II

Environment: Owner-reported Windows 8 Pro x32 tablet via external USB hub; J2534 package v1.2.8.0 reported; firmware and exact Collector executable unknown. Evidence: `mongoose-win8-hub-capture-20261003`.

Next test: Download 0.5.5 and select MongoosePro GM II itself beneath the existing hub. Repeat initialization/VIN, retain the original, review/sanitize and explicitly upload; verify adapter payloads and stored bytes.

Complete when: Exact model/build/driver/firmware evidence, Mongoose rather than hub traffic, graceful stop, review and confirmed private upload/read-back. The current hub-only upload does not qualify Mongoose diagnostics.

### GM MDI / MDI 2

Environment: Windows USB profile planned; not tested. Evidence: none recorded.

Next test: Create separate MDI and MDI 2 USB profiles with driver/firmware versions and a known working vendor session. Do not apply Nano categories to their packets.

Complete when: Independent per-model capture/stop/review/upload evidence. Network operation requires a separate capture method.

### Other USB adapters

Environment: Case-by-case qualification. Evidence: none recorded.

Next test: Add an exact-model profile before recording; check existing vendor driver, USB address and actual payloads. Start with identification/read-only diagnostics.

Complete when: Each model needs an exact-artifact case. A generic USB capture capability does not establish every adapter or an OpenSAAB diagnostic backend.

## Next actions

- **P1 · done · adapter-labels-056-rollout:** Collector 0.5.6-preview.1 published from passing native main CI. Compatible upload service is live; legacy and labelled synthetic packages were verified against private stored bytes and filename metadata. Public executable and documentation downloads matched all checksums. Website update uses only its Collector section.
- **P1 · pending · mongoose-hub-picker:** 0.5.6 includes the published 0.5.5 USB hub picker fix and larger scrolling controls. Obtain a new MongoosePro GM II capture on Windows 8 Pro x32 with hub/mouse/keyboard; the earlier upload recorded hub controls only. Separately verify actual touch and keyboard-button behavior on that tablet.
- **P1 · done · nano-windows-warm-vin-and-reboot:** Exact private Windows native probe passed warm read-only VIN with independent wire/cleanup evidence and an adapter-reboot acknowledgement. Warm readiness still requires vendor startup; public Nano support remains unqualified.
- **P1 · done · nano-direct-beep:** Own sender verified Nano identity and two beep success replies with no VCX library loaded; original Collector captures independently validate exchange. Audibility and cold start are still unverified.
- **P1 · pending · nano-read-pool-coverage:** Seven-recording comparison and JEV routing favor receive-pool lifecycle inspection. Ten pending IN reads occur even with fully paired completions; do not treat them as proof of loss/crash. Whole-vehicle reference and loss counters still required for bounded CAN coverage.
- **P1 · pending · nano-validity-semantics:** Original startup controls narrow the readiness target to a changing three-step request/reply path. Recover generation and validation below mapped boundaries; current code extraction omits implementation bodies. SDK-export hooks caused measured failures. No static captured-payload replay or vendor-check modification.
- **P1 · done · owner-gui:** Installed owner.2 picker, local summary, consent cancellation, cached rejection and unfinished-capture handling verified on EliteBook; no new model call or vehicle command.
- **P1 · pending · nano-firmware-policy:** Private Windows warm VIN is qualified from our native probe. Original-firmware Engine/info/DTC remain separately pending; bench missing-module and vehicle-selection limitations are retained. Review the existing narrow data-definition guard before changing policy.
- **P1 · pending · nano-cold:** Controlled full USB/OBD power-loss test completed and direct channel opening failed before vehicle TX. Independent cold initialization is still unresolved. Implement our portable initializer and qualify two separate full-power-loss starts before any vendor program, followed by VIN and cleanup.
- **P1 · pending · public-roundtrip:** Qualify public 0.5.6 with a fresh live Chipsoft capture/review/sanitized-upload/read-back; general synthetic software checks and earlier 0.5.0 hardware evidence remain separate.
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

### android-vin-owner-0.2 · owner_only

Source: `457ff8f`. Executable SHA-256: `bf223069c2cad6c8ba67ba667953f1075607a11a41354d61318941f8a5cafc4b`.

Evidence reference: owner-local: ANDROID_NANO_VIN_2026-10-01.md.

- **nano-pixel-vin-channel-rejected · bench_readonly_vin_attempt · 2026-10-01 23:47 PDT · failed:** Unchanged installed shared Rust worker in separate owner binding verified echo and Nano identity. Channel open returned FE; stopped before VIN, zero diagnostic transmit. Twelve valid frames/six paired exchanges; filter removal returned 96, stop/close 00. USB closed and binding cleanup passed. Ten exclusion tests plus 17 framing checks passed, actual activity compiled. Main preview.30 unchanged; no provider request/upload. Gap: No VIN or CAN channel success. FE meaning unknown; do not infer license cause. Filter cleanup rejected, so channel cleanup is not a pass. Application recording is not USBPcap/independent CAN reference. Audible sound and independent full power removal remain unobserved. Earlier OEM Windows VIN is separate.

### init-comparison-cli-1 · owner_only

Source: `6ede42a`. Executable SHA-256: `not recorded`.

Evidence reference: owner-local: NANO_INIT_COMPARISON_2026-10-02.md.

- **nano-init-comparison · saved_recordings_and_model_routing · 2026-10-02 00:54 PDT · pass_scoped:** Four unchanged stopped recordings indexed privately: Windows vendor channel-only, vendor VIN, direct beep and Pixel direct VIN attempt. Same open parameters succeed in vendor channel-only but return FE on Pixel; vendor VIN uses a different third byte. Both vendor paths have the five-step changing prelude, Pixel does not. Corrected JEV routing chooses a same-host control; 81 owner tests pass. Two calls reported $0.000251034 total. Gap: No new hardware capture or command. Pixel source is an application log, not USBPcap; whole-bus gap counter unavailable. Initial model input had an incorrect data-header counter and is preserved as superseded; corrected result is separate. Returned scores are uncalibrated advice. Preconditions/FE semantics/cold-start causality remain unresolved; no public Nano support or model accuracy claim.

### nano-windows-direct-private-r4 · owner_only

Source: `7c8968c43fbfeefdcc591e6c0024df31041d69c2`. Executable SHA-256: `d1bf33f6d9265b2fb0f336a76b57af6d3613632d4fa62b92d57ec44815aaa3a4`.

Evidence reference: owner-local: VCX_NANO_WINDOWS_DIRECT_GATE_2026-10-02.md; per-run frozen binary/capture receipts.

- **nano-windows-direct-warm-r4 · bench_direct_transport · 2026-10-02 PDT · pass_scoped:** Exact private Windows native probe uses our Nano framing/channel client over OS serial. After documented original Tech2Win conditioning, one read-only VIN request plus flow control produced three ordered positive ECU frames independently matching the probe result. Shutdown statuses passed, serial closed and ownership lock reacquired. Sender imports/module observations show no vendor SDK loading, with transient-observation limits recorded. Gap: Vendor-assisted warm startup remains required; installed OS serial driver remains. This standalone probe does not qualify original-firmware Engine/info/DTC, independent cold startup, other networks or public Nano support.
- **nano-direct-adapter-reboot-r4 · bench_adapter_only · 2026-10-02 PDT · pass_scoped:** Own Windows probe verified identity, issued one measured empty adapter-reboot command and received its exact success reply. No CAN channel or vehicle traffic; serial closed and lock reacquired. Vendor Restart reference corroborates the command form. Gap: Acknowledged adapter reboot is not physical loss of both supplies or proof of persistent profile reset. Distinct reset/profile semantics remain unresolved; recovery is separately tested.
- **nano-direct-post-reboot-rejection-r4 · bench_reboot_control · 2026-10-02 PDT · failed:** Warm direct VIN pass was followed by acknowledged adapter reboot. A direct attempt more than 45 seconds later verified identity but received FE at channel opening, before vehicle TX. Filter cleanup replied 96 while stop/close replied zero; serial closed and lock reacquired. Gap: Retain the negative result and rejected filter cleanup. FE/96 meanings remain unknown; an identity or reboot acknowledgement does not qualify diagnostic readiness.
- **nano-direct-physical-cold-rejection-r4 · owner_confirmed_physical_cold_bench · 2026-10-02 PDT · failed:** Owner confirmed removal of USB and OBD/bench power. Four uninterrupted USB-absence samples spanned 10.348 seconds; fresh enumeration was recorded. Our direct client ran before vendor software and received FE on channel opening, with zero vehicle TX. Serial closed and lock reacquired. Gap: This is a qualified cold-state negative, not an independent-start pass. Power removal is owner-observed and USB absence is host-measured; complete vehicle/module coverage remains untested.
- **nano-original-startup-restores-direct-r4 · bench_vendor_reference_and_direct_result · 2026-10-02 PDT · pass_scoped:** Retained same-host control establishes direct FE, followed by normal original Tech2Win Main Menu/close, then independently wire-qualified direct VIN and cleanup from the unchanged native probe. Original startup contained no observed CAN/data TX. USBPcap phases and separate sender outcomes retained. Gap: This establishes resident warm readiness after vendor conditioning, not a vendor-free initializer. Pairing boundary gaps remain recorded; no independent bus-loss counter or full CAN coverage claim.
- **nano-manager-and-load-dll-negative · bench_two_vendor_comparison_controls · 2026-10-02 PDT · failed:** After measured adapter reboot and negative direct baselines, normal Manager launch/green license view did not restore direct readiness. A separate Manager plus installed PASSTHRU helper, VXDIAG interface selection and successful Load DLL also left direct opening at FE before vehicle TX. Both helpers closed normally; capture intervals and cleanup verified. Gap: The installed helper has no Tech2 interface entry, so this is explicitly a variation from the reviewed video workflow. Device Open/Connect were not tested. No explicit beep command was observed; autonomous sound is not excluded and audible confirmation remains separate.

### nano-startup-controls-private-2026-10-02 · owner_only

Source: `76f18c09d5ceafc4dc5beee355dd48b8a0d04283`. Executable SHA-256: `not recorded`.

Evidence reference: owner-local: NANO_COLD_START_AUTOMATION_2026-10-02.md; individual run manifests preserve tested snapshots.

- **nano-startup-observer-and-ui-failures · bench_failed_observation_controls · 2026-10-02 PDT · failed:** SDK-export interception produced startup failures; Windows events identify the instrumentation agent and exception. Untraced and attachment-only startup controls passed. A later manually operated startup missed its UI deadline and required termination; no follow-up VIN was run in that rejected case. Failed captures and receipts remain retained. Gap: Hooked and force-terminated runs cannot qualify normal startup. Interceptor fault semantics remain unresolved; the external UI wait was increased only for the later manual-control mode.
- **nano-observed-original-warm-recovery · bench_observed_original_startup · 2026-10-02 PDT · pass_scoped:** Native remote-display control brought original Tech2Win to its exact Main Menu template; the process closed normally with exit zero and no force kill. A fresh direct VIN matched the captured response and passed cleanup. Capture stopped gracefully; test tasks/processes were removed. SDK injection/hooks were disabled. Thirty-two evidence/parser tests passed on Mini and Windows for the runner tooling. Gap: This repeat began already warm after the preceding incomplete startup attempt. It is not a fresh FE-to-pass or cold-init proof. Exact per-run tooling manifests and the rejected earlier case remain separately retained.

### nano-readiness-analysis-private-2026-10-02 · owner_only

Source: `92db6ba431c0f03527ac3dd9712bcb18fc2eeedf`. Executable SHA-256: `not recorded`.

Evidence reference: owner-local: NANO_INDEPENDENT_READINESS_2026-10-02.md; offline input manifest and reviewed results.

- **nano-independent-readiness-boundary-analysis · offline_stopped_capture_and_synthetic_checks · 2026-10-02 PDT · pass_scoped:** Five distinct retained phase recordings were compared without hardware or vendor launch. Three original-startup recordings contain 30 complete adjacent cycles of the narrower three-step startup path; two Manager comparisons contain none. Fresh request payloads vary, and exact equality relationships are preserved privately. Forty-three comparison/evidence tests pass, including synthetic USB decoding and damaged, duplicate, stale and reordered inputs. Existing code extraction lacks the mapped implementation bodies. Gap: Order, lengths, replies and variability do not recover request generation/validation or prove necessity/sufficiency. Native initializer remains unimplemented. Rejected UI-startup evidence stays rejected; warm baseline and ten unmatched USB completions/ten pending reads per phase remain explicit.

### 0.5.3 (contributor reported; exact executable unverified) · public

Source: `not recorded`. Executable SHA-256: `not recorded`.

Evidence reference: private-owner: Windows 8 Mongoose hub upload review, October 3 PDT.

- **mongoose-win8-hub-capture-20261003 · contributor_capture_wrong_device · 2026-10-03 18:48 PDT arrival · failed:** Owner reports Windows 8 Pro x32 tablet, external hub with mouse/keyboard and MongoosePro GM II. Session reports Collector 0.5.3 and J2534 package v1.2.8.0. Private R2 read-back verified the stored bundle digest. Notes supplied: ECM VIN read attempted, no diagnostic result. The selected Generic USB Hub capture contains six control records, zero bulk/interrupt traffic and no Mongoose exchange. Source inspection confirmed that the picker excludes enabled physical devices behind hubs. Local parser regression and Windows executable cross-compile pass; source fix is not published. Gap: Contributor executable digest/source, firmware, Tech2Win driver version, original capture, sanitizer report and vehicle/context unknown. Graceful stop is recorded in metadata only. Mongoose diagnostics, live sanitizer behavior and corrected Windows 8/hardware capture remain unqualified.

### 0.5.5-preview.1 · public

Source: `640f1e5510afd81366658ee3719ca010705b437e`. Executable SHA-256: `1a5d7f0c01a8d5dc82d4ad259fa66b988e28147c44c68c0df84abf82c911fa85`.

Evidence reference: GitHub PR #6; Windows CI run 37182812971; private publication receipt; docs/HUB_DEVICE_SELECTION_0.5.5.md.

- **public-055-hub-picker-ci · windows_ci_and_verified_publication · 2026-10-03 23:28 PDT publication · pass_scoped:** Merged PR #6 has the same source tree as the tested PR executable. Windows CI passed the six new physical-device picker regression checks plus existing bundle/sanitizer checks; all ten server tests passed. The four public assets were downloaded anonymously and matched their staged SHA-256 values. Parser preserves devices behind one or two external hubs while excluding disabled/composite display children. Gap: No fresh Windows 8 Pro x32/Mongoose or Chipsoft live capture-to-reviewed-upload round-trip for 0.5.5. CI selection fixtures are not hardware diagnostics; the earlier hub-only failure remains preserved.

### 0.5.6 candidate · public

Source: `not recorded`. Executable SHA-256: `not recorded`.

Evidence reference: docs/ADAPTER_LABELS_0.5.6.md; desktop/test.ps1; server/test_collector_api.py.

- **adapter-labels-056-offline · synthetic_software · 2026-10-04 PDT · pass_scoped:** Four-model identity and legacy naming tests, USB picker checks and C#5/.NET Framework cross-compilation passed on the Mini. Server fake-store tests pass for legacy and labelled submissions, UTC/model validation and corrupted private metadata. Native Windows CI and service activation are the next rollout gates. Gap: No actual tablet digitizer test or new Chipsoft/MDI/Mongoose/Nano hardware capture/diagnostic qualification. This source candidate is not yet the public release.

### 0.5.6-preview.1 · public

Source: `59c7bb14f5d3e9bda3b4fa425573bde60bdccda3`. Executable SHA-256: `0da50a8e944b1acd4d94ac8d1e33b8a097352138a833f036de15b2d3cb3fc316`.

Evidence reference: https://github.com/djfremen/OpenSAAB-Collector/releases/tag/v0.5.6-preview.1; https://github.com/djfremen/OpenSAAB-Collector/actions/runs/37187449018; docs/ADAPTER_LABELS_0.5.6.md.

- **adapter-labels-056-windows-release · synthetic_software · 2026-10-04 PDT · pass_scoped:** Native Windows CI built this exact executable and passed identity, USB picker, fresh bundle/unchanged retry, sanitizer label preservation and shown-form layout/scrolling checks at 800x600 and 360x360. Both selector orders, missing choices and busy locking are covered. All four public release assets were downloaded anonymously and matched the published source files. Earlier candidate test runs exposed focus-driven scroll behavior in the test setup; revised immediate bounds and separate native scrollbar checks passed on both PR and main. Gap: No actual Windows 8 Pro x32 digitizer or on-screen keyboard launch test, and no new live adapter/vehicle capture. Dropdown choices declare contributor provenance; they do not establish diagnostic support.
- **adapter-labels-056-live-server · synthetic_software · 2026-10-04 PDT · pass_scoped:** All 35 server tests passed. Compatible API 0.5.6 became live through a two-file overlay preserving existing runtime configuration and website. Clearly marked legacy and Mongoose-labelled software fixtures uploaded successfully and were independently retrieved from private storage with byte-identical ZIPs, content-hash receipts and the exact declared filename metadata. Confirmed retry of the legacy fixture retained its object key. Gap: These two retained fixtures contain no adapter/vehicle traffic and qualify no Mongoose hardware. An unsigned S3 request returned HTTP400 without object bytes; that request did not independently verify public bucket configuration. Private deployment/upload receipts stay outside this public register.

## Limits

- Capture, diagnostic communication, sanitizer coverage, upload storage and model accuracy are separate qualifications.
- Public 0.5.3 has no JEV feature. Owner.2 is private research; its bridge needs the Mini and SSH tunnel. No model controls the adapter or CAN loop.
- USBPcap captures USB traffic only. Bluetooth and network paths require other capture mechanisms.
- No raw captures, VINs, seeds, credentials, contact details, notes or device serials belong in this register or dashboard. Private receipts and payload evidence stay private.
- No field-user success is claimed by these bench and synthetic tests. Capture totals are not people, unique cars or full diagnostic success.
- Historical shim/native-log implementations remain archived and are not features of the current public USBPcap application.
- October 2 independent Nano initialization is unresolved. Manager green licenses, beep acknowledgement, Load DLL, and successful vendor-conditioned VIN are not substitutes for a direct-first physical-cold pass. No complete-car relocation/test has been performed in these new cases.

## Maintaining the record

Append a case for every meaningful build, test, failed attempt or field receipt. Keep prior results and gaps; never replace a failed model evaluation with a later retry. A new release begins unqualified until exact-artifact evidence exists. Record unknown driver/firmware values as unknown, not inferred from a model name.

For each adapter record exact make/model/variant, Windows edition/build/architecture, vendor driver and firmware version, Collector version/source/executable digest, USB transport and selected address, bench/field/synthetic context, Pacific arrival/test time, operation and failure stage, capture integrity/pairing, cleanup/stop, review/consent, private upload receipt and read-back, local decoding/model result, gaps, next test and completion criteria. Keep identifiers and raw evidence in a private receipt; this register contains only reviewed safe summaries.

One family can have several profiles: do not collapse MDI 1/2 or Nano variants into a universal support claim. A new adapter starts with every stage untested. Owner-only tests cannot qualify public builds. A successful capture is useful evidence even when diagnostics failed.

After editing, run the renderer and inspect the diff. On the Mini, run `/Users/mini4/Documents/Projects/djfremen-dashboard/.venv/bin/python /Users/mini4/Documents/Projects/djfremen-dashboard/tools/sync_local_dashboard.py` to regenerate the dedicated Collector card and dashboard JSON from this same register. This is an explicit maintenance step; it does not upload captures, publish a release, or run background model requests.

Earlier retired shim/native-log progress is preserved in the repository history and dated documentation. It does not qualify the current USBPcap application.

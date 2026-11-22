# DLE — Durable / Legible / Executable

## Mission
Reproducible ear-specific acoustic experimentation with local-first outcomes, an actual native Android application, and optional future physiological observers. Never imply an objective tinnitus biomarker has been validated.

## Public/private responsibility boundary
- `SemperSupra/aural-observatory` (public): reproducible MIT source, algorithm and build fixtures, public evidence, public DLE, source-only Android UI/DSP, test workflow.
- `SemperSupra/aural-observatory-private` (private): portfolio priorities, owner decisions, deployment signing/runbook references, evidence-review notes; **never** secrets, raw medical/biometric data, personal identities, device credentials, or account tokens in Git.

## Non-negotiable invariants
1. Actual Android native Views/AudioTrack, not a WebView wrapper.
2. No network/telemetry; BLE scan/connect permissions only for an explicit sensor connection, legacy Android location permission for BLE discovery only, no location collection.
3. Independent ear configurations, bounded frequencies/gain/duration, immediate STOP, no autoplay, lifecycle stop.
4. Fixed digital ceiling is NOT dB SPL. Device acoustic validation is a release gate.
5. User subjective outcomes authoritative for personal benefit; biomarkers contextual only.
6. Deterministic generators seeded and versioned; process order explicit; report algorithm approximations.
7. Never check in keystores, raw biosignals, private user observations, or third-party unlicensed sources.

## Atomic operations
`profile.validate`, `stimulus.generate-block`, `audio.start`, `audio.stop`, `observation.record`, `observation.export`, `sensor.select`, `sensor.connect`, `sensor.observe`, `sensor.disconnect`, `sensor.csv-opt-in`, `quality.packet-check`; future: `clock.align`, `quality.annotate`, `experiment.randomize`, `outcome.analyze`.

## State machines
Playback `idle → configured → playing → finished/stopped/error`; lifecycle pause interrupts playback. Outcome `unrated → baseline-rated → session-completed → post-rated → persisted/exported`; saving requires a completed preview. Sensor future `disconnected → discovering → consent → connected → recording → disconnected/error`.

## Gates
- G0 source review and static checks: PASS for source and XML; acoustic/clinical claims excluded.
- G1 native Android + three-platform core compile/test: PASS — GitHub Actions run 44928818567; debug APK only.
- G2 acoustic fixtures and physical headphone measurements: PENDING.
- G3 physical device smoke and USB/Bluetooth audio routing: PENDING.
- G4 BLE standard HR+optional RR plugin implementation and synthetic protocol fixtures: IMPLEMENTED IN SOURCE; platform compilation and on-hardware qualification PENDING. Proprietary ECG is NOT supported.
- G5 user-consented A/B experiment and safety review: FUTURE.
- G6 F-Droid personal then upstream, Google Play packaging/review: FUTURE.

## Dependencies
G1 before distributing an APK; G2/G3 before prolonged listening. G4 independent of HRTF. G5 depends on G2/G3 and journal quality. Mainline is source authority; keep PRs and issues narrow.

## Sensor plugin increment (v0.5)
- Typed `IPhysiologicalSensor`, `HeartRateMeasurement`, `HeartRateObservation`; no Garmin-specific parsing, no inference of tinnitus symptom improvement from physiological signals.
- BLE HRM packet decoder covers optional RR, contact state, energy, 8/16-bit BPM; malformed notification rejection and bounded read channel.
- Android optional native sensor controls; local recording requires user opt-in during a preview. Explicit permissions; no server, no continuous background capture.
- Windows and Linux console diagnostics use the same code and local-only CSV. Mac CoreBluetooth target is in source with CI compile gate (runtime permissions/hardware still pending); ANT+, Garmin proprietary data and persistent signed releases remain open.
- G4 qualification split: G4a parser tests/CI; G4b real HRM-Pro Plus BLE discovery and RR visibility; G4c timestamp and drop characterization; G4d Mac/native-device adapter and usability.
- No collection of user physiological data within GitHub Actions. Synthetic fixture-only CI. Never include BLE addresses, pairing secrets, raw recordings, device serials, or personal biometrics in Git.

## Search and data-source increment — R6
- **Scope:** Generic SIG BLE HRM source remains transport-neutral; Garmin Personal history uses separate read-only `IHistoricalDataSource` and explicit user credentials.
- **Implemented in source:** randomized fixed-gain logarithmic frequency search with repeated baseline, separate masking/post-stimulus interval holds, optional refinement, native Android pointer/keyboard UI and opt-in system voice-note intent, desktop local browser controls with same C# DSP, user-triggered Garmin snapshot console.
- **Atomic actions:** `trial.plan`, `trial.begin`, `user.quiet.down`, `user.quiet.up`, `audio.actual-stop`, `trial.annotate`, `trial.complete`, `result.local-export`, `source.authorize`, `source.fetch-day`, `voice.note-once`.
- **Trial state:** Ready → Stimulating → PostStimulus → TrialComplete → Ready or Finished; interrupted trials are retained but excluded from ranking. Device volume never adjusted by a search.
- **Trust:** No clinical conclusions from subjective silence or HRV; no passive microphones; Personal API key is owned by the user, not checked in or used for public multiuser login; Android speech intent may not be offline despite preference.
- **Gates:** R6 synthetic build/test and Android compile pending CI; microphone/system provider qualification, live HRM, real Garmin account, device acoustic safety, and N-of-1 study remain open.
- See `docs/AUTOTUNE-AND-DATASOURCES.md` for algorithms, interfaces, qualification and safety.

## R7 — configurable response controls and public package candidates
- Three response modes: hold-quiet, release-quiet after audible-arm, and toggled quiet. Core interpreter makes physical-edge interpretation deterministic. Inverse mode is unarmed before first press; phase boundaries reset without importing a false quiet state.
- Response mode is recorded in R7-local CSV; R6 observations are retained in a separate file to avoid mixed schema.
- Public-source-only candidate packaging through pinned SHA Actions on standard Ubuntu 24.04, Windows 2025 and macOS 26 runners; the Windows target includes a user-scoped NSIS EXE. These are unpromoted, unsigned/unvalidated artifacts.
- Private Android and Windows Foundry repositories retain governance, eligibility and release controls. Public product/Foundry planes own reusable build mechanics; no private or personal input data in public CI.
- G7 status: synthetic build/test and installer compilation PENDING; on-device BLE/ANT+ acquisition, acoustic validation, Windows install/uninstall, signing and production publishing NOT COMPLETE.
- Authority and acceptance: docs/QUIET-MODES-AND-PACKAGING-R7.md, public PR for this branch, runner census SemperSupra/agent-dispatch-private#223/#281.

## R9 desktop convergence — machine-readable evidence and user journeys
- R7 validated baseline merged to main at c97cac4. Current integration branch adds R8-A/B/C + R9-D/S/Q: measured DSP spectra, corrected separate noise variants, native Android preferences, preregistered study plan, bilateral desktop controls and persistent presets, localhost desktop lifecycle guard, real Chromium synthetic E2E. Workcell source, gates, and audience boundaries in docs/R9-INTEGRATION.md.
- Public three-OS compilation and integration browser/Android qualification REQUIRED and not yet passed at this combined SHA. Do not infer actual acoustic dB SPL from digital peak ceilings or autogenerated audio.
- Main is release authority after tests. PRs #9–11 and #13–15 are workcell evidence; issue #12 remains open for manual OS accessibility and first-class desktop UX. Foundry signing/publishing on HOLD.

## R10 — restore Aural authority; integrate Tinnitus Lab R3 as evidence, not a separate app

Authority correction, 2026-11-22: the `SemperSupra/aural-observatory` public product and its private governance companion **already existed**. A separate chat-local `Tinnitus Lab R1–R3` was developed without first reconciling these repositories. **No additional repository is needed.** Original reference bytes are not yet imported; preserve four local Git commits and exact SHA-256 custody in [R10 reference crosswalk](research/R10-TINNITUS-LAB-REFERENCE-CROSSWALK.md). The local browser test counts do not qualify native Android/C# or headphones.

Workcells, deliberately non-overlapping:
- **R10-A `#20`**: bounded `AcousticSweep` and reference tone / moving notch-center within **the existing** portable C# engine; 8–15 seconds, independent ear selection, no auto-start, output level unchanged, hard silence after sweep. Public Android and desktop compilation, DSP fixtures, interruption semantics are gating. Initial public CI failure due to compiler parameter shadowing was repaired on the workcell branch; fresh CI is authoritative.
- **R10-B `#22`**: independent analytic digital notch -3 dB width/edge results at native 48 kHz compared to four original R3 WebAudio reference values. Keep theoretical transfer function and actual generated-PCM measurements distinct. No physical SPL inference.
- **R10-C `#21`**: reference legal/privacy custody, source-of-truth comparison, evidence ledger and this DLE. No duplicate production PWA/JS audio engine. The original R3 HTML/Git bundle remains outside this repository until independent admission.

Remaining acceptance gates: (1) CI at the latest exact PR heads; (2) actual tone/notch user controls in native Android and desktop that invoke shared C# DSP with a user gesture, no mid-trial unrecorded mutation and correct STOP; (3) actual generated moving-notch PCM and A/B/phase/ramp measurements; (4) physical headphone/SPL, route changes, assistive technologies and user safety; (5) research protocol, consent and outcome validity. Release is still **HOLD**, not a clinical claim. R9 `#18`/ `#19` failures are separately owned and must not be hidden by unrelated green CI. Public GHA only unless a separate governed executor admission exists.

## R11 — portable shared-core candidate, OS-aligned presentation (2026-11-22)

**Decision state: QUALIFICATION, not migration.** [R11 candidate architecture and measured decision gates](architecture/R11-PORTABLE-CORE-QUALIFICATION.md) defines one product across native UI, deterministic headless automation and agent-accessible contracts. .NET `AuralObservatory.Core` remains the authoritative working DSP. C17 ABI and Go are intentionally independent *experimental* candidates; WebAssembly is a deployment target, not another source of algorithm truth. Keep Android `AudioTrack` and desktop C# generated audio intact until cross-engine/OS physical acceptance; do not build unnecessary UI forks or grant automatic playback.

**Workcells / resource envelope:** (A) C17 numerical + bounded PCM probe and public CPU GHA; (B) Go non-RT alternative / native qualification and public CPU GHA; (C) C#/Kotlin/Swift/WinUI/GTK/browser/ARM adapter feasibility research; (D) existing R9/R10 stabilization. Each has disjoint paths and exact receipt. Work may proceed concurrently only after the shared versioned fixture/ABI/safety criteria are fixed. No private GHA minutes or remote personal health data.

**Acceptance barrier:** quantify deterministic response, sample ceiling, STOP, phase/block invariance, no render-time allocation, cross-language numerical tolerance and actual end-to-end bridge cost on qualifying targets; retain golden C# and R3 WebAudio reference responses for comparison. Then evidence-based choose KEEP .NET / SPLIT Go controller with shared DSP / ADOPT C ABI, preserving rollback. Human/owner controls merger, release, publishing and any irreversible migration. Until physical calibrated headphone and OS routing tests pass, output is experimental and **release HOLD**. An artifact or green CI is not a clinical claim.

**Discovery:** root `AGENTS.md` points to canonical portfolio policy and this DLE, not an independent governance copy. Current integration branch is not `main`; continue with issue #5 and exact R9/R10 PR SHA rather than older README assertions.

### R11 first independent public-runner receipts (2026-11-22)

- Contract / discovery workcell: [PR #25](https://github.com/SemperSupra/aural-observatory/pull/25) contains `AGENTS.md`, [R11 qualification ADR](architecture/R11-PORTABLE-CORE-QUALIFICATION.md) and frozen `experiments/portable-core/fixtures/{sweep,notch}-v1.csv`; existing native Android + three-platform core workflow PASS [run 44972133379](https://github.com/SemperSupra/aural-observatory/actions/runs/44972133379). This is a **proposed** decision, not adopted migration.
- C17 candidate [PR #27](https://github.com/SemperSupra/aural-observatory/pull/27) / source head `1514e254de3eb7376ca86d3b126eaef3451bbdf5`: public [run 44972399107](https://github.com/SemperSupra/aural-observatory/actions/runs/44972399107) PASS Ubuntu+macOS native C17 compilation, 13 frozen tone-trajectory cases, four RBJ digital-notch response cases, STOP/ear/capacity/block controls; Linux ASan/UBSan and shared-library export checks PASS. Linux C library ~16 KiB. No actual moving-notch/noise render, Android JNI, Swift, WASM, ARM or device callbacks yet.
- Go alternative [PR #26](https://github.com/SemperSupra/aural-observatory/pull/26): standard-library synthetic reference-tone and analytical notch test matrix passed on Ubuntu, macOS and Windows after correcting PowerShell decimal-duration argument. **Important validator correction:** earlier benchmark at SHA `a168d665` was contaminated because the finite sweep expired, leaving only silence; the code now resets the private benchmark-only state when reaching its bounded end. Recompute any throughput claim from the latest exact SHA, not prior runs.
- .NET-to-C ABI [PR #28](https://github.com/SemperSupra/aural-observatory/pull/28) / source head `47c893a1e6b901e6a0a08bb1e8df88781a4da354`: public [run 44972539220](https://github.com/SemperSupra/aural-observatory/actions/runs/44972539220) PASS direct .NET 10 P/Invoke into C17 library, native STOP and PCM isolation, plus 4 numerical notch comparisons against **actual current** `NotchResponseAnalysis`. This proves one ABI route on Ubuntu, not production migration or full cross-platform binding parity.

**Known missing proof:** same-host apples-to-apples CPU/jitter/size and ABI glue cost, C# versus candidate actual generated moving-notch/noise waveform parity, mobile/iOS/ARM/WASM builds, native UI integrations, device-route/Stop-on-background, real calibrated headphone output, research efficacy. Release remains HOLD. Disjoint workcells run on standard public free-tier GitHub Actions; GitHub PRs are unmerged candidate surfaces, not production acceptance. Next dependency barrier: freeze shared full-DSP test fixtures/metrics, run same-machine bakeoff and a minimal real native-platform binding; decide retain .NET / C ABI / split Go control only after independent review and owner authority.

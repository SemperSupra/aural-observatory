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

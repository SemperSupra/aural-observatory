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

## R8-A — digital spectral and signal qualification
- Owner PR: #9 (draft), independent R7-stacked branch. Runs on public standard Ubuntu 24.04, Windows 2025, macOS 26 (max-parallel 2).
- Executable synthetic `AuralObservatory.Audio.Tests` renders actual `StimulusEngine` PCM16; measures bandwise PSD and approximate spectral exponent, left/right notch suppression and off-band leakage, peak/RMS, block partition identity, ramp start and channel isolation. Versioned JSON receipt; no user audio in CI.
- CI status: pending completion. Do not assert exact pink/brown/blue/violet PSD from color labels without numerical evidence. Keep original generator intact for historical comparisons.
- This does NOT measure physical dB SPL, hearing-aid output transfer, acoustics or clinical benefit; G2 acoustic/physical release gate remains blocked.

### Digital output measurement receipts (real generator output, synthetic inputs)
- First R8 Goertzel spectral evidence: public Actions run 44938619323 (Linux/Windows passed); emitted 48-kHz PCM16 white PSD exponent ~-0.012, pink ~0.988, brown ~1.963, **legacy blue ~-1.987 versus nominal -1**, **legacy violet ~-3.907 versus nominal -2**; notch-center 4 kHz attenuation -38.9 to -49.1 dB and off-band near 800 Hz ~-0.1 dB. These are power spectral exponents for \(S(f)\propto f^{-\alpha}\); measurement probe limitations noted in receipt.
- Versioned (not destructive) `NoiseColour.BlueCorrected` and `VioletCorrected` implemented in shared engine. Second public Actions run 44938877960 Linux/Windows emitted corrected blue exponent ~-0.988 and corrected violet ~-1.987; compare legacy outputs rather than replacing them. Mac runner still needs evidence for full three-target parity. Original legacy enum numbers and output algorithms preserved.
- Observed digital peaks <=0.08 full scale; NOT acoustically calibrated safe SPL. Neither headphone/hardware frequency response nor aid processing validated.

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
- Windows and Linux console diagnostics use the same code and local-only CSV. Mac CoreBluetooth target, ANT+, Garmin proprietary data and persistent signed releases remain open.
- G4 qualification split: G4a parser tests/CI; G4b real HRM-Pro Plus BLE discovery and RR visibility; G4c timestamp and drop characterization; G4d Mac/native-device adapter and usability.
- No collection of user physiological data within GitHub Actions. Synthetic fixture-only CI. Never include BLE addresses, pairing secrets, raw recordings, device serials, or personal biometrics in Git.

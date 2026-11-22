# R10 — Tinnitus Lab R1–R3 admission / Aural Observatory convergence crosswalk

Status: **reference intake only; no second application or clinical release**. Date: 2026-11-22. Parent work order: [#5](https://github.com/SemperSupra/aural-observatory/issues/5). Production integration branch: [PR #16](https://github.com/SemperSupra/aural-observatory/pull/16). R10 C# core workcell: [PR #20](https://github.com/SemperSupra/aural-observatory/pull/20).

## Authority and repository hygiene

* This public repository owns Aural Observatory's **single production DSP** in `AuralObservatory.Core`, native .NET Android `AudioTrack`, and desktop browser shell which consumes C# generated PCM. WebAudio is **not** a second production engine.
* `SemperSupra/aural-observatory-private` holds only non-public decisions, evaluations and admission controls; **no raw personal observations, biosignals, names, credentials, recordings or keystores in either repository**.
* The predecessor Tinnitus Lab standalone R1–R3 was generated in a chat sandbox. It is **not remotely hosted, committed to Aural, or qualified on actual hardware**. Its Git history is four commits (`cd3b2af`, `3cf28c8`, `2578d50`, `0fd55ec`). Local files are preserved in that original conversation and must not be assumed visible to an unrelated cloud runner.
* Local content-addressed custody, verified on 2026-11-22:
  - `tinnitus-lab-r3.git.bundle`: SHA-256 `33d0aa1ac244485efcde840293bf99c24555595d8c391d02de461800f4e8667d`;
  - `tinnitus-lab-r3-source.zip`: SHA-256 `3a70c7fe1e4f074a038372aefd7affd7fc101b6551d5a870fc800254f56d6648`;
  - `tinnitus-lab-r3.html`: SHA-256 `08eb1e492113516ab1fa2c66d68b807412d22b8a906c7c7990940bcb7856dce2`.
* The reference snapshot is original MIT-licensed software (2026 project contributors); license/privacy provenance and copied-source hygiene still need independent review before importing source. No compiled PWA or user history belongs in a production package.

## Transfer contract / do not confuse implementations

| Dimension | R3 browser reference | Authoritative Aural R9 | Decision |
|---|---|---|---|
| Engine | Web Audio native `BiquadFilterNode` / oscillators | portable .NET `StimulusEngine`, generated 48-kHz PCM16 | Keep .NET as sole production DSP; use reference only for independent verification |
| Ear/frequency range | ~125–12,000 Hz (then sample-rate clamp) | 200–10,000 Hz, independently configurable ears | Preserve tighter Aural bounds; reject unrepresentable parameters; do not silently clamp imported observations |
| Duration | 8–120 s sweeps, five-minute general audio timeout | 15-s preview; lifecycle STOP | Enforce 8–15 s R10 sweep; a 120-s reference is **not** permission to extend native output |
| Tone sweep | Continuous phase logarithmic browser oscillator | New bounded shared-engine `AcousticSweep` under PR #20 | Test phase/block continuity and frequency endpoint math; no autoplay |
| Notch center sweep | WebAudio filter center/Q moves continuously | C# `NotchBiquad` with moving center and retained state under PR #20 | Require actual generated-PCM spectral/time measurements, ear isolation and audible transition checks |
| Notch width | Nominal octave-based Q; measured digital -3 dB edges | Existing C# notch Q formula at 48 kHz | Do not describe nominal octave as true digital width; qualify at 48 kHz and expose the measured value after validation |
| Pitch mark / half-double | R2/R3 UI controls; subjective user record | Existing R6/R7 quiet-search and user journals | Add mode-specific UI and typed provenance only after DSP CI; don't overwrite randomized quiet search or confuse masking with residual inhibition |
| A/B bypass | In-flight browser filter toggling | Shared C# engine and staged preset protocol | Require level-matched, intentional user A/B and no mid-trial mutation of frozen settings; independent randomized sham remains a different protocol |
| App/runtime | offline PWA and browser UI | real native Android and browser-backed desktop | No WebView replacement; if a PWA is retained, label it reference-only until new platform evidence |
| Privacy | explicit local JSON exports | local OS user data and separate private governance | No personal export data in Git/CI; versioned synthetic records only |
| Safety | digital ceiling 0.035; no calibrated SPL | 0.08 full-scale ceiling; no calibrated SPL | Neither is an acoustic safety certification; physical coupler/headphone measurement remains P0 |

## Qualification status: do not inherit evidence across engines

Reference R3 local receipt: 15 Node tests, 33 Chromium smoke checks and 9 SciPy comparison scenes; **these test the browser reference, not Aural's C# engine or Android headphones**. R3's SciPy `freqz` difference approximately `2.98e-14` was for its RBJ digital reference, not a human perceptual or device calibration metric.

Aural R9 convergence PR #16, at `40aac0b4b371154e2ab9bf97136510b963d5b927`, reported 7 successful **public** workflows including synthetic audio and native compilation. That does **not** qualify PR #20 until the R10 commit's own public runner receipts are inspected.

Acceptance sequence:
1. **G10a**: bounded deterministic trajectory, semantic ear selection, waveform peak, silent after 15 s, no gain automation — CPU synthetic tests on public runner.
2. **G10b**: compare actual generated moving-notch PCM (not coefficients alone) with versioned 48-kHz RBJ reference samples at multiple center points, directions, ears, and block sizes; record measured -3 dB response and coefficient-stepping artifacts. Aural's static noise-color fixes cannot be silently rolled back.
3. **G10c**: native Android touch/STOP/AudioTrack vs desktop/keyboard/touch interface path. No extra WebAudio synthesis. Preserve per-ear controls and explicit user playback.
4. **G10d**: real physical earphone, Bluetooth/USB route change, system interruption, safe output / SPL measurements with calibrated apparatus. Until completed, candidate-only status.
5. **G10e**: randomized experimental protocol and pre/post reports can be introduced only after basic digital and hardware gates. Quiet during playback is masking, not necessarily residual inhibition.

## Independent workcells and integration order

* **DSP / parity:** PR #20 (source + synthetic tests), follow-on 48-kHz response and time-domain measurement. R8-A PR #9 is historical; do not resurrect redundant branches.
* **Human UI / accessibility:** build on PR #16 and #12; native Android and desktop have separate presenters but one C# DSP. The R3 visual is a design reference only. Validate TalkBack, NVDA, VoiceOver, Orca on actual systems later.
* **Safety / experimental controls:** preserve existing R6/R7 state machines, 15s preview, 10s post-stimulus observation and interrupted-trial flags. Use PR #11 study protocol as source of requirements, not as a medical efficacy proof.
* **Repository / release:** keep public GHA and candidate packages in product repo; Android and Windows foundries govern signing and promotion. Do not burn private CI minutes or push personal records to public workers.

Gate on actual green CI before merging. Existing R9 child PR #18 and #19 had failing runs; reconcile those independently rather than blending their failures into sweep core. An integrated PR, status receipt and public DLE annotation are the durable return baton. 

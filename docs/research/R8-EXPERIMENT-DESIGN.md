# R8 — Experiment design: credible acoustic N-of-1 research

**Status:** design + deterministic blocked-randomization core and synthetic tests. Not a clinical study, clinical efficacy claim, acoustic calibration or participant enrollment protocol.

## Distinguish discovery, confirmation and longitudinal change

1. **Discovery / AutoTune (adaptive):** exploratory fixed-safe-level trials may sample diverse frequencies, widths, temporal/spatial manipulations and noise classes, with repeated controls and explicit cost/burden budgets. All winners are provisional; post-selection p-values would be invalid.
2. **Preregistration / freeze:** choose one contrast and one primary outcome, identify algorithm version, seed, stimulus/hash, device/ear calibration and trial order, exclusion and missingness rules, washout, maximum exposure; freeze a canonical protocol SHA-256 *before* testing confirmation.
3. **Confirmatory N-of-1 (nonadaptive):** balanced randomized AB/BA pairs with adequately matched controls or a documented sham, concealed condition codes where feasible, repeated baseline and follow-ups. Do not tell the optimizer the condition while the user is reporting results.
4. **Follow-up:** next-hour, next-day and longer-term outcomes (validated TFI/THI when appropriately administered and licensed); adverse effects and meaningful burden changes, not only momentary masking.
5. **Synthesis:** predeclared paired active-control contrast, uncertainty interval, randomization/permutation inference or hierarchical model where preregistered, check carryover/period, missingness and multiplicity. Do not call a result a cure.

## Why this design

Tinnitus fluctuates with attention, fatigue, stress, expectation and hearing-aid fitting. During-stimulus quiet may indicate masking; post-stimulus quiet may indicate residual inhibition. Both must be timestamped but never treated as interchangeable outcomes. User self-report remains the direct measure; autonomic, face, pupil and gaze are secondary hypotheses, not validated tinnitus detectors.

Sources (accessed 2026-11-21):
- https://pmc.ncbi.nlm.nih.gov/articles/PMC3897200/ — tinnitus trials: control conditions, blinding, stable baselines, multidomain outcomes.
- https://pmc.ncbi.nlm.nih.gov/articles/PMC9118607/ — pitch, masking, residual-inhibition protocol.
- https://pubmed.ncbi.nlm.nih.gov/42167925/ — 2026 randomized neuromodulation evidence synthesis; acoustic CR single-study evidence.
- https://www.healthquality.va.gov/guidelines/CD/tinnitus/index.asp — VA/DoD 2024 CPG.
- https://aao-hnsfjournals.onlinelibrary.wiley.com/doi/full/10.1002/ohn.1217 — clinically important differences for TFI/THI require context.

## Required protocol fields before live confirmatory runs
- `experiment_id`, protocol schema and immutable SHA, owner consent version and participant pseudonym (never identifying details in public Git).
- Ear-specific audiogram/audibility status and output routing: headphones vs supported hearing aids, model/firmware, gain/processing state, equalization and audio latency; avoid overinterpreting uncalibrated dB FS.
- Independent physical safety gate for calibrated transducer output; abort and emergency STOP, bounded duration, no unattended sessions or auto-gain changes.
- Unambiguous device/session monotonic and UTC timestamps; event quality (late/duplicate/touch blur/packet loss), output device change interrupts the trial.
- Source/stimulus SHA-256, algorithm version, seed, transformation DAG identity, sample rate and measured spectral qualification, fixed gain.
- Prior sleep/stress/context, medication/caffeine changes and posture as possible covariates without imposing rigid restrictions.
- Pre/post tinnitus loudness and distress NRS, response mode, masking timing, post-sound window, washout and adverse-response fields.
- Missing data policy: do not back-fill absent responses; distinguish skipped and interrupted from quiet; retained but excluded per preregistered analysis.
- Active vs control matched for loudness/spectral content where scientifically possible; assess sham credibility; document unavoidable unblinding.
- Longitudinal TFI/THI only when appropriate/rights permitted; record MCID, noise floor and minimal detectable change rather than guessing a universal cutoff.
- Machine interface: `ExperimentProtocol`, `ExperimentPlanner.Build`, synthetic protocol validators, JSON receipts and read-only evidence for automation/agents. Agents may suggest candidates or analyze sanitized fixtures, but may not autonomously initiate audio playback, change gain or access raw health data.

## Planned study types
- Auditory pitch matching and test-retest reliability.
- Minimum masking level at *calibrated* acoustics (not by automatic volume escalation).
- Residual inhibition latency, duration, post-stimulus NRS with appropriately spaced controls.
- Randomized active/notch vs sham/control, broadband vs colored noise, spectral decorrelation, binaural/stereo vs mono.
- Factorial or response-surface *discovery* across frequency, width, temporal/spatial and ear asymmetry; do not confound >1 changed factor in confirmation.
- Daily ecological momentary assessment: tinnitus burden, attention, sleep, context, noise exposure; delayed outcomes and adverse events.
- Observational sensors: HR/RR, EDA, respiration, accelerometry/posture; optional eye/face data only under separately consented research modality.

## Computer vision secondary signals — optional, never default
Relevant research:
- https://pmc.ncbi.nlm.nih.gov/articles/PMC10769427/ — pupil and face measures of auditory affect (not a universal tinnitus measurement).
- https://pmc.ncbi.nlm.nih.gov/articles/PMC12340780/ — videographic autonomic measures in tinnitus/sound sensitivity.
- https://pmc.ncbi.nlm.nih.gov/articles/PMC13004733/ — blink measurements confounded by gaze angle.

Design opt-in sensor plugins for blink timestamps, gaze angles, pupil proxy with lighting calibration, head pose, facial action-unit kinematics and gross posture. Avoid categorical 'emotion', 'stress', 'pain' or 'tinnitus relieved' inferences from facial expressions. Video must not be a hidden requirement. Default to discard raw frames after local feature extraction, explicit consent for any storage, camera shutter indicator, source-quality flags, deletion and participant privacy. Camera motion, illumination, eyelid anatomy, glasses, fatigue, blinking, head pitch, demography and algorithm bias require protocol qualification and negative controls.

## Interpretation rule
Only clinically meaningful *and* replicated changes, evaluated against baseline and active-control contrasts with uncertainty, justify a personalized efficacy hypothesis. A post hoc 'best sound' from an adaptive search is preference discovery, not a demonstration of therapeutic benefit.

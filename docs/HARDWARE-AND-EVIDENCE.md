# Biometric acquisition plan — 2026-11-21

## Questions and measurement tiers

- **What did the application output?** Digital PCM peaks, PSD/spectral notches, calibration playback fixtures, timestamps and rendering provenance. This is necessary, not sufficient: real acoustic SPL needs a coupler/calibrated microphone and suitable calibrator, and headphone output changes across devices.
- **How did the body respond?** ECG and RR intervals, HR/HRV; EDA sympathetic arousal; accelerometer/movement; optional EEG bands and connectivity. These are indirect measures, confounded by breathing, posture, stress, movement, caffeine, medications and sleep. Never label as objective tinnitus response.
- **Did tinnitus improve?** User ratings of perceived loudness, annoyance, functioning, residual inhibition after sound stops; later follow-up. Objective physiology alone cannot determine this.

## Procurement ladder

1. **Polar H10**: first purchase. Standard BLE Heart Rate Service and publicly documented Polar BLE SDK: HR and RR, raw ECG at ~130 Hz when using proprietary characteristics and SDK. Widely available including Amazon.com. No need to purchase EEG for the first studies.
2. **PLUX BITalino (EDA/ECG modules)**: modular optional second instrument; verify the exact bundle contains EDA/GSR sensors and electrodes before purchase. Raw biosignals and open interfaces support experiments, but setup is more complex.
3. **Muse 2 / Muse S**: exploratory four-channel EEG + auxiliary sensors; device generations differ in protocol. Vendor SDK access and open BLE readers vary. Some newer models are unsupported by existing readers. Do not claim EEG beta/alpha ratios prove tinnitus changes.
4. **OpenBCI Ganglion/Cyton**: optional advanced electrophysiology only after protocols and clinical hypotheses are justified; high hardware/electrode effort and cost.
5. **Calibrated acoustic coupler + measurement mic**: important before prolonged acoustic trials or public health distribution. A cheap USB microphone measuring sound in air does not calibrate pressure at the ear drum.

## Experimental protocol proposal

- Fixed listening equipment and documented comfortable, low volume; avoid high-frequency threshold chasing.
- Pre-register a primary perceptual outcome such as loudness/distress score *after silence*, not while a masker is playing.
- Baseline 2–3 minutes, active/sham randomized 5–15 minutes (the current prototype only delivers 15-second previews), immediately rate post-stop and later after 5/15 minutes.
- For HRV use clean ECG/RR data, artifact and ectopic detection, time-domain RMSSD and sensitivity analyses. Five-minute stationary windows are preferable for stable short-term metrics; do not interpret 15-second HRV samples as robust effectiveness measurements.
- Record timing, left/right condition, device model, sample rate, data gaps, subjective pre/post, medication/stimulant changes where volunteered, and adverse events.
- Keep personal biometric data in the private work area, never committed to public source repositories. Users own the data; consent before any remote synchronization.

## References

- Review of biosensor-based tinnitus biomarkers, November 2026: https://pubmed.ncbi.nlm.nih.gov/43050753/
- Biomarkers systematic review, 2026: https://pubmed.ncbi.nlm.nih.gov/42612615/
- Polar H10 SDK: https://github.com/polarofficial/polar-ble-sdk/blob/master/documentation/products/PolarH10.md
- Polar SDK: https://github.com/polarofficial/polar-ble-sdk
- Muse EEG SDK: https://choosemuse.com/pages/developers
- Muse open reader: https://github.com/obo/muse-record
- F-Droid inclusion: https://f-droid.org/docs/Inclusion_Policy/
- F-Droid simple binary repositories: https://f-droid.org/en/docs/FAQ_-_App_Developers/

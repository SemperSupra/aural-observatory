# R8 — ANT+, bladeRF SDR, ReSound and naming feasibility

## ANT+ transceiver
- ANT USB-m compatible radio chips can be master (transmitter) or slave (receiver); generic ANT API supports broadcast TX/RX and profile-specific pages. An ANT+ certified device uses governed network keys/profiles. User's QCA61x4A laptop BLE radio does **not** expose ANT+.
- For PC ANT+ use an ANT USB-m supported transceiver, not a plain BLE dongle. Verification: Garmin ANT PC SDK samples, master transmit into independent known receiver, profile/ID collision avoidance, channel close/reopen, sender/receiver timestamps, dropped/buffered packets and proper disconnection. No health data should be spoofed as a real person's observation.
- ANT+ stream output is an *optional* outward adapter to synthetic/timestamped telemetry or authorized targets; keep identity, consent and explicit user action. No automatic transmission of personal biomarkers.

## User's existing bladeRF x40 bench
- Two bladeRF x40 SDRs are TX/RX-capable and tune through the 2.4 GHz ANT band; user has dedicated attenuators and dummy loads. They could capture RF envelopes, test GFSK channel and packet timing, or generate lab-only frames.
- A compliant ANT/ANT+ PHY/MAC/profile stack is considerably more work than selecting an existing ANT USB transceiver. Need radio timing/hopping, CRC, modulation, channel lifecycle, error recovery, proper network key/profile and RF emission rules. GNU Radio/libbladeRF are viable RDTE surfaces; RadiANT is a candidate third-party protocol reference requiring independent legal/source review.
- Start with shielded/conducted synthetic bench, measured attenuation and dummy loads, output disabled by default, then low-power compliant over-air only after applicable German/EU RF legality/conformity review. Do not connect high-power TX directly to RX. Avoid treating an SDR transmission as genuine medical measurements.
- Candidate source: https://github.com/Nuand/bladeRF and https://github.com/winedarksea/RadiANT. Supplier hardware coverage: https://www.nuand.com/product/bladerf-x40/ ; ANT overview: https://developer.garmin.com/ant-program/ant-ant-plus/.

## ReSound Smart 3D
- ReSound Smart 3D is a proprietary hearing-aid control/settings app, not an openly available generic sound-output API. It can adjust supported Tinnitus Sound Generator features depending on the model and clinical fitting.
- Aural Observatory should use the *OS media output route*: Android ASHA where supported, LE Audio/Auracast on compatible newer hearing aids/phones, iOS MFi on supported models, or authorized accessory/desktop audio path. It does not need to control the ReSound app directly to stream experimental PCM.
- Verify the *exact hearing-aid model, phone OS/hardware, program and current pairing*. Do not claim every ReSound/Android device supports direct stereo streaming. Output is reshaped by hearing-aid compression, band limiting, programs and ear-specific fitting, potentially invalidating intended notch/HRTF. Record audiologist-approved baseline settings and never reprogram a clinical fitting from this utility.
- Test: OS audio route query; mono/stereo, left/right output and aid latency; no simultaneous unplanned TSG generation; confirm whether spatial cues remain audible; headphone SPL and hearing-aid acoustic output independently qualified. If unknown, mark stimuli uncalibrated and not eligible for cross-device comparisons.
- Vendor refs: https://www.resound.com/en/help/compatibility ; https://www.resound.com/en/help/apps/smart-3d ; https://play.google.com/store/apps/details?id=dk.resound.smart3d .

## Project name TinEar
- Web queries (2026-11-21) did not find a confidently established, currently distributed tinnitus app named exactly 'TinEar'; that does *not* prove the name is available.
- Historical 'TIN EAR' trademark filings exist, and many related products named MindEar, Tinnitracks, Tinnara and similar exist. Complete EUIPO/DPMA/USPTO trademark and app-store/domain/name checks are still needed before launch.
- Keep public project name **Aural Observatory** and app **TinnitusLab** pending qualified naming/legal review.

## DX / audience interfaces
- Humans: primary screen/voice/keyboard/touch; immediate STOP, no autoplay, large text, TalkBack/VoiceOver/NVDA, accessible feedback and local settings.
- Automation: versioned JSON stimulus/profile/protocol schema, deterministic build and numerical QA, read-only evidence and export, no privilege to emit audio or move sensitive health data without explicit user interaction.
- Agents: protocol/stimulus selection and offline analysis by bounded CLI/API/MCP adapter only after verification; user and human research governance authorize actual session start, sound output, biometric acquisition, cloud sync and release. Never expose private health streams by default.

## Qualification gates
- ANT+ TX not implemented. SDR conducted loopback not implemented.
- ReSound app interop/API not implemented. OS-route feasibility documented, real device unverified.
- Camera-based observer not implemented; opt-in and privacy/signal-quality gates required.
- Naming availability not proven.

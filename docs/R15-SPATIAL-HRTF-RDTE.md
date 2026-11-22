# R15 — Spatial HRTF RDTE: truth boundaries, transfer measurements and physical gates

**Independent workcell.** Governing Aural issue #5; R12 desktop spatial controller PR #29. Owns only `tests/spatial-rdte/**`, `.github/workflows/r15-offline-hrtf.yml`, and this document; **does not modify production DSP or the R15 card-layout branch**. Synthetic public GitHub Actions only, no personal/health data, credentials, audible output or private minutes.

## Finding: not fully researched, developed, tested or evaluated

R12 source uses the authoritative C# `StimulusEngine` for an 8–15 second bilateral manual **ReferenceTone** sweep; optionally routes a browser `MediaElementAudioSourceNode` to `PannerNode` (`panningModel=HRTF`, fixed generic browser subject filters) then `GainNode(0.25)` into destination. It never enters randomized QuietSearch or ear-specific notch/noise, so the source/phase guards are intentional. R12 browser automation tested the *control/UI/panner graph and STOP request*, not measured headphone/ear-drum output, perceived localization success, actual OS hardware silence or consistent cross-browser HRTF response. Existing phase and empirical safety gates remain pending.

## Why speaker listening may be disappointing

- For conventional headphones, the processed left-ear signal predominantly reaches the left ear and vice versa, preserving binaural interaural time/level/spectral cues. Binaural HRTF also depends on individualized anatomy, playback device, target signal and head motion.
- With ordinary stereo speakers, each ear receives sound from **both** loudspeakers; acoustic crosstalk and room reflections generally confound the HRTF ear-specific signals. Specialized loudspeaker crosstalk cancellation is a *separate* renderer that needs speaker/listener geometry and safe validated calibration; no such algorithm exists in Aural.
- A changing-frequency reference tone is an especially weak single stimulus for spatial evaluations. Strong localization evidence should compare repeated controlled fixed-frequency and bounded broadband signals rather than conflating pitch changes, gain or position.
- Reduced digital gain (0.25 after the C# sample attenuation) is a conservative sample-domain factor, **not SPL calibration**. Do not compensate for weak spatial cues by turning up volume or automatically applying gain.

Independent references: [W3C Web Audio 1.1 PannerNode/HRTF](https://www.w3.org/TR/webaudio-1.1/), [MDN spatialization basics](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API/Web_audio_spatialization_basics), [Spatial Audio Review (2017)](https://www.mdpi.com/2076-3417/7/5/532), [Localization with headphones vs loudspeakers+CTC](https://pmc.ncbi.nlm.nih.gov/articles/PMC7198834/). These support mechanism, not Aural qualification.

## Executed public-synthetic workcell

`tests/spatial-rdte/offline-hrtf.cjs` uses true browser `OfflineAudioContext`, `PannerNode=HRTF`, and deterministic *test-only* low-level mono broadband random signal at 48kHz. It renders front/left/right/rear/above/far-right in Chromium and WebKit with **no connection to physical speaker output**. Receipts record per-channel RMS, left/right level delta, zero-lag correlation, sample rate, channel count, finite data and peak sample bound. This can detect silent graphs, clipping and gross browser variability. It **cannot** establish individualized HRTF quality, interaural time-delay perceptual thresholds, room/speaker crosstalk, acoustic SPL, or hearing-safety.

## Required verification matrix and gates

| Cell | Method | Authority / acceptance | Status |
| --- | --- | --- | --- |
| A. Software-graph transfer | Headless OfflineAudioContext HRTF across angles, engines, seed fixtures; deterministic JSON | This branch; output finite, correct channel count, no digital overload; variation advisory, not ear proof | SOURCE STAGED / exact-head CI pending |
| B. Headphone binaural perceptual RDTE | Locally user-started low-volume bounded blinded left/right/front/back localization trials; compare on/off HRTF and controlled broadband/static tone | Local actor only with person physically present; collect anonymous aggregate or device-local reports | HOLD |
| C. Hardware output route/STOP | Verify actual wired/Bluetooth headphone route, disconnect, resume, interrupt, sample-rate changes, start/STOP latency and measured SPL | Existing Aural G2/G3 #3 and signing/release gates; no unapproved sound in cloud | HOLD |
| D. Loudspeaker reproduction | Independent equal-power stereo panning as a non-binaural baseline; assess room/speaker geometry, and optional separate CTC concept only after explicit research approval | No claim stereo speakers reproduce binaural 3D; speaker mode separate from HRTF | FUTURE |
| E. Better localization stimulus | Static/broadband reference instrument in the *existing* C# DSP with seeded goldens, fixed gain/ramp, explicit user action and unscored isolation | Distinct future workcell; do not add unauthorized synthetic JS treatment engine | FUTURE |
| F. Devices & accessibility | OS/browser and actual wired/Bluetooth headphones, 320px/landscape, TalkBack/VoiceOver/NVDA and A/B protocol | Physical validation by local actor with sanitized receipts | HOLD |

**Recommended decision:** default binaural mode label to 'Headphones recommended, generic HRTF, unqualified'; separate 'Speakers — stereo pan (not binaural)' only as a future design candidate. Keep HRTF optional and do not automatically select device or start playback. Preserve pure-tone sweep for pitch-search research without promising spatial localization.

**Blocker resolution:** public synthetic software evaluation can progress autonomously now. A real headphone or speaker comparison and SPL/actual STOP measurement requires local equipment, a physically present consenting listener, approval for any audible test, and human safety oversight; these are *not* delegated to unattended cloud runners. Existing public/paired repository topology is sufficient; no purchase or new repo is needed for this source-only batch.

**Next cold-start actor baton:** Read Aural #5 + AGENTS/Way of Working, inspect exact R15 offline-audio PR and source, claim bounded independent headset-input-only/route survey vs DSP benchmark workcells, record model/thinking effort/OS/CWD/ref and free resources, submit actual immutable receipts to #5. Do not play sounds without explicit human presence, expose raw medical records, alter card branch or use paid/private Actions.

# Aural Observatory — native Android C# prototype (v0.4)

A reproducible, **actual Android app**, authored in C#/.NET 10 for Android, using Android `Activity`/`View` controls and native `AudioTrack` PCM streaming. **There is no WebView, bundled PWA, localhost web service, microphone, Internet permission, telemetry, or cloud account.** A framework-independent `net10.0` core provides signal generation on Windows/macOS/Linux and Android. This repository is open source (MIT).

**Experimental research utility, not a medical device or validated tinnitus treatment.** Generated sound is digitally attenuated but is **not** calibrated in dB SPL; hardware volume, headphone sensitivity, and hearing status affect actual exposure. Start quietly, stop immediately if uncomfortable. Unilateral or pulsatile tinnitus, sudden hearing changes, and other concerning symptoms warrant appropriate professional evaluation. An observation of lower loudness is not a clinical efficacy demonstration.

## Currently implemented

- Two independent ear profiles: perceived frequency (200–10,000 Hz), notch width, digital gain, notch enable, white/pink/brown/blue/violet **approximate** noise.
- Bounded 15-second user-started, nonlooping playback via Android native streaming audio; ramp-in and end fade; STOP and lifecycle cancellation.
- Bounded amplitude modulation (0–20 Hz, depth 0–0.9), seeded pseudorandom generators, transparent config validation.
- Subjective loudness ratings (0–10) before/after a completed listening preview, local CSV observations and explicit Android share sheet export.
- Independent .NET 10 DSP test runner and GitHub Actions Linux Android + Windows/macOS/Linux core build matrix.

There is **no biometric sensor connection yet**: the native UI says this explicitly. HRTF, calibrated audiograms, sound-level metering, real-time ECG/EEG/EDA acquisition, notch-equivalence qualification to the previous app, and blinded N-of-1 trials are subsequent, separately gated milestones.

## Build (requires .NET 10 Android workload / Android SDK)

```
dotnet workload install android
dotnet run --project tests/AuralObservatory.Core.Tests/AuralObservatory.Core.Tests.csproj
dotnet build src/AuralObservatory.Android/AuralObservatory.Android.csproj -c Debug
```

Install the debug APK output using `adb install -r <path-to-produced-apk>` after checking the artifact path. Production distribution requires a release keystore, stable application ID, versioning, a privacy policy, reproducible build qualification, accessibility tests, actual device/headphone safety checks, and store review. **Do not publish the test build as a health treatment app.**

Package ID `org.sempersupra.auralobservatory`, min Android API 26. The supported target API is determined by the .NET 10 Android workload (API 36 as of this project). The app requires no permissions for its current audio/journal functionality.

## Biometrics roadmap

1. Standard Bluetooth LE Heart Rate Service (180D) RR-interval acquisition from Polar H10; use monotonic clock and packet-quality tracking.
2. Offline RR preprocessing with artifact rejection, RMSSD and baseline comparisons. None of these metrics measure tinnitus directly.
3. Optional EDA and EEG adapters behind interface contracts, with open licenses, explicit consent, and timestamp synchronization.
4. Randomized sham/active paired comparisons using the same headphones and session schedule. Primary outcome is user-reported tinnitus burden and residual-inhibition rating, not algorithm-optimized physiology.
5. Hold the generated output steady while sensors observe. Never automatically increase volume based on feedback.

See `docs/HARDWARE-AND-EVIDENCE.md` and `docs/DLE.md`.

## Verification

- Core code and Android bindings reviewed against the documented .NET/Android APIs.
- The authoring runtime lacks .NET and Android workloads, so **compilation, APK installation, listening, and real sensor tests remain unverified** until CI succeeds and devices are tested. A GitHub Actions workflow is supplied to run builds; do not treat workflow existence as a green check.
- Prior v0.3 PWA/desktop code is retained as a separate baseline package, not silently imported into the native runtime. Cross-engine parity has not been established.

## License

MIT for new original code. Prior third-party project implementations and commercial device SDKs are not bundled, copied, or relicensed.

# R11 — portable core family qualification (candidate ADR)

Status: **PROPOSED / reversible qualification; NOT a language migration or release authorization**
Date: 2026-11-22. Authority: [Aural convergence issue #5](https://github.com/SemperSupra/aural-observatory/issues/5), public `docs/DLE.md`; canonical portfolio [Way of Working](https://github.com/SemperSupra/engineering-governance-private/blob/main/WAY_OF_WORKING.md) is referenced, not reproduced.

## Decision question

Would a shared compiled core improve portability, distribution, offline reuse, platform-native UI/UX, predictable real-time audio, and headless qualification enough to justify changing the already-running .NET 10 reference implementation?

**No migration is authorized by this document.** Baseline `AuralObservatory.Core` is the only qualified integrated engine so far. R10 manual sweep controls and R9 launchers remain their respective source owners. Do not split the app into a second independent DSP or duplicate the randomized study state machine.

## Product responsibility partition

| Responsibility | Candidate owner | Rationale and non-goals |
| --- | --- | --- |
| Sample-domain DSP (tone, moving RBJ notch, seeded noise, ramp, modulation, independent ears), fixed-size render blocks, digital response metrics | A single portable library selected after bakeoff; initial **C17 ABI** probe vs current **.NET** oracle and **Go** probe | No implicit microphone/network/physical audio; no auto gain; no direct UI state |
| Experiment planning, data versioning, research analyses, deterministic batch and headless CLI | Existing .NET now; **Go** candidate for non-real-time orchestration only after measured fit | No new cloud account, remote data plane or unauthenticated control endpoint |
| Android UX and hardware | Existing native Android C# Views/AudioTrack until an evidence-backed replacement; candidate Kotlin/Compose + NDK/JNI | Native touch, accessibility, route/permission/lifecycle, STOP on background |
| iOS, macOS | Future SwiftUI + AVAudioEngine / CoreAudio host; C ABI through Swift interop where qualified | Requires Apple signing/device path; never infer from successful cross-compile |
| Windows | Existing desktop C# loopback UI; possible WinUI/Windows App SDK native shell; WASAPI | Keep R9 launcher/security and upgrade state; no premature separate app |
| Linux | Existing loopback UI; possible GTK/libadwaita/Qt shell; PipeWire/ALSA host | Host audio callback must remain outside managed/unmanaged portability assumptions |
| Browser/PWA | TypeScript semantic UX, AudioWorklet + shared WebAssembly **only if selected core compiles and passes** | R3 WebAudio oscillator/notch remains reference, not production engine |
| Raspberry Pi/Jetson/OCI/CLI | headless Go or C/.NET adapters as qualified; CPU architecture and runtime evidence per target | No physical playback from cloud; no ARM capability inferred from x64 unit tests |

Human/automation/agent are **three audiences, not three implementations**: semantic accessible native UI; deterministic CLI/CI synthetic controls; discoverable contracts and typed receipts. Only a human-authorized gesture can cause hardware playback. Portable algorithm does not own OS audio, OS storage, BLE consent, microphone/speech, localization, safety policy or signing.

## Candidate decision

1. **Stay .NET**: already implemented, known numerical output, fewest changes, portable headless Linux/macOS/Windows; no unnecessary new FFI. Examine .NET SDK/AOT deploy/size, mono/Android and iOS separately, not as one generic NativeAOT claim.
2. **C17 with stable opaque-handle C ABI**: strongest *interoperability hypothesis* for Swift/JNI/.NET/Python/Go/JS(WASM)/embedded, low per-render overhead. Memory-safety, ABI ownership, compiler/arch determinism and FFI integration cost are serious risks; test and sanitize. An internal C++ DSP implementation is admissible behind the same C ABI, not raw C++ ABI across runtimes.
3. **Go**: preferred for command-line, services, deterministic study orchestration and distribution; `gomobile` binding availability is not enough to assume hard real-time callbacks, zero runtime cost, fixed allocation or Swift/Kotlin API parity. Test `go test`, generated library size and real JNI/Swift FFI, heap allocations, worst-case jitter before suggesting a DSP renderer.
4. **Kotlin Multiplatform**: plausible Android/domain logic reuse, but requires proof on Apple/native interop, Win/Linux and web; does not automatically eliminate a cross-language DSP core.
5. **JUCE/C++**: qualify only if its extensive audio/plugin functionality is needed, under current JUCE license and cross-platform build evidence; large framework should not be imported for a two-biquad experiment.
6. **WASM**: distribution target, not a distinct algorithm authority. Browser AudioWorklet, cross-origin isolation, memory copy and worklet scheduling are separate runtime gates.

## Minimal portable ABI candidate — version `aural_kernel_v0` (experiment only)

- `aural_kernel_api_version() -> uint32_t`; reject unknown version/struct length.
- `aural_kernel_create(config*, handle**) -> status`; config immutable, validated before any output.
- `aural_kernel_render(handle*, int16_t* interleaved_stereo, uint32_t frames, uint32_t capacity_samples) -> status`; no allocations/syscalls/logging in render, bounded frames, caller-owned output and capacity checks. After STOP/end, valid render calls fill silence. On **invalid pointer/capacity**, return a typed failure **without writing** to an untrusted buffer; the owning OS audio adapter must explicitly mute/clear its own known-valid device buffer on any error. No implicit device control.
- `aural_kernel_stop(handle*) -> status`; idempotent, fail closed.
- `aural_kernel_destroy(handle*)`; ownership explicit, no use after destroy.
- `aural_kernel_measure_notch(centerHz,widthOctaves,sampleRate, result*) -> status`; pure numerical oracle may be a separate symbol.
- All structs `stdint.h` fixed-width with explicit size/version/units/endian and separately tested alignment; no exceptions across ABI, no C++ std types, no GC or native pointers in serialized records.
- State machine: `uninitialized -> configured -> rendering -> stopped/finished -> destroyed`; stop and interruption cannot resume without a new user authorization and instance. Repeated `stop` is a no-op.
- Render callback cannot allocate or block. Test exact integer PCM ceiling and buffer overrun guards. OS adapter still owns ramp if needed, route behavior and calibrated sound-level release gate.

**These are desired contracts, not claims that either candidate already implements them.**

## Comparable qualification envelope (version r11-probe/1)

Frozen comparison: current .NET source and exact SHA + 48 kHz/stereo/PCM16, bounds 200–10000 Hz, minimum 8 s maximum 15 s, fixed digital peak 0.08, deterministic logarithmic tone in both directions and ear selection, sample-rate explicit, oscillator phase continuous across block boundaries, end returns silence, notch measured -3 dB edges against existing `NotchResponseAnalysis` values. Different noise-color definitions, RNG arithmetic and filtering are not interchangeable until spectra/seed parity is established.

Independent synthetic proofs must include: version/schema and invalid-input errors; 1, 127, 1024 and 4096 frame chunk reproducibility; float finite checks; maximum absolute output and initial/final ramps; off-by-one at end; left/right zero isolation; coefficient and sample-state continuity; STOP, no autoplay and lifecycle, negative config; compiler sanitizers on Linux; static analysis as feasible. Cross-runner Ubuntu/Windows/macOS, arm64 cross-compile and wasm build are **separate gates**, not implied by one Linux success.

Performance methodology: release build, warmup, fixed test data, measured samples/second (throughput), p50/p95/p99 and **max block-time** at block 128/256/1024, heap allocations/render, working set and stripped binary size. A pass on average throughput is not a proof of real-time safety; validate on physical audio devices and under load. Compare implementation complexity, glue code/bridge size, package/ABI licensing, accessibility, runtime startup, distribution, cross-platform reproducibility and maintenance effort. A cheaper controller does not justify unsafe DSP callbacks.

**Disposition gate:** Keep .NET unless native candidate has proven numerical/behavioral parity, essential target feasibility, comparable or better lifecycle correctness, independent engineering review and materially lower total maintenance/deployment cost. If C17 wins, migrate one bounded DSP operation behind an adapter/feature flag and retain .NET oracle and rollback. If Go wins only for headless orchestration, adopt Go there and leave DSP in .NET/C; this is a valid split outcome. Do not change native UI as a prerequisite for core selection.

## Parallel workcells and dependency barrier

Sequential **first**: baseline R9/R10 source and immutable fixtures, ABI boundary and baseline safety/behavior criteria; no further migration work starts from guesses about unsupported runtime targets.

Then **parallel**, disjoint file owners:
- P11-A `experiments/portable-core/c17/**` plus dedicated C17 CI: C ABI/numerical/PCM probe (not production).
- P11-B `experiments/portable-core/go/**` plus dedicated Go CI: Go reference probe, allocation and packaging experiments (not production).
- P11-C `docs/architecture/**` and candidate matrix: native UX/runtime bindings for Android/iOS/macOS/Windows/Linux/browser/ARM, license and toolchain source review; no app UI migration or device permissions.
- P11-D existing R9/R10 CI and security/browser branches: monitor source-locked tests; unrelated historical failures must not block independent probes.

Sequential **integration barrier**: compare exact receipts to one frozen criteria set; peer-review cost/safety/FFI; choose **one** authority or reject migration; only then a platform integration vertical slice, physical/audio validation and explicit publication approval. Cross-branch merge and production switch are not delegated by this document.

## Current status / UNKNOWN

R9 convergence PR #16 is still open as of 2026-11-22, with R10 core/presenter branches #20/#23/#24 independent and private clinical/acoustic release gates open. Compiler/source parity is not physical audio qualification. We have **not** shown a C17 or Go runtime replacement works on a real Android/iOS device, or produced a current compiled WASM target. CI exists only when a run receipt has been observed.

External candidate documentation used during design: Go mobile https://go.dev/wiki/Mobile ; Android JNI https://developer.android.com/ndk/guides/jni-tips ; .NET Native AOT https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/ ; Kotlin/Native https://kotlinlang.org/docs/native-overview.html ; JUCE https://github.com/juce-framework/JUCE (verify license before reuse).

# R11 C17 ABI candidate — nonproduction

This deliberately small, **experimental** native C17 library tests whether a versioned C ABI provides a more portable foundation for Aural Observatory than a managed runtime. It is **not** a production replacement for `AuralObservatory.Core` and does not implement colored noise or moving-notch DSP. No OS audio, network, BLE, microphone, patient data or physical device access is present.

Public API: `aural_r11_api_version`, `aural_r11_tone_create`, `aural_r11_tone_frequency`, `aural_r11_tone_render`, `aural_r11_tone_stop`, `aural_r11_tone_destroy` and `aural_r11_measure_notch`. The opaque handle owns memory; render writes into a **caller-owned** PCM16 buffer, checks sample capacity and frame budget, and does not allocate. STOP is idempotent. Struct sizes and API version are explicit. These bounds are an **ABI prototype**, not proof of safe use from arbitrary FFI clients.

From repository root, run:

```sh
cc -std=c17 -O2 -Wall -Wextra -Werror -pedantic experiments/portable-core/c17/aural_r11_kernel.c experiments/portable-core/c17/conformance.c -lm -o /tmp/aural-r11-c17-check
/tmp/aural-r11-c17-check
```

Linux sanitizer qualification adds `-fsanitize=address,undefined -fno-omit-frame-pointer`. Public GHA runs are defined in `.github/workflows/r11-c17-probe.yml`. Fixture ownership is frozen in sibling `../fixtures/*-v1.csv`.

**Open:** compiler/OS/arch/ABI compatibility, library binary size, actual JNI/Swift/.NET FFI integration, moving-notch/noise/RNG parity to C#, real-time jitter under device load, memory safety fuzzing, WASM AudioWorklet, acoustic SPL and clinical governance. Passing this probe must not be described as a complete migration or clinical validation.

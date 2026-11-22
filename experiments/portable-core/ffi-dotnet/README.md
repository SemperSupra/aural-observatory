# R11 .NET ↔ experimental C ABI integration probe

This is a **bounded cross-language consumer test**, not a runtime migration: current C# `AuralObservatory.Core` remains authoritative and the C17 `aural_r11_kernel` candidate is loaded via `DllImport` on Linux. It proves that a separately compiled C ABI can be called from .NET 10 and that its 4 exact notch measurements agree with current `NotchResponseAnalysis`, while checking native PCM digital peak, ear isolation and STOP.

Reproduce from repository root on Linux:

```sh
cc -std=c17 -O2 -fPIC -shared -Wall -Wextra -Werror experiments/portable-core/c17/aural_r11_kernel.c -lm -o experiments/portable-core/ffi-dotnet/libaural-r11-candidate.so
LD_LIBRARY_PATH="$PWD/experiments/portable-core/ffi-dotnet" dotnet run --project experiments/portable-core/ffi-dotnet/NativeProbe.csproj --configuration Release
```

The public workflow `.github/workflows/r11-csharp-native-ffi.yml` runs the same synthetic proof. Unknown: ABI lifetime on Android/iOS, calling convention/signing/library packaging, actual callback scheduling, complete C# DSP parity, memory pressure, ARM, real audio calibration, production release. A passing P/Invoke test cannot justify automatic engine migration.

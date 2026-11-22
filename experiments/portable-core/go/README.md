# R11 Go candidate — nonproduction

A standard-library-only comparison implementation of **bounded logarithmic reference-tone sweeps** and **digital RBJ notch magnitude analysis**. It does **not** implement Aural's complete noise/filter/moving-notch DSP and does not access OS audio, BLE, microphone, storage, network, or patient data.

Run: `go test -count=1 -v ./...` and `go test -run '^$' -bench BenchmarkToneRender1024 -benchmem ./...` from this directory. The tests consume the frozen sibling `../fixtures/*-v1.csv` from R11 contract. Public GHA tests Windows/macOS/Linux in `.github/workflows/r11-go-probe.yml`.

API: `NewTone` validates immutable `Sweep` (ear, start/end Hz, 8–15 seconds, 48k); `Tone.Render` writes interleaved PCM16 into caller-owned blocks; `Stop` is idempotent; `MeasureNotch` is an analytic digital oracle. This is a **prototype** and is not approved for a real-time audio callback or playback. No medical inference or calibrated SPL. The Go garbage collector/runtime, `gomobile` binding limits, binary size, full cross-platform device callback performance and ABI/lifecycle integration must be measured separately before adoption.

Qualification status: SOURCE ONLY until exact public workflow receipts are inspected. Current .NET C# `AuralObservatory.Core` remains sole production DSP.

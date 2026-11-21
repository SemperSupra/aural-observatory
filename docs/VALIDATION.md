# Validation status (not clinical validity)

| Check | Status |
|---|---|
| .NET 10 native C# source authored | Yes |
| Android WebView absent / no INTERNET permission | Source reviewed |
| Deterministic DSP tests authored | Yes; unexecuted locally |
| Linux/Windows/macOS .NET core build | PASS — run 44928818567 |
| Android .NET 10 Debug APK | PASS — run 44928818567 (debug-signed) |
| UI / output on real Android hardware | Pending |
| Bluetooth and headphone transition tests | Pending |
| Physical acoustic SPL | Pending |
| Tinnitus symptom change demonstrated | No; no clinical claim |

The public GitHub Actions run https://github.com/SemperSupra/aural-observatory/actions/runs/44928818567 compiled and tested the core on all three runner OSes and produced a debug-signed APK. Physical device and sound pressure validation remain pending.

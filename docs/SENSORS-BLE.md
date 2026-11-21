# BLE Heart Rate sensor plugin v0.5 — integration and qualification

## Scope and component boundaries
- `AuralObservatory.Sensors` — framework-independent, versionable `IPhysiologicalSensor` interface, 0x2A37 parser, RR statistics, monotonic host timestamps.
- `AuralObservatory.Sensors.Ble` — C# BLE provider for Linux/BlueZ.
- `AuralObservatory.Sensors.Ble.Windows` — same source with Windows GATT backend; explicitly Windows-targeted .NET TFM.
- `AuralObservatory.Sensors.Ble.Android` — same source with native Android BLE backend, integrated into the .NET Android UI.
- `AuralObservatory.SensorConsole` — interactive Windows/Linux diagnostic and optional local CSV recording.
- The Mac desktop native BLE backend (CoreBluetooth/.NET macOS target) and iOS adapter are **future work**, not falsely claimed complete. The shared protocol and plugin contract do run on macOS as .NET code; a Mac-specific BLE runtime build is needed.

The Bluetooth implementation uses the MIT-licensed `InTheHand.BluetoothLE` 4.0.45, source: https://github.com/inthehand/32feet . Native OS BLE backends; no bundled WebView, no Garmin developer account, no cloud, no ANT+ dependencies. Target sensors: Garmin HRM-Pro Plus, Polar H10, GEOID HS500 and any SIG-compliant strap with Heart Rate Service 0x180D and characteristic 0x2A37. HRM-Pro Plus RR availability is **not yet confirmed** and must be measured on the user's device. This plugin does NOT support Garmin proprietary ECG, Garmin Connect, ANT+, or ECG waveforms.

## Windows 10/11 diagnostic
With .NET 10 SDK installed, from repository root:

```powershell
# Wear the strap; moisten electrodes; enable Bluetooth; select the HRM from the system device picker.
dotnet run --project src/AuralObservatory.SensorConsole/AuralObservatory.SensorConsole.csproj
# To persist explicit, local physiological observations (not committed to Git):
dotnet run --project src/AuralObservatory.SensorConsole/AuralObservatory.SensorConsole.csproj -- --csv "$env:USERPROFILE\\Documents\\aural-hrm-local.csv"
```

Expected: `HR=... BPM RR=... ms contact=...`. If `RR=NOT ADVERTISED` even though the heart rate is visible, that is evidence of an RR-absent BLE stream under the current conditions — not proof that the sensor hardware does not support RR via ANT+ or other modes. Test with competing phone/watch connections disabled and record firmware, connection state, and packet counts.

On Linux: install/enable BlueZ and a supported BLE adapter; run the same console command. macOS CoreBluetooth is not yet included in this CLI target; do not treat the Linux `net10.0` binary as a verified macOS BLE implementation.

## Native Android
The Android application has optional **Connect**, **Disconnect**, and **Record locally during preview** controls. No hidden background scan or upload. The Android 12+ BLE_SCAN and BLE_CONNECT runtime permissions are requested only on user action. Android 8–11 requires the OS's legacy location permission solely to discover nearby BLE peripherals; code does not request or use GPS/coordinates. The app disconnects when backgrounded. Readings are shown while foregrounded; local CSV recording is opt-in and limited to an active 15-second preview. Files stay in the app sandbox. Stream data is not exported automatically.

`MainActivity` presents BPM, RR values if present, contact status and exploratory RMSSD after sufficient RR intervals. No HRV feedback changes stimulus volume or frequency. End-user health benefits must be evaluated with self-reported outcomes, not asserted from RR/HRV.

## Bluetooth packet standard and parser
Bluetooth SIG Heart Rate Measurement characteristic 0x2A37 flags:
- Bit 0: unsigned BPM 8-bit vs 16-bit LE.
- Bits 1–2: contact detected / supported.
- Bit 3: optional 16-bit accumulated energy.
- Bit 4: zero or more 16-bit LE RR values at 1/1024 second per tick.

Parser rejects truncated/odd-length/zero RR payloads. Missing RR flag yields valid BPM-only observations. Each received packet carries UTC receipt and host monotonic Stopwatch ticks. Neither timestamp is a manufacturer sensor-side beat timestamp: packet batching, BLE scheduling and OS latency must be assessed before aligning with audio events. Per-packet sequence numbers and invalid/dropped counts aid transport quality analysis; loss estimates cannot identify every missed radio packet without device-side sequence counters.

RR-derived RMSSD uses an exploratory artifact gate: 300–2,000 ms accepted RR and adjacent differences over 20% flagged, with at least 30 accepted pairs required to publish a numeric estimate. This is NOT a clinically qualified HRV algorithm. Natural arrhythmia or movement can trigger exclusions; never interpret an HRV estimate as tinnitus relief or make medical recommendations on it.

## Verification gates
1. Unit fixtures: 8-/16-bit BPM, contact, energy, multiple RR, zero/malformed payloads, little-endian conversions, insufficient RMSSD data.
2. CI: pure sensor protocol tests on Windows/Linux, BLE plugin and desktop console compile, full .NET Android compilation. Compilation does not verify hardware acquisition.
3. Windows Dell Qualcomm QCA61x4A + Garmin HRM-Pro Plus: confirm discovery, CCCD notify, 5 min receipt, RR presence, drop/invalid count; repeat after competing-device disconnect.
4. Android device: permission prompts, pairing, preview simultaneous audio + BLE, screen lock/pause disconnection, file permissions, offline operation.
5. Time alignment: monotonic clock domain, drift vs output timeline; repeated baseline/test/recovery experiments.
6. macOS CoreBluetooth target and native UI bridge, followed by ANT+ adapter, are independent workstreams after BLE proof.

Do not check physiological recordings, BLE addresses, pairing information or any sensitive session logs into either public or private GitHub repositories.

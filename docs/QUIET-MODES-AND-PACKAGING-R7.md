# R7 — Quiet-input controls, hardware adapters and public packaging

## Input contract
- Select **HoldWhenQuiet**, **ReleaseWhenQuiet** (hold while audible; release when quiet), or **ToggleQuiet** before search.
- Physical press/release is converted to an explicit perceived-quiet boolean by `QuietInputController`; repeated edges are ignored.
- In inverse mode, unpressed before first down is **unknown**, never silently scored as quiet.
- Playback and post-playback are separate response phases; resets require explicit re-arm and never extend a masking interval into residual inhibition.
- Input mode is stored with each newly appended search observation; older CSVs are legacy format, do not append new columns to the old header without migration.
- Quiet responses represent subjective reports, not validated objective tinnitus detection.

## Sensors
- Existing BLE SIG heart-rate 0x180D/0x2A37 plugin is vendor-neutral; pairing/reconnect and RR availability depend on actual hardware.
- Extra useful adapters: pulse oximetry (0x1822), blood pressure (0x1810), weight (0x181D), cycling cadence (0x1816), body composition (0x181B); temperature (0x1809); optional EDA/EEG and respiratory sensors often proprietary. Integrate only after source and quality gates.
- ANT+ is not Bluetooth. A Windows, macOS or Linux PC needs an ANT-capable receiver (usually an ANT USB-m stick) for direct ANT reception. Existing QCA61x4A Bluetooth handles BLE but **not** ANT+. An ANT+ adapter is optional until BLE RR capability is proven on the Garmin HRM-Pro Plus.
- All adapters publish typed, timestamped observations with source, transport, units, quality, and local consent; no inference of tinnitus improvement.

## Build and Foundry boundaries
- Public product source repo: `SemperSupra/aural-observatory`. Public standard hosted runners compile and create *candidate* artifacts only.
- Public Android Publishing Foundry: reusable build/publishing contract and eventual store trust evidence; private companion holds operator approval/credential references. No unapproved store uploads.
- Public Windows Package Foundry: generic packaging/trust machinery; private companion owns exact release eligibility/validation. Do not publish generated WinGet/Chocolatey/Scoop metadata without private admission.
- This branch adds a **user-level NSIS Windows installer candidate**, desktop portable bundles, and a native Android debug APK candidate on SHA-pinned public workflows. None are signed, acoustically qualified, or production-approved; no live health data enters CI.
- Standard hosted runner placement (see authoritative `SemperSupra/agent-dispatch-private#223` and `#281`): `ubuntu-24.04` for Android (SDK/KVM), `windows-2025` Windows installer, `macos-26` Apple Silicon desktop, parallelism max 2 for desktop matrix; image features require run receipts and cannot be assumed from labels.

## Evidence and gates
- G7a static input edges + portable tests: CI pending.
- G7b Windows installer compiler + archive hashes on public runner: CI pending.
- G7c native Android build and output hashes on public runner: CI pending.
- G7d physical device, acoustic measurements, clean Windows install/repeat/upgrade/uninstall, and API/keyboard accessibility: pending.
- G7e store eligibility, signing, third-party notices, security review and explicit human publication: pending.
- Do not consume private-repository GitHub-hosted Actions minutes; do not copy or checkout private material from any public job.

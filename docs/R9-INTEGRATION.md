# R9 integration — desktop runtime convergence

Authority: public SemperSupra/aural-observatory branch integration/r9-desktop-runtime-convergence, based on main c97cac4. Source integration only; never signs, publishes or authorizes acoustic exposure.

## Workcell reconciliation
- R8-A (#9): actual emitted 48-kHz PCM16 PSD measurement, legacy blue/violet spectral mislabel confirmed; separate BlueCorrected / VioletCorrected variants preserve historic output; notch, channel and digital peak tests; no SPL calibration.
- R8-B (#10): versioned settings/presets with atomic OS-user-data storage, native Android light/dark/system accessibility settings and import/export, semantic desktop interface and fixed keyboard shortcut; actual assistive device tests pending.
- R8-C (#11): reproducible typed preregistered N-of-1 AB/BA protocol, JSON schema and safe synthetic tests. Not a clinical trial.
- R9-D (#13): desktop bilateral notch/frequency/width/gain, noise color and modulation editor with validation. Named preset first saves edited settings; new search applies changed profile only after active trial ends.
- R9-S (#14): per-user exclusive OS file lock; Kestrel OS-assigned loopback port; default browser launcher for GUI targets; --no-browser/--headless/AURAL_NO_BROWSER=1; authenticated exit. Still browser-backed, not native OS widgets.
- R9-Q (#15): real headless Chromium+axe on public runner with synthetic user preferences, validation, preset restore, appearance/zoom and 320px reflow, persistent JSON evidence; does not replace manual NVDA/VoiceOver/Orca.

## Human / automation / agent boundary
Humans authorize and control listening, clinical observations, sensors and data exports. Automation may run deterministic DSP and protocol tests with synthetic fixtures. Agents may propose offline experiments and read sanitized proof, never autonomously play acoustic output, change user gain, access protected health records, or publish.

## Deployment / user state
Desktop profiles and observations live in OS user data, independent of installed binary. Unknown/corrupt preferences are preserved instead of silently overwritten. Android app-private files survive compatible upgrades only, not uninstall; explicit user-triggered settings JSON import/export is supported. Source+test automation does not include personal physiology, credentials or actual recordings.

## Integration qualification
R7 passed public Windows installer lifecycle and Android/package matrix before main integration. R8-A/C previously passed public three-platform synthetic tests; this combined branch must still pass fresh CI. Every produced package is an unpromoted candidate. Actual headphone/hearing-aid response and dB SPL, audio route timing and dropout, BLE acquisition, real assistive technology, signed macOS/windows OS integration, clinical study governance and app store admission remain hard blocks.

## Repository hygiene
Only merged and CI-qualified integration source may update main. Standalone branches/PRs are traceable evidence, not separate sources of truth; once integrated close redundant workcells with pointers to the integrated SHA. See issue #12 for desktop UX remaining gates and private Foundry intake holds.

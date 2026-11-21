# R8 UI / UX / DX independent peer review and acceptance plan

## Evidence boundary (as of 2026-11-21)
Reviewed actual `MainActivity.cs`, SearchDesktop `wwwroot/index.html`, local server, shared `StimulusEngine.cs`, existing tests, foundry and package workflows. R7 source passes baseline compilation; prior CI does **not** prove accessibility or scientifically valid audio.

## Target application topology
- **Android:** truly native .NET Android Views, AudioTrack and BLE; device- and sensor-facing UI must follow Android system accessibility.
- **Windows/macOS/Linux desktop:** local-loopback browser UI + real C# DSP, *not* a native Windows/macOS widget app. Use semantic HTML/WCAG and the user's browser + OS assistive technology; keyboard, mouse, switch access and touch where supported.
- **Automation and agents:** reusable, deterministic C# core, no GUI dependency, constrained experiment planner, machine-readable JSON diagnostic receipts and stable exit codes. Do not expose a health-data API or authorize acoustic output without explicit human interaction.

## Information architecture / journeys
1. **First run and safety:** intended use, consent, sound level is NOT calibrated dB SPL, headphones/hearing-aid route, immediate STOP, first-use quiet baseline.
2. **Configure:** ear-specific hearing profile, playback output, presets, noise and transformation settings; identify experiment algorithm version.
3. **Explore:** finite AutoTune search; fixed volume; hold/release/toggle quiet; visible trial stage and precise during vs post-stimulus phase.
4. **Observe:** concise 0–10 loudness/distress, optional after-sound note/dictation, quality flags, side effects.
5. **Review and compare:** active/control plots, repeatability, confidence/uncertainty; no treatment claims.
6. **Sources:** opt-in BLE/ANT+, optional Garmin historical data; distinguish live sensor status vs data quality.
7. **Research protocol:** preregistered trial and consent; signed immutable stimuli and randomization evidence; independent confirmation.
8. **Export/manage:** local profile/preset migration, data export, optional user-triggered deletes, source provenance and privacy notice.

## R8 changes in this branch
- Core `UserPreferences` JSON v1: appearance system/light/dark, family system/sans/serif/mono, 80–200% text scaling, contrast, reduced motion, response mode, last two-ear stimulus and 50 distinct named presets, strict validation and atomic user-data save. Upgrade does not rewrite unknown future schemas or corrupt files.
- Desktop local REST settings/preset endpoints require per-session token, reject changes during active trials and retain user profiles outside install directory. HTML adds skip link, responsive semantic sections, status live regions, WCAG-oriented focus/contrast/48px controls, system-theme response and font scale.
- Android collapsible native Views group controls by journey, apply OS-aware light/dark colors/font sizes and high contrast, expose settings/presets and TalkBack-accessible click alternative for quiet button; no UI font/colour state stored in installed binary.
- User session data and preferences are separate from binaries. Windows per-user installer update/uninstall retains data stored outside `$INSTDIR`; app-specific Android `FilesDir` survives normal updates but is deleted on app uninstall. Export/import and migration are separately required release gates.
- `ReduceMotion` stored; no new UI animation to disable yet. System accessibility setting wins where applicable; app-specific scale multiplies it.

## Critical red-team findings / pending acceptance
- **HARD BLOCK for general release:** missing physical acoustic calibration. A digital ceiling of 0.08 FS cannot guarantee a safe SPL through headphones or hearing aids.
- Android TalkBack, large font (200%), Switch Access, external keyboard, rotation, landscape, hardware volume handling, color blindness, dark theme, voice intent permissions and app background/foreground transitions require actual-device tests. Code compilation alone is insufficient.
- Desktop NVDA+Firefox/Edge, macOS VoiceOver+Safari, Linux Orca+Firefox, zoom/reflow to 400%, keyboard focus, screen readers' announcement cadence, text size and reduced motion need OS qualification.
- Profile import/restore, interrupted or partial file saves, schema downgrade/upgrade migration and secure deletion require additional deterministic tests.
- App may not assume playback through intended output route; hearing aids can compress/EQ sounds, invalidating measured virtual HRTF; describe actual output path in every session's provenance.
- Workflow cancellation/non-serialized playback and mutable UI state require concurrency review. Avoid relying on a mouse/touch down event for accessibility: a Click path must work too.
- Desktop loopback server is user-session-only, but should consider Origin header hardening and no accidental external binding. It is not automatically a safe remote administration API.
- Voice transcription may invoke a cloud speech provider. It requires explicit opt-in and a clear privacy explanation.
- Input labels, fonts, accessibility status and colors are usability improvements, not evidence of WCAG AA conformance until manual/automated independent audits pass.

## Test checklist
- Core JSON migration guard, roundtrip full ear preset, atomic local save, corrupted/future schema preservation, duplicate replacement, invalid gain/font reject.
- Desktop HTML JS parse and .NET server compilation on standard public Linux/Windows/macOS runners.
- Android native compile on public Ubuntu SDK runner.
- Device-driven UX smoke scripts: start/STOP, recording consent, BLE pairing abort, quiet button TalkBack double tap, 200% text, dark/system theme, filter controls, export, app upgrade and OS process death recovery.
- Run keyboard-only with no pointer; screen reader + low vision; motion sensitivity; slow device; disconnected sensor; mismatched audio route and high-frequency hearing loss.
- Separate automated accessibility scanner results from human verification evidence; never claim completed without both.

## Definition of done
A release candidate requires platform-native design checklists; annotated screen recordings or accessible testing results; finite test plans and defect ledger; physical acoustic/latency qualification; profile upgrade restore tests; signed release and reproducible provenance; no uncontrolled health-data telemetry.

## Second-pass desktop source audit / corrections
- **Fixed in R8-B source:** Space keyboard shortcut ignored focused native button, input, select, textarea, links and ARIA interactive roles, preserving browser control activation. See `tests/ui_static_check.js` for static guard; actual keyboard test remains open.
- **Critical missing feature:** Desktop HTML has no two-ear audio editor; `/api/preset` persists `LastStimulus`, which users currently cannot edit from that page. Implement typed/config-validated form, separate explored/default experiment state, and ensure export contains correct settings.
- **Desktop installer/shell:** Windows NSIS provides a shortcut and local app installation but actual foreground browser window launch, single-instance, clean exit/service teardown and crash handling are not verified. macOS/Linux currently ship archives, not OS-native application bundles.
- **DX:** Pure C# DSP/protocol schemas are sound foundations, but there is no stable plugin/CLI specification or source-described per-platform API discoverability. Preserve human approval for audio/sensor actions and add synthetic developer samples/fixtures.
- Required follow-up tracked as https://github.com/SemperSupra/aural-observatory/issues/12. Desktop CI build/static checks do not assert native OS or WCAG compliance.

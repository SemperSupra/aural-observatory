# R13 Mobile/tablet touch controller — Open Space Duel UX crosswalk

Status: **source-only experimental desktop browser/controller workcell** under [Aural issue #5](https://github.com/SemperSupra/aural-observatory/issues/5), [R12 controller #29](https://github.com/SemperSupra/aural-observatory/pull/29), and current [Way of Working](https://github.com/SemperSupra/engineering-governance-private/blob/main/WAY_OF_WORKING.md). User-provided references: [Open Space Duel public](https://github.com/mark-e-deyoung/open-space-duel) and private-source development authority `mark-e-deyoung/open-space-duel-private`. **Do not reproduce private game source or unpublished evaluator material in the public Aural repository.** This is an independent original implementation inspired by publicly describable interaction patterns. It has no dependency on or copy of Open Space Duel modules.

## Existing source study and *bounded* analogies

The private Open Space Duel `src/ui/joypad.ts` uses a floating thumbstick whose origin follows the initiating touch; a separately addressable left-side touch region; right-side buttons; touch identity tracking; a deadzone and radial indication; release/cancel resets; portrait/landscape repositioning with safe-area insets; optional orientation/tilt permission; and Gamepad API per-device calibration. Related original source and mobile tests live in `src/game/input/gamepad.ts`, `src/ui/controlCalibration.ts`, `tests/e2e/mobile-controls.spec.ts`, and `tests/e2e/mobile-smoke.spec.ts`. Its public README explicitly describes virtual mobile thumbstick, action buttons and opt-in tilt.

**Differences in Aural:** ship rotation/thrust/fire have no acoustic equivalent. The source motion mapping is used only for **sound azimuth, elevation and relative distance**; there is no continuous acceleration, fire, automatic playback, randomized trial scoring or synthesized browser tone. Aural's existing C# `StimulusEngine` generates the bounded WAV and R12's browser `PannerNode` applies optional HRTF to the manually authorized, unscored **bilateral reference-tone** preview. It must not reinterpret subjective quiet-search observations.

## R13 workcell implementation

- `src/AuralObservatory.SearchDesktop/wwwroot/mobile-controller.js`: A separate contextual, on-demand **full-viewport touch overlay** opened by an explicit button in the R12 spatial section. Two independently tracked pointer IDs support simultaneous left/right thumb input. Each analog base appears where its touch began and uses clamped displacement plus a small deadzone. Left drag sets head-relative azimuth; right drag sets bounded elevation and relative distance. The underlying R12 sliders, preview eligibility, and HRTF settings remain the single source of position truth—**no duplicate DSP** or hidden sound-control path.
- `mobile-controller.css`: Portrait/landscape and phone/tablet grid layout with CSS safe-area insets, `100dvh`, bounded high-contrast targets, responsive 40–48px minimum action sizes, reduced-motion support, and touch-action restriction only on the stick surfaces (not on the full app).
- The overlay prominently offers STOP, Close + STOP, Mark, Center, and **Play prepared only**. It never generates, prepares, starts, or resumes sound merely from touch movement or orientation. HRTF can be explicitly toggled using the existing guarded R12 setting, which refuses ear-specific/notch playback.
- Pointer end/cancel/blur/resize/orientation clears active IDs and hides floating controls. Dismissing the overlay stops active/pending manual audio through existing local STOP; hidden app also closes/stops. Existing randomized study and source are unchanged.
- The overlay declares an accessible modal with inert background, labelled controls, keyboard arrow/Home operation and focus restore. `Escape` closes and requests STOP. It does not trap the user or disable zoom globally.

**Critical deployment boundary:** The existing **desktop** server binds only `127.0.0.1`; a phone/tablet on another machine **cannot access that server via LAN**. The overlay is testable using browser touch emulation and a *same-device* browser shell. **The Android product is native .NET Views/AudioTrack, not a WebView.** This new browser overlay does NOT make the native Android app implement touch spatialization. Native Android and iOS/tablet adapters must be separate platform-owned workcells using the same typed UI intents under their own host authorization; do not open a network listener to make this HTML reachable from arbitrary devices.

## Acceptance matrix and authority

| Gate | Automated criterion | Status |
| --- | --- | --- |
| M0 source pattern and ownership | Reference actual Open Space Duel source; independent original Aural files, public-safe explanation only | IMPLEMENTED |
| M1 touch pointer isolation | Two simultaneous active zones, independent pointers, bounds/deadzone, cancel/blur/orientation and stop-on-exit | CI REQUIRED |
| M2 responsive/acoustic guard | Portrait 390×844, landscape 844×390, 768×1024 tablet, 320px reflow; no scrolling/gesture interception beyond control; HRTF opt-in bilateral-only; no autoplay/randomized scoring | CI REQUIRED |
| M3 accessibility | Real browser keyboard/axe check, modal close/focus, accessible STOP, pointer alternatives | CI REQUIRED; MANUAL SCREENREADER UNKNOWN |
| M4 hardware/controller | Real Android touch, iPadOS Safari, physical Xbox and RumblePad2, screenreader/tilt permission | HOLD |
| M5 physical sound | Real authorized host START/STOP, route/buffer flush, calibrated headphone dB SPL, HRTF localization | HOLD |
| M6 release | Owner/source/security acceptance and exact artifact qualification | HOLD |

**Independent next batches:** dedicated public Playwright mobile/desktop interaction audit on a child PR; native Android touch surface/input-only candidate on R10 Android without opening sound; threat review for STOP and user activation. **Sequential:** M1–M3 CI/source acceptance → native platform adapter and actual hardware/OS input → owner-approved acoustic qualification → authorized integration/release.

## Minimal actor reentry

An agent starts with the root `AGENTS.md` (currently added on R11 PR #25, so use that branch to read until it is integrated), canonical Way of Working, [Aural issue #5](https://github.com/SemperSupra/aural-observatory/issues/5), and this file. Read the current PR head, changed paths and CI. Claim one **disjoint** READY workcell, report model/effort/body/OS/CWD/repo/branch/resource budget, execute parallel batches when dependency-safe, publish exact SHA / public run / failure receipt, reconcile the same issue and leave a bounded next baton. No clipboard text beyond that pointer, no direct main mutation, no production host or device permission implied.

# R15 — OSD-pattern card workspace (desktop loopback)

**Authority:** [Aural issue #5](https://github.com/SemperSupra/aural-observatory/issues/5); R12 desktop spatial controller PR #29, R13 mobile #30, R14 corrected keyboard focus #36. This work is stacked on #36 and intentionally preserves existing C# DSP, study scoring, token-authenticated routes and STOP handlers. No main merge, actual acoustic exposure, deployment, or clinical approval.

## Source-grounded pattern and implementation

The private Open Space Duel `mark-e-deyoung/open-space-duel-private/src/ui/menu.ts` implements a 4-card horizontally navigable carousel, 44px next/previous actions, position indicators, compact-landscape adjustment and an always accessible bottom action bar. **We reuse the interaction pattern, not its private source or art.** Aural's navigation uses genuine native tab semantics for equivalent keyboard and assistive-technology behavior, with one visible workspace at a time and source-owned sections moved rather than cloned.

| Card | Source sections | Reason |
| --- | --- | --- |
| Sound setup | `#configuration` | Two independent ear configurations and safety context |
| Spatial lab | `#manual-sweep`, `#spatial-controller` | Must prepare/STOP a C# manual tone while moving the existing HRTF source, without a hidden control |
| Quiet search | `#experiment`, `#response`, `#feedback` | Keep active and post-stimulus observation in the same journey |
| Preferences | `#appearance` | Appearance/presets separate from active sound |

New source-owned `card-deck.js/css` are served by the **existing local loopback host**, not copied to the visual-only public PWA as if it had real sound. The compact deck has 44px tabs and prev/next, visible current-card text, keyboard Left/Right/Home/End, real native control IDs and handlers, a sticky STOP that dispatches the **existing** stop controls, theme inheritance, and narrow-screen reflow. Physical audio silence remains unmeasured. A session-only card preference is cosmetic; no health/biometric data added to storage.

Safety invariant: an active QuietSearch stage (including after-stimulus observation) or a pending/playing manual sweep **blocks navigation to another card** until STOP. The sticky STOP remains available throughout; original manual/audio stop actions retain authority and are not reimplemented in a new endpoint. Spatial HRTF remains opt-in and manual bilateral reference-tone only. A separate R14 mobile full-screen controller has its own persistent STOP and modal focus management. No auto-play, gain increase, stereo/mono change, device route change, new token, or user journal mutation is introduced.

## Verification and release holds

- New dedicated public GitHub Actions matrix executes **real local C# server** with Chromium/Firefox/WebKit across 320px phone, compact landscape, tablet and desktop. Oracles check card exposure, original control reachability, keyboard navigation, stuck/pending state fencing, STOP, no autoplay, overflow, target size, and automated axe; screenshots and JSON receipt exported by engine.
- R9 browser, R13 mobile-touch and R14 adversarial browser tests are adjusted to select the explicit visible workspace rather than implicitly relying on a long-scroll DOM. These are *test navigation changes*, not disabled tests.
- This card UI is a source-only draft until exact-head CI and browser evidence pass, and remains subject to real-phone landscape, keyboard/AT and hardware/audio acceptance.
- The static Pages visual PWA in PR #37 is a **separate** release projection and does not imply native/desktop engine availability on GitHub Pages.

## Audio correction / research boundary

User reported that existing HRTF sounded weak on ordinary stereo speakers. This is **not** evidence of a defective browser implementation alone. Browser PannerNode HRTF targets binaural two-ear reproduction; loudspeakers introduce acoustic crosstalk unless independently calibrated cancellation is applied. The implementation uses a fixed conservative post-render 0.25 gain and a 8–15s C# generated *swept reference tone*, which changes pitch during movement and does **not** provide a standardized broadband localization signal. Existing CI qualified UI routing and synthetic controls, **not** psychophysical localization or headphone SPL. The Spatial lab now states headphone recommendation and speaker limitation directly. Full acoustic RDTE belongs to a separate independent experiment, with unattended audio output forbidden.

## Decisions and dependency sequence

1. ACCEPT review of compact card navigation on a stacked draft; do **not** merge it past R14-F and prerequisite R9–R13 source before exact CI and owner integration agreement.
2. KEEP one production C# StimulusEngine. Any broadband/static spatial audition stimulus must be separately bounded, user-started, unscored and DSP-parity-qualified before exposure.
3. SPL/STOP hardware and physical headphone hearing tests remain independent release blockers; software-only Panner output tests are diagnostics, not acoustic acceptance.
4. No extra credential or private CI authorizations required for synthetic public workflows; local actor is only needed for actual headphone, controller, Android/speaker HIL tests after explicit human presence and approval.

**Actor baton:** Read issue #5 + AGENTS, exact R15 PR and parent #36; claim *disjoint* HRTF offline metrics and physical input-only workcells; record model/effort/host/CWD/refs/resource budget, exact receipts, blockers and next action in GitHub. Do not copy this plan through chat, share a branch with another actor, trigger raw audio output unattended, or use paid private CI.

# R14 — Aural brand, public SPA/PWA projection and release gates

**Status: CANDIDATE / VISUAL-ONLY WEB PREVIEW.** Authority: Aural issue #5 (https://github.com/SemperSupra/aural-observatory/issues/5), project docs/DLE.md and adopted engineering Way of Working. This does not promote R9–R14 draft features, authorize unattended sound, or claim clinical effectiveness.

## Two governance scopes reviewed

SupraCraft/brand-governance-private: AGENTS.md, BRAND_CONTRACT.json v0.3.0-candidate, POLICY.md, docs/BRAND_ARCHITECTURE.md, docs/public-surface-affordances.md. SemperSupra/engineering-governance-private: AGENTS.md and WAY_OF_WORKING.md. SupraCraft's brand is scoped to hobby, creative-play, and games; Aural is a SemperSupra acoustic research instrument. Reuse transferable design discipline (project-native metaphor, semantic HTML, accessible theme control, provenance, stable machine facts), **not** SupraCraft's logo/producer attribution or mandatory house palette. No adopted or managed SupraCraft status is inferred.

**Candidate Aural identity:** an observed acoustic source with opposing signal arcs, rendered as one distinctive compact mark. The source point and arcs communicate observation/spatial control, not a diagnosis, hearing measurement, treatment effect or certified sound-pressure level. Exclude medical crosses, brain anatomy, audiogram results, generic AI neural imagery, copied Open Space Duel assets and third-party device marks. At 16–24px prove recognizable positive/negative-space rendering before brand signoff. Product name remains canonical ASCII "Aural Observatory".

### Semantics and visual tokens

| Role | Light | Dark |
| --- | --- | --- |
| Canvas | #F4F7F5 | #0E171D |
| Card | #FFFFFF | #18252C |
| Primary text | #15262B | #F1F8F6 |
| Secondary text | #495E62 | #C4D7D6 |
| Action / signal | #066967 | #A2E2D9 |
| Focus outline | #AF6200 | #FFD47F |
| Warning foreground / background | #6C3B00 / #FFF4DF | #FFDA92 / #42301C |

Contrast calculations for the exact token pairs: action on card ~6.51:1 light/~10.76:1 dark, secondary ink on card ~6.86:1/~10.48:1, warning ~8.51:1/~9.39:1. Those figures are token arithmetic, **not** end-to-end WCAG conformance. Check rendered states, hover, disabled, forced-colors, physical screens and zoom. Primary font: system sans UI native fallback; diagnostic readouts: tabular numerals/system monospace. No external font licensing, downloaded brand font, animation dependency or network theme service.

**Interaction contract:** 44 CSS px or larger targets, one System/Light/Dark radio group with accessible labels/checked states, visible focus, semantic headings, reflow, reduced motion, prominent STOP in any audio-capable variant; brand colors map to native Android/Desktop roles, not pixel-identical widgets. Humans use accessible application surfaces, automation stable typed test/artifact interfaces, agents the same factual authority; neither machine audience can bypass local human physical START consent.

## Standalone Pages PWA slice in this PR

The public-repo branch feat/r14-pwa-publication-brand-review owns web/ and its dedicated qualification/Pages workflows. It stages a static offline-capable visual PWA with icon, identity, theme preference only, native touch/keyboard/pointer controls for visual azimuth/elevation/distance, one clear RESET, a service worker limited to its GitHub Pages base path, and app-info.json for machine/agent discovery. There is **no** DSP, generated audio, biometrics, API backend, journal, account, telemetry, or private repository runtime dependency. This is a preview—not the finished audio-enabled SPA/PWA.

GitHub Pages candidate address: https://sempersupra.github.io/aural-observatory/ . Site must NOT be called live without an independently verified HTTPS response. Production workflow deploys only web/ after main merge and valid CI; Pages repository Settings -> Pages must select GitHub Actions as its build source, if not already configured. Public/free hosted Actions only. The current PR does not merge its own code or alter the local actor's R13/R14 branches.

## Independent concurrent workcells

| Cell | Scope | Status |
| --- | --- | --- |
| P1 Public visual PWA | Unique visual language, static shell, service worker, machine metadata, CI and publication wiring, separate branch | SOURCE STAGED; CI/release verification pending |
| P2 Browser sound engine | Reuse authoritative C# core through qualified WASM path, or separately validated C ABI; seeded 48kHz PCM, notch PSD, ramp, silence, stereo and stop parity; no forked production WebAudio DSP | REQUIRED FOR AUDIO RELEASE |
| P3 Form factors/UI | Existing R13-M/Q and R14-J/B browser work; real Chromium/Firefox/WebKit, portrait/landscape, 320px, 200/400% zoom, accessibility, touch identity/STOP | IN PROGRESS |
| P4 Android & three-audience API | Existing R13-N native input and R14 agent/automation work; local human authorization; lifecycle | IN PROGRESS |
| P5 Red-team release gates | Device routing/SPL, actual silence/STOP, CSP/service-worker updates, privacy, source/public diff, license/provenance, live Pages assertions | PENDING |

Sequence: independent source/CI evidence -> identity review & static preview acceptance -> merge to main -> enable/deploy Pages -> verify actual live origin and offline install -> preserve visual-only scope. For **sound-enabled PWA**, require additionally matched authoritative DSP output, human start/STOP, browser/host lifecycle, device/headphone calibration, actual iOS/Android browser HIL, manual screen-reader/zoom reviews and explicit owner acceptance. A published web URL alone does not satisfy these gates.

## Visual and compatibility test matrix

- 320/360/390 phones, landscape 844, 768/1024 tablets, 1280/1440 desktop; System/Light/Dark, color contrast, zoom 200%/400%, forced-colors, reduced motion and high DPR. Compare exact screenshot and browser receipt, not subjective style alone.
- Keyboard-only tab/arrow/Home/End, theme native radio semantics, no lost focus, sensible 44px target size, accessible live status, visible RESET, visual values duplicated as text, no accidental acoustic START.
- Chromium/Firefox/WebKit synthetic matrix as early evidence, distinct from physical iOS Safari/iPad and Android TalkBack, NVDA/VoiceOver/Orca device qualification.
- Service-worker install/update/offline fallback and base-path isolation; icon inspection at 16, 24, 40, 192, 512 logical px; iOS PNG home-screen icon is still pending.
- No personal health/audio data in the Git tree, no external CDN, no claims of validated tinnitus treatment or safe sound pressure, no network/device permissions in this static preview.

## Release ledger

- [x] Repositories and governance boundary examined.
- [x] Project-native branding candidate documented, semantic roles and contrast token calculations recorded.
- [x] Original SVG mark, offline visual PWA source, manifest, scoped service worker and machine endpoint staged on a separate branch.
- [x] Public no-audio/static contract test and pinned Pages deployment workflow authored.
- [ ] Exact-head public Actions run green; responsive screenshot and axe/browser findings reconciled.
- [ ] Independent visual/UX and public/private source exposure review.
- [ ] Reviewed main merge and actual GitHub Pages Settings configuration.
- [ ] HTTPS Pages origin, install, offline reload and update-cycle receipts.
- [ ] Audio-core/WASM or ABI parity, physical acoustic STOP/SPL and native/accessibility HIL.
- [ ] Explicit release approval for live audio-enabled SPA/PWA.

**Stop conditions:** identifiable health data, private source/credentials, remote audio START, misleading medical/acoustic claims, unsigned or unreviewed dependency, stale branch/CI, inaccessible primary controls, or impossible recovery from offline update.

## Durable actor return baton

Read governing Aural #5, applicable AGENTS.md and canonical Way of Working; report actor/model/effort/runtime/branch/resources and claim disjoint P2–P5 workcells. Continue in independently parallel, bounded reps; post exact SHA/test receipts to GitHub DLE. Do not overwrite the public-preview branch, copy chat walls, spend private CI minutes, promote R9–R14 prototypes or generate uncontrolled sound.

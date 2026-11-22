# R14 — Three-audience journeys, UI/UX/DX, and product release gates

Status: **AUDIT/QUALIFICATION WORKSET, not product-ready**. Owning [Aural Observatory DLE #5](https://github.com/SemperSupra/aural-observatory/issues/5); canonical [AGENTS.md](https://github.com/SemperSupra/aural-observatory/blob/docs/r11-portable-core-decision-contract/AGENTS.md) and [Way of Working](https://github.com/SemperSupra/engineering-governance-private/blob/main/WAY_OF_WORKING.md). Source baseline: desktop R9/R10, native Android R10/R13, desktop spatial R12, contextual touch R13. Open Space Duel's private joystick code informed interaction patterns, not imported code.

## Executive design issue

The current **single C# DSP core is an engine boundary**, not an admitted three-audience API. The current desktop is a C# native loopback service at `127.0.0.1` with a local web UI and ephemeral token; the native Android app is not a WebView. An emulated mobile browser on Linux/Windows/macOS is *not* a deployed remote phone app, and the existing mobile touch screen on Android is **input only**. The DLE lists atomic operations such as `audio.start`, `audio.stop`, `observation.export`, but that list does not establish a stable exposed agent API or complete headless CLI.

**Recommended target:** one versioned, idempotent and permissioned capability contract; multiple native/browser/CLI/agent projections of the same underlying validated domain actions. Human takes priority, and only a trusted device host can confirm actual audio effects. Do not invent a network agent server, make Android a WebView, or expose the local token/public sample journal to agents merely to claim parity.

## Independent test and evaluation lanes

| Lane | Audience / scenario | Qualification venue | What is NOT proven |
|---|---|---|---|
| R14-J journey/evidence baseline (this PR) | Human/Automation/Agent goals, steps, positive/negative oracles, authority + 12 platform channels | 3-OS pure Python stdlib manifest linter; JSON + Markdown reports | No simulated flow represents users, HIL, or a released API |
| R14-B adversarial browser | Keyboard-only modal loop, Escape STOP, focus/reflow, alternate pointer, touch cancellation, high contrast, 200% zoom, error handling | Public Linux Chromium/Firefox/WebKit browser CI, axe, screenshots, triage of failures | Screenreader user study, Safari/iPad hardware, real USB controller, SPL |
| R14-N native/action parity | Android native touch+STOP and human-priority gate vs CLI/agent capability; typed operation truth | Public 3-OS .NET core source/tests and Android compile, static admission audit | Android emulator/user devices, actual headphone STOP, exposed agent API |
| R14-H HIL/UX research | One-handed motion, fatigue, thumb reach, real Logitech/Xbox axes, TalkBack/VoiceOver, route disconnect | Separate explicitly authorized on-device local actor; no private recordings | Cannot substitute synthetic CI for actual humans/hardware |

## Accessibility/admission references

- W3C [WCAG 2.2](https://www.w3.org/TR/WCAG22/) criteria include **2.5.7 dragging movement alternative**, **2.5.8 target size**, **2.4.11 focus visibility**, keyboard operation and contrast; treat **AA** as target on web and use [WCAG2ICT](https://www.w3.org/TR/wcag2ict/) where relevant to native apps.
- WAI-ARIA [modal dialog guidance](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) requires focus containment, Escape and appropriate focus placement/return. A `role="dialog"` plus `aria-modal` alone does not implement modal keyboard behavior.
- Playwright emulation can exercise browser touch/viewport/reduced motion; **not** iOS Safari hardware or real Android touch firmware.

## Journey evidence states, not a vanity percentage

`not_demonstrated → planned → input_only/test_harness_only → source_compiled → emulated_browser/browser_tested/ci_qualified → real_device_qualified`.

These are **categorical**, not mathematically linear readiness ranks. The JSON manifest deliberately declares **zero** physically qualified journeys until there is real device/locale/accessibility/HRTF provenance. The Python linter enforces negative outcomes, principal authority, no false native-device claims, and complete coverage of 12 product surfaces. It generates `artifacts/evaluation/r14/{machine-scorecard.json,human-scorecard.md}`.

Don't convert the number of green CI jobs into a release confidence score. An agent and a human may **both read** state but must not **both acquire** the same side-effect authority. STOP/lifecycle cancellation and human preemption dominate agent requests; untrusted automation cannot forge a host-stopped event.

## Parallel allocation and gates

**Parallel:** R14-B browser adversarial, R14-J manifest/operational DX, R14-N native/agent capability audit, local-actor hardware input-only mapping/Android TalkBack feasibility when devices exist. Distinct branches, path ownership, public runner matrices, no paid/private GHA, all findings as DLE receipts.

**Sequential:** establish required journeys and authority contract → run adverse/equivalence oracles → fix P0/P1 failures → ensure independent requalification exact SHA across relevant variants → real physical headset/USB/gamepad/accessibility tests → owner decides release. Never silently merge/force-push or perform acoustic playback.

## Review verdict by dimension (source-based, not exhaustive)

- **Human UX**: promising R13 adaptive touch and R12 desktop gamepad, but input/cognition/screen-reader outcomes and native spatial sound remain *not demonstrated*. UI may not be reachable from a separate phone given loopback host.
- **Automation DX**: source build and deterministic fixtures exist; a documented list of operations is **not** the same thing as a supported versioned CLI for each operation.
- **Agent DX**: an independently authenticated agent control API, session/revision/Epoch/CAS, rights scoping, fail-closed direct STOP and human preemption are **not present as an accepted Aural runtime**. The separate Audio Foundry reducer prototypes do not change this.
- **Platform parity**: desktops source-qualified, Android compiled input-only, mobile WebKit/Chromium **emulation** tested, iOS/iPad native support unimplemented, physical gamepad/HRTF/STOP and device install tests HOLD.
- **Privacy/safety**: no public user recordings/biometrics; explicit human start and separation between manual preview vs randomized trial remain hard gates.

**No new repository, separate orchestration layer, or chat baton.** Keep current GitHub Issue #5 and the R14 evidence report as the machine-readable work queue for all actors.

## Concurrent local-actor visual PWA (distinct release surface)

The local actor has independently opened [R14-P #37](https://github.com/SemperSupra/aural-observatory/pull/37): a separate public visual-only installable-candidate PWA, with `app-info.json` explicitly saying no acoustic output or remote control. This is **not** the R13 loopback desktop audio UI, not a remote phone control plane, not the native Android product, and not yet an owner-approved audio release. The R14 journey manifest now tracks `visual_pwa` separately with H11/A08/G07 journeys. Read-only metadata is an automation/agent-facing fact source, **not** an authenticated agent command API. Parent #37 is owned by the local actor; this review does not mutate its files.

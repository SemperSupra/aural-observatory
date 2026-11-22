# R14 red-team / blue-team UI, UX, DX and multi-audience release review

Authority: [Aural DLE #5](https://github.com/SemperSupra/aural-observatory/issues/5), [R14 machine journey contract](JOURNEYS.json), [R14 root note](README.md). The three independently authored R14 workcells are [journey inventory #33](https://github.com/SemperSupra/aural-observatory/pull/33), [adversarial browser #34](https://github.com/SemperSupra/aural-observatory/pull/34), [native/agent parity #35](https://github.com/SemperSupra/aural-observatory/pull/35), with separately staged [modal focus repair #36](https://github.com/SemperSupra/aural-observatory/pull/36). **Review is source/CI-specific and not a certificate.**

## What the review actually evaluated

- **Human jobs:** first-use clarity and mode selection; explicit manual preview consent; repositioning sound with touch, mouse, keyboard and optional gamepad; STOP/close/background; no corruption of randomized trials; export and error recovery. Each has a positive and adversarial negative oracle.
- **Automation jobs:** deterministic profile validation, synthetic sound generation, provenance, UI/Android regression qualification, interruption, idempotent retry and privacy-safe export. Existing test harness supports some work but versioned CLI coverage is not established by DLE operation names.
- **Agent jobs:** capability discovery, session revision/epoch observation, constrained proposal, human preemption over concurrent requests, trusted host effect authority and error escalation. These remain **proposed interfaces**, not a deployed permissioned API.
- **Ergonomics and form factors:** desktop 1280px, constrained laptop 640x360, narrow phone 320x568, phone portrait 390x844 and landscape 844x390, tablet 1024x768; actual native Android source build, existing emulated mobile browser CI; no physical iPhone/iPad or gamepad qualification.
- **Accessibility:** keyboard-only; Tab/Shift+Tab cycles; pointer vs single-pointer/keyboard alternative to drag; accessible button target and clipping geometry; Escape and focus restoration; reduced motion and dark mode; automated axe. True TalkBack/VoiceOver, switch-control and hearing/comfort research still require human HIL.

## Finding register (priority is release/user impact, not completed CI)

| ID | Severity | Source-derived/adversarial evidence | Blue-team corrective action | Admission state |
|---|---|---|---|---|
| F01 | **P1 accessibility** | [R14-B initial run 45024513600](https://github.com/SemperSupra/aural-observatory/actions/runs/45024513600) reproduced modal focus escape in Chromium/Firefox/WebKit | [R14-F #36](https://github.com/SemperSupra/aural-observatory/pull/36) explicit Tab/Shift+Tab containment and focusin guard; requalify all 3 engines | **FIX PROPOSED**, exact-head CI required; do not merge |
| F02 | **P0 physical safety** | C# source, R13 tests only prove an accepted STOP request; no device-output buffer silence, amplifier/headphone dB SPL proof | Host-owned direct STOP, route interruption and acoustic measurement; keep human preemption independent of persistence | **HOLD** |
| F03 | **P1 agent DX/authority** | [R14-N #35](https://github.com/SemperSupra/aural-observatory/pull/35) source inventory of actual local token routes vs DLE operation list; no dedicated agent-control principal/CAS/lease protocol | Approve minimal versioned capability contract, read-only first, errors/receipt, human approval gate; do not expose local session token to network | **NOT IMPLEMENTED IN AURAL RUNTIME** |
| F04 | **P1 deployment parity** | Desktop host binds `127.0.0.1`; R13 browser test emulates mobile but is not a separate phone app | Distinguish packaged desktop browser, native Android, PWA feasibility and iOS/iPad roadmap; keep LAN auth closed | **HOLD for separate-device UX** |
| F05 | **P1 Android functional parity** | `NativeSpatialTouchSurface` changes position metadata only; Android AudioTrack not spatially connected | Native qualified HRTF/positioning behind native DSP/route/audio permission; real-device testing | **INPUT ONLY** |
| F06 | **P1 evidence honesty** | Android and desktop R13 sources are unmerged sibling PR stacks; no single binary with both verified | Read both exact branch SHAs; form accepted integration graph only after owner permission and regression | **QUALIFIED SOURCE INVENTORY, UNMERGED** |
| F07 | **P2 localization** | `index.html lang=en`, English-only controller labels; no qualified German/Thai locales | Extract strings, UI fit/pseudolocales, human review and voice/screenreader tests for EN/DE/TH | **GAP** |
| F08 | **P2 touch reach/accessibility** | R13 responsive tests and R14 geometric 40px screen targets; not individual finger reach or motor impairment | One-handed ergonomic/large-font testing, target-size heuristic, landscape collision/zoom, hardware gestures | **AUTOMATED BROWSER ONLY** |
| F09 | **P1 controller hardware** | Browser synthetic Gamepad API is not an actual Xbox or Logitech Rumblepad 2 | Input-only HIL with sanitized device axis/button traces, disconnect/reconnect and STOP; native approved HID option only if required | **HARDWARE UNKNOWN** |
| F10 | **P1 privacy/DX** | Local personal CSV and preferences are real project features but raw data must not enter public agents/cloud CI | Scope export, provenance, explicit consent, redaction/audit and per-principal privacy review | **HOLD for agent export** |
| F11 | **P1 multisession concurrency** | No admitted human+automation+agent shared-session arbitration with revision/CAS and explicit OS effect confirmation | Prove STOP priority, stale epoch fencing, idempotent immutable receipts and multi-audience views in qualified runtime | **DESIGN ONLY** |
| F13 | **P1 public deployment parity** | Actor-owned [R14-P visual-only PWA #37](https://github.com/SemperSupra/aural-observatory/pull/37) declares `acoustic_output=false` and `remote_control=false` in `app-info.json` and has separate offline shell. Not a released audio-enabled PWA or merged R13 desktop | Qualify standalone app-info/offline/Pages, keep control API absent and avoid inferring native platform parity | **VISUAL-ONLY SOURCE CANDIDATE** |
| F12 | **P2 browser portability** | Playwright Firefox rejects `isMobile:true`; touch+viewport emulation is not mobile-UA support | Separate Firefox touch/viewport path, preserve exact browser failure, do not call it physical mobile | **TEST HARNESS FIX #36** |

## Evaluation method and realistic metrics

**Evidence ladder:** source-read → offline contract and mutation/negative tests → desktop real browser and native compile → OS emulator/HIL → real input device and accessibility task observation → physical headphone/acoustic qualification → owner release. No level is promoted merely by counting green checks.

Acceptance oracles for future real-user studies must record *task completion without assistance*, number of unintended starts, time to find STOP, ability to recover after loss of focus/connection, whether the user can tell "sound planned" vs "sound actually playing/stopped", and whether a one-handed or assistive-tech user can reposition or cancel. Device acoustic stop latency and allowed SPL need an owner-approved measurement protocol; **no arbitrary safety threshold is imposed by this audit**.

Automation DX metrics: reproducible single-command setup, stable machine-readable error/exit status, deterministic hashes, clean cancellation, bounded retry/no blind retries, versioned provenance, synthetic fixture-only CI; agent DX adds principal identity, session revision, human interruption receipt and no spoofed physical effect. Current source supports some tests but **does not meet full contract**.

## Independent parallel batches and owner boundary

| Cell | Source ownership | Public runner | Objective acceptance |
|---|---|---|---|
| R14-J | `docs/evaluation/r14/JOURNEYS.json`, `scripts/evaluation/r14/check_journeys.py` | Ubuntu/Windows/macOS Python stdlib | All 3 audiences and 11 surfaces, positive+negative paths, explicit UNKNOWN, reproducible scorecards |
| R14-B | `tests/desktop-e2e/r14-adversarial-ux.cjs` | Chromium/Firefox/WebKit on disposable Ubuntu | 6 responsive layouts × 3 engines; real browser STOP, keyboard containment, no token bypass, no autoplay, axe |
| R14-N | `scripts/evaluation/r14/native_agent_dx_audit.py` | Ubuntu/Windows/macOS Python stdlib and separate Android native build | Source truth across unmerged siblings, no false agent API/Android spatial claims |
| R14-F | `src/AuralObservatory.SearchDesktop/wwwroot/mobile-controller.js` only | Inherits R14-B + R13 real-browser CI | First-failing focus regression → repaired cross-browser negative oracles |
| Local actor HIL | Actual allowed Windows/Android devices in separate worktree | Local physical host only when available/authorized | Real controller mappings, TalkBack, physical STOP and route evidence; never fabricated |
| Review-only peer | Separate docs/review branch after all PRs | Source/PR/CI read-only | Adversarial independent acceptance, ownership and no unsafe merge |

**Sequencing:** R14-B failure → R14-F fix → rerun exact source against all engines → combine reviewed source branches only with owner authority; validate Android native and browser separately → actual human/device/accessibility/acoustic HIL → production acceptance/merge/sign/release. Audit and independent actor work may continue concurrently until this integration barrier.

**Human remains highest priority.** No local actor, CI job or agent is authorized to bypass deliberate human audio-start control, modify patient records, expose private session tokens or claim hardware silence. Preserve all failing-first receipts and correct at source; do not inflate success counters or copy chat walls.

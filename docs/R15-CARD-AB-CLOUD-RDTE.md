# R15 card vs long-scroll — autonomous free-runner RDTE

**State:** source/CI evaluation proposal. Exact status and output SHA are recorded in governing [Aural issue #5](https://github.com/SemperSupra/aural-observatory/issues/5) and this PR. No merge or product acceptance is implied.

## Comparison validity

- Two **navigation conditions on the same C# locally hosted app and source controls**. A (classic): the R15 deck's original live section nodes are moved back into the old long-scroll order, the original anchor navigation is revealed, the deck removed, and pre-deck 780px body width restored in the browser-only harness. B (new): the R15 four-tab workspace runs unmodified.
- Both conditions have identical underlying section elements, event handlers and C# generated audio authority. No playback, user trial, medical data or remote API authority is involved. This is a **synthetic UI-path/geometric comparator**, not a human study: the script already knows target IDs and task destinations. Layout includes as-shipped width differences on desktop; this is a product-system comparison, **not** a pure card-nav isolation. Future width-normalized control could separate navigation from responsive geometry.
- Public Ubuntu disposable runners matrix: real Chromium, Firefox, WebKit × phone 320×568, phone landscape 844×390, tablet 768×1024, desktop 1280×800 × original/classic vs R15. **24 independent browser view sessions, 12 matched pairs**, nine task targets per session. The browser uses real DOM and event layout rather than a mock screenshot.
- Measured: actual document vertical height, deterministic minimum scrolling displacement from known starting tasks, explicit anchor/tab navigation actions, overflow at native/150% typography, unqualified interaction safety, serious/critical axe, page errors and external network requests. Report screenshots + JSON for each engine.
- STOP validation: the new tabbed workspace must preserve an on-viewport sticky STOP deep inside the spatial UI; no interaction may start the study or manual audio. Original long-scroll has no equivalent globally sticky STOP; this is a source/UI affordance observation, **not physical device silence evidence**.
- Fail tests on actual unreachable task targets, missing nav, STOP not visible in a deep card, unexpected audio start, serious/critical automated axe, remote requests or more than 2px horizontal overflow at either native or 150% text.

## Task specification

| Sequence | Original layout | Tabbed workspace | Task target |
| --- | --- | --- | --- |
| 1 | header Configure | Sound setup | left ear frequency |
| 2 | manual scroll | Spatial lab | manual preview mode |
| 3 | header Spatial controller | Spatial lab | azimuth |
| 4 | header Explore | Quiet search | hold-quiet control, no actuation |
| 5 | header Appearance | Preferences | theme select |
| 6 | manual scroll | Spatial lab | manual preview ear select |
| 7 | header Explore | Quiet search | existing study STOP |
| 8 | header Configure | Sound setup | save audio settings control, no actuation |
| 9 | header Appearance | Preferences | export preferences control, no actuation |

**Critical caveat:** automated scroll distance does not equal human search effort; neither an interaction-action count nor 5% improvement threshold proves a design superior. Natural task order, expertise, multitasking, text zoom, motor abilities, perceptual comprehension and user preference must be studied with consenting people separately. Keep the old layout as a reversible test fixture until that is done.

## Existing first-failure, independent of this new work

The latest R15 parent R9 Chromium UI smoke [run 45031951575](https://github.com/SemperSupra/aural-observatory/actions/runs/45031951575) **failed** at 320 CSS pixels with 150% font size: `scrollWidth=326px` vs `innerWidth=320px`, six pixels overflow. Do not conceal this behind the dedicated R15 card matrix PASS. The new harness logs offending element geometry at enlarged font across both layouts to determine the smallest source-owned correction and retain a red→green evidence trail.

## Follow-on independent workcells

1. Source-diagnose any 150% overflow by identifier from JSON; fix minimal CSS/markup on a separately scoped proposal, then run inherited R9/R13/R14/Android checks.
2. Synthetic keyboard/voice-over navigation and task grouping review using actual accessibility tree; no unsupported AT claim from automated axe.
3. Compare horizontal tab visibility at real 200% zoom, 320px portrait and 844px compact landscape; test an always-visible STOP after scroll and while the SPA is in an active state.
4. Human research acceptance: controlled counterbalanced long-scroll vs tabs, consent, sampled participants and genuine task timing/error/preference. This **cannot be completed autonomously by an unattended CI runner** and remains HOLD without actual participants.

**Ownership/authority:** public free-tier CI and source-only experiments; private engineering value and hardware/acoustic/release permissions remain governed separately. Report exact commit, synthetic environments, results and first-failure to #5. Never infer a sound benefit from a UI benchmark.

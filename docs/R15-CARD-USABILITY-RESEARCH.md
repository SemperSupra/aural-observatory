# R15 UX research — cards, tabbed workspaces, paging and safety-critical controls

**2026-11-22 / source-grounded evidence workcell.** Parent [Aural DLE #5](https://github.com/SemperSupra/aural-observatory/issues/5), draft PR #38, [canonical Way of Working](https://github.com/SemperSupra/engineering-governance-private/blob/main/WAY_OF_WORKING.md). This note does not override real-device, medical/acoustic STOP/SPL or release gates. It is a reviewable source/evaluation protocol, not an assertion that Aural has been human-tested.

## Correct taxonomy (do not treat distinct patterns as synonyms)

| Pattern | Purpose | Aural/OSD application | Research and test implication |
| --- | --- | --- | --- |
| **Card UI / common-region grouping** | Discrete bounded content containers; often multiple per page | Acoustic setting groups, status summaries, metadata | Helps visual grouping; does not establish optimal navigation axis or information architecture |
| **Tabbed workspace / in-page tabs** | Small named set of selectable `tab` + `tabpanel` views | **R15 Aural #38**: Sound setup, Spatial lab, Quiet search, Preferences | Keep tab labels short, discoverable, keyboard-operable, logical task grouping, few enough to remain findable |
| **Horizontal card carousel / scroll-snap slider** | Swiping/arrowing through peer cards with progress indicators | **OSD** `src/ui/menu.ts`: horizontally scrollable four-card menu with 44px arrows, dots and persistent controls | Do not automatically assume this is faster than tabs; discoverability, hidden content and gesture conflicts require tests |
| **Hypertext deck-of-cards / paged UI** | One page at a time like turning book pages | A different possible model, not current Aural | Inferiority to vertical reading scroll depends on content/task and size; preserve browsing within long card |
| **Wizard / stepper** | Sequential task completion with progress and prerequisites | Future opt-in calibration/onboarding, not recurring lab use | Never require sequential completion to access standalone user controls |
| **Accordion / progressive disclosure** | Expand secondary content in place | Optional advanced DSP and debugging options | May be preferable to many tabs on compact mobile or when only one section is long |
| **Dashboard / split-view master-detail** | Simultaneous observation and operation | Candidate desktop 'active session + parameters' monitoring mode | Required where users must compare, monitor or retain uninterrupted access |
| **Persistent safety/utility controls** | Independent context and visible high-priority action | Existing browser STOP in sticky tab header, R13 modal own STOP | Must remain visible and operable in every reachable active state; no auto-play or gain change |

## What human research does and does NOT support

1. **NN/g, “Cards: UI-Component Definition,”** distinguishes bounded card containers from *deck-of-cards* page navigation, and describes the Gestalt **common region** cue. That establishes rationale for *grouping*, not that replacing vertical scroll with swipe carousel improves task performance. https://www.nngroup.com/articles/cards-component/
2. **NN/g, “Tabs, Used Right” (2024 update)**: appropriate for a *small number* of concisely labeled content groups **when users do not need to see information from two tabs simultaneously**. Hidden tabs and switching can increase memory and interaction cost; users may ignore non-default content. It warns tab-list overflow undermines discovery and accordions may outperform tabs on small screens. **Aural implication:** never split a preparation control and a required live monitor across tabs; keep STOP/active status persistent, avoid hiding essential safety disclosures. https://www.nngroup.com/articles/tabs-used-right/
3. **W3C WAI-ARIA APG Tabs Pattern:** `role=tablist/tab/tabpanel`, `aria-controls`, `aria-selected`, focus/arrow keys, wrapping from last to first, Home/End options, and activate-on-focus where panels are preloaded with no noticeable latency. **Aural implication:** native tabs, accessible labels, roving focus and Left/Right wrapping (source change and browser-oracle added in this workcell). Automated axe alone is not manual AT acceptance. https://www.w3.org/WAI/ARIA/apg/patterns/tabs/
4. **Warr & Chi, CHI 2013, “Swipe vs. Scroll: Web Page Switching on Mobile Browsers”:** a controlled mobile browser-page switching experiment reported *vertically stacked card switching* faster and less frustrating than horizontally swiped page switching, without significant error-rate differences. Different domain/task from acoustic controls; not a direct Aural effect estimate. https://research.google/pubs/swipe-vs-scroll-web-page-switching-on-mobile-browsers/
5. **“An experiment with content distribution methods in touchscreen mobile devices,” Applied Ergonomics 2015:** empirically compared scrolling/paging/internal links for reading web documents; favored vertical scrolling on mobile *unless content is sufficiently large*, when paging can be appropriate. Do not ban normal vertical scrolling *within* the active Aural tab. https://pubmed.ncbi.nlm.nih.gov/25959321/
6. **Baymard, “Ecommerce Mobile App UX Trends” (2026):** tall horizontally scrollable areas can hijack intended vertical scrolling and cause accidental taps; important areas may justify exception. Transfer cautiously from ecommerce to precise audio controls. Do not create a full-viewport swipe surface behind sliders/multitouch gamepads. https://baymard.com/research-articles/mobile-app-ux-trends
7. **NN/g “Carousel Usability” and W3C WAI Carousels:** hidden slides may be missed; clear large controls and no unexpected auto-rotation are necessary. The interactive Aural workspace should *never auto-advance*. https://www.nngroup.com/articles/designing-effective-carousels/ ; https://www.w3.org/WAI/tutorials/carousels/
8. **NN/g “Progressive Disclosure”:** advanced/secondary controls should be disclosed on demand while preserving learnable primary paths. This is a design principle supported by research, not a quantified Aural improvement. https://www.nngroup.com/articles/progressive-disclosure/

**Evidence grade:** cited works include practitioner synthesis, accessibility normative/authoring patterns and a small number of empirical experiments in *other* domains. There is **no Aural-specific comparative human usability result** here. Synthetic browser PASS proves functional/accessibility oracles for tested inputs only, not superiority over the prior long-scroll page.

## Comparative human evaluation design (hold until consent/participants exist)

**Pre-registered design:** Compare same source/controls in (A) original linear long-scroll and (B) R15 four-tab workspace. Use counterbalanced within-person task order A→B/B→A, balanced device profiles and no actual acoustic stimulus or health data needed for UI-only tasks. Keep baseline and variant layouts available as reversible study fixtures; do not enable clinical/real audio output in remote CI. Start with ~5–8 diverse moderated formative testers to find faults, then separately plan a powered quantitative experiment if an improvement claim is required. Do not treat 5–8 participants as statistical evidence of superiority. Include keyboard-only, screen reader and touch participants as available; test user journey and form factor per audience rather than relying on synthetic events.

**Task set (same end states, no audio):**
- Locate and set *both* ear parameters, reject an out-of-range value, save a valid configuration.
- Find and load a previously stored synthetic profile, and explain when it applies.
- Locate manual preview controls and position controller, identify that HRTF is opt-in and headphones are recommended.
- Navigate from Spatial lab to Quiet search without losing a simulated active-state/STOP invariant.
- Find STOP from every tab, from narrow/landscape viewport and with 200% text zoom, without accidentally invoking PLAY.
- Find advanced appearance/export controls and recover from invalid import.
- Explain (unaided) which features are experimental/unqualified and which can start sound.
- Use gamepad/keyboard/touch alternatives and return focus from R13 modal; no sound emission.

**Primary outcomes:** independent task success rate, critical safety task success/incorrect PLAY rate (zero unintentional START tolerance), time-to-task, count of navigation steps, wrong-tab selections, interaction-level errors, visible STOP reachability and recovery from invalid state. **Secondary:** total scroll distance, number of cross-panel transitions, subjective Single Ease Question per task / post-variant SUS, perceived control and cognitive load, accessibility and localization readability. Less vertical scrolling alone is a *secondary metric*, not proof of improved usability. Do not collect identities, audio recordings or medical records. Use anonymized/local sanitized summaries only.

**A priori hypothesis and tradeoff:** R15 should reduce finding costs and scroll for independent functions while retaining equal-or-better task success and zero critical safety regressions; may worsen workflows requiring simultaneous monitoring or cross-tab comparisons. Predefine acceptable task-success deltas and completion-time ranges from pilot power/variance rather than inventing universal thresholds. Preserve a persistent safety/status region outside tabs; consider a split-screen desktop mode only if cross-tab switching proves costly. Consider an accordion or visible segmented picker on narrow mobile if tab overflow obstructs discoverability.

## Engineering gates and current evidence

- **R15 PR #38**: real C# source-backed Chromium/Firefox/WebKit × 320px phone, phone-landscape, tablet and desktop, plus inherited R9/R13/R14 tests. No claim of natural-touch/mobile Safari or human task time.
- **New standard conformance:** Left from first tab wraps to last and Right from last wraps to first; Home/End and no-playback tested per browser.
- **Visible small-screen choices:** at widths ≤500 CSS pixels the tab strip becomes a 2×2 grid with all four labeled destinations visible; prev/next arrows hidden in this breakpoint, because they would duplicate visible tabs. Normal vertical panel scrolling is unchanged. A new actual-browser geometry oracle checks 44px minimum targets and no tab clipping. This is a reversible UX hypothesis pending comparative human tests.
- **Release HOLD** until real-device/manual accessible UX acceptance and parent source integration; no user claim that card navigation is intrinsically superior.
- **Next disjoint workcells:** behavioral A/B harness with synthetic task receipts, screen reader/real touch input-only audit, hard STOP/status visibility audit, keyboard/arrow/focus cross-browser review. Local actor reports actual model/effort/runtime and GitHub receipts in issue #5, not copied chat prompts.

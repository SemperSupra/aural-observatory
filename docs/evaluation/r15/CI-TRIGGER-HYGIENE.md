# R15 CI trigger and repository housekeeping — event de-duplication

**Authority:** [Aural issue #5](https://github.com/SemperSupra/aural-observatory/issues/5), [R15-U test #40](https://github.com/SemperSupra/aural-observatory/pull/40), canonical Way of Working v0.0.4. This is an independent bounded CI-hygiene proposal, not an owner release authorization.

## Observed concrete duplication

The public R15 A/B three-browser workflow had both `push` targeting its own feature branch and `pull_request` events with the same source-path filter. Once its PR exists, the same source commit can cause both event families to start independent 3-engine jobs. On exact source `6bd0492d1954038dc4d1651272c431046e17efd1` the accepted, redundant paired runs include:

- `45033075566` — same-source R15 controlled A/B run, completed successful
- `45033080315` — same-source R15 controlled A/B run, completed successful and selected for canonical immutable artifacts/manifest.

Both ran on the public free-tier GitHub-hosted workers; this was **not** paid/private GHA consumption, but duplicated browser downloads, C# setup and test CPU. The earlier red `45032815655` is historical fault evidence and must not be deleted or misrepresented as a green run. Historical receipt run IDs remain stable after any workflow change.

## Minimal correction

In **`.github/workflows/r15-card-ab-rdte.yml`**, retain **only `pull_request`**, preserving every exact `paths` selector, SHA-pinned checkout/setup/artefact actions, read-only `contents:read` token, timeout, fail-fast-false, 3-engine matrix and always-upload evidence. Remove only the duplicate feature-branch `push` event. The source branch is already under a draft PR, so PR-triggered public Actions remain discoverable for source-only and CI/harness changes. The run may still restart if a new source commit supersedes an old one; `cancel-in-progress: true` is intentional because source and outcome changed, not a blind retry.

No `workflow_dispatch`, release, main-push privilege, hosted secrets, private runner, dedicated GHA PAT, Pages publishing or audio is added. Other inherited workflows also have independent triggers; those are **not** modified by this narrow ownership cell. Before additional changes, inspect their project-specific branch/PR semantics and acceptance contracts.

## Acceptance

1. Static unified diff shows the **only** modified YAML lines are removal of the feature-branch `push` event block.
2. Child draft PR source records exact SHA; the PR-triggered public A/B job completes in Chromium/Firefox/WebKit on that head with published artefacts, no disabled scenario.
3. No duplicate independent **R15 A/B** branch-push run for the same new head from this workflow (check run event type).
4. Keep all historic runs as evidence; no cancel/retry to manufacture green or to erase a red receipt.
5. No cross-project CI governance activated by a single project-local change.

**Limitations:** This does not audit all parent-branch workflows or guarantee a single run from all sources. The full merged product stack remains draft, and neither cloud tests nor workflow hygiene prove physical audio safety.

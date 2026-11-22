# AGENTS.md — Aural Observatory

This repository follows [canonical Way of Working](https://github.com/SemperSupra/engineering-governance-private/blob/main/WAY_OF_WORKING.md) and [agent delegation policy](https://github.com/SemperSupra/engineering-governance-private/blob/main/policies/agent-delegation.md). Read these, then [docs/DLE.md](docs/DLE.md) and the current issue/PR before mutating any source. Do not copy governance text into a second manual.

## Owning surfaces and entrypoints

- Public product `SemperSupra/aural-observatory`: code, tests, public-safe source evidence, experiment candidates. Current continuing work authority: [issue #5](https://github.com/SemperSupra/aural-observatory/issues/5) and R9 convergence PR #16; R11 portability candidate [qualification note](docs/architecture/R11-PORTABLE-CORE-QUALIFICATION.md).
- Private companion `SemperSupra/aural-observatory-private`: nonpublic decisions and release governance, NOT live user records/secrets. Raw health/audio/biometric journals stay on authorized user-controlled devices only.
- `AuralObservatory.Core` (C# .NET 10) remains the only qualified source-of-truth DSP; R11 C17 and Go experiments are **nonproduction** until independently accepted. Never add a second silently active DSP.
- Android is native .NET Activity/AudioTrack; desktop has C# generated WAV and local loopback browser. Distinguish platform UI, core DSP, OS audio/sensors, experiment control, release governance and qualified evidence.

## Working rules

Run small independent disjoint workcells, each with bounded owned paths/branch, repeatable synthetic checks, exact commit/run receipts and no hidden human clipboard baton. Continue from current DLE and open PRs, not chat-only claims. Keep old fixtures/implementations for comparisons. Do not mutate `main` directly or merge, sign, publish, change repository visibility, promote a candidate, or use paid/private CI without explicit applicable authority. A public PR and green CI are evidence, not production acceptance.

Never place raw personal medical/audio records, BLE identifiers, session tokens, account credentials, app signing material or other secrets in Git or public cloud CI. Playback requires explicit human authorization; STOP and lifecycle cancellation are safety invariants. Digital sample ceiling is not calibrated acoustic SPL. Real-device/headphone and ethics/clinical gates remain HOLD.

Human: discover the UI and safety status in README/docs. Automation: `dotnet run --project tests/AuralObservatory.Core.Tests` and relevant synthetic CI workflows; R11 experimental probes have their own README/tests. Agent: use issue #5 as baton, inspect exact branch and CI status, change only the owned workcell, emit immutable receipts and next bounded action. No new orchestration layer or repository just to coordinate this work.

# R14-N — Native-platform, automation and agent DX acceptance

**Review-only independent lane** based on R13-N #32; no Android source is altered here. Owns only `scripts/evaluation/r14/native_agent_dx_audit.py`, its free public 3-OS CI, and this note. A green report **means the source-derived inventory is accurate against selected code patterns**, not that automation, agent parity, OS authorization or acoustic output has been implemented.

## What actually exists today

1. **Human desktop**: a local-only `127.0.0.1:0` ASP.NET Core host launching a browser, session token in local HTML and required for `/api` operations. Manual preview via bounded C# WAV, optional R12 HRTF, separate randomized trial controls.
2. **Human Android**: native .NET Android Views/AudioTrack. R13-N dual-pointer view records spatial position but **does not route to an Android spatial DSP**; STOP and lifecycle interruption are native owner functions. No WebView.
3. **Automation**: deterministic C# core/test entrypoints, platform packages, and pinned CI. The DLE's atomic operation list does **not** itself constitute a stable CLI with typed errors, session IDs, idempotency, testable exit codes, versions and capability negotiation.
4. **Agent**: no independent agent principal/session authorization, human-preemption priority, idempotent effect ledger, CAS or authenticated host-observation service in the Aural runtime. Separate Audio Foundry proposals/experiments cannot be silently declared deployed here.

## Required three-audience use-case contract (future proposal, NOT implementation)

| Projection | Discover/read-only | Propose/request | Execute effect | Emergency STOP |
|---|---|---|---|---|
| Human UI (desktop/native) | Session state, limits, preview/study mode, quality/host evidence | User-gestured prepare, route and positional intent | Human-consented device host only | Direct local host/device path, always available; fail-closed state |
| Automation CLI/headless | `capabilities`, `validate`, `render`, `receipt`, export approved synthetic fixtures | Idempotent request with expected revision/target and bounded rate | Only offline synthetic/pinned source unless explicitly granted host authorization | May request STOP; result distinguishes accepted intent from observed silence |
| Agent API | Typed capabilities/schemas, scoped session/epoch, provenance and limitations | Read-only recommendations and conditional change proposals | **Never** impersonate user gesture; independent approval and trusted host required | Human preemption dominates agents; agents cannot retry into resumed playback |

**Invariants:** Single highest-priority human control per session, STOP fencing stale/cached START, revision and epoch CAS, distinct command admission vs host effect observation; no raw biosignals or journals in public CI; no remote port by default; one versioned contract shared across UI/automation/agent without forcing one executable runtime into Android or browser. Any signed protocol requires separately authorized owner decision, threat review, privacy classification, idempotent trace and an OS-first STOP path not blocked on DB.

**Operational DX:** A new actor first reads the same DLE and a machine-readable `capabilities` *once it exists*, then uses `plan → proposed changes → execute allowed action → validate → reconcile receipt`. When an action is unsupported, return a typed `NOT_IMPLEMENTED`/permission result rather than inventing a URL. Include examples for English, German, Thai labels as needed; localization itself is not implemented merely by this document. Expose developer error examples (wrong-target, stale revision, stale host epoch, loss of human consent, untrusted device) before adoption.

## R14 audit gate

The offline source auditor reads current `Program.cs`, Android `MainActivity`, native touch view and C# projector, and DLE. It inventories actual token-protected local endpoints and maps each DLE operation to its source implementation/evidence. It rejects claims of qualified agent APIs, missing native lifecycle STOP, WebView replacement, or sound creation inside the touch-only view. It writes human `source-parity.md` and machine `source-parity.json` artifacts with zero real-device and zero agent claims.

**Stronger follow-on, gated:** A separate independent branch could implement a stdio/offline *read-only capabilities CLI prototype* against the real portable C# core; then contract tests with per-operation receipts and typed errors. Do not expose `audio.start` or local records to network agents, and do not call a proposed CLI production until cross-session authorization, safety, privacy and owner acceptance.

## Owner and release readiness

- **Parallel** to R14 browser/adverse work and R14 journey-manifest review.
- **Sequential**: approve journey contracts and audience authority; prove session revision, human preemption and trusted host; implement native device STOP and physically measure it; perform actual Android/tablet input/a11y and phone/browser HIL; integrate, sign and release only with separate owner approval.
- No real audio, raw medical data, shipping deployment, new repo, private actions or direct main merge.

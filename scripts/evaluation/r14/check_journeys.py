#!/usr/bin/env python3
"""Offline, reproducible three-audience journey/evidence admission report.

This validates data quality and reports gaps. A passing linter is NOT a
passing UI, hardware, accessibility or agent-production acceptance test.
"""
import json
from collections import Counter, defaultdict
from pathlib import Path
import re
import sys

BASE = Path(__file__).resolve().parents[3]
DOC = BASE / "docs/evaluation/r14/JOURNEYS.json"
REPORT = BASE / "artifacts/evaluation/r14"
AUDIENCES = {"human", "automation", "agent"}
ALLOWED_AUTH = {"human", "human_required", "host_verified", "human_preempt", "read_only", "automation"}
APPROVED_OS = {"windows_desktop", "macos_desktop", "linux_desktop", "android_phone",
               "android_tablet", "iphone", "ipad", "desktop_web", "mobile_browser",
               "visual_pwa", "automation_cli", "agent_api"}
STATES = {"not_demonstrated", "planned", "input_only", "test_harness_only",
          "source_compiled", "emulated_browser", "browser_tested",
          "ci_qualified", "real_device_qualified"}
KNOWN_STUDY = {"H05", "H06", "H07", "H08", "H09", "G04", "G05", "A06"}
MATRIX_ORDER = ["windows_desktop", "macos_desktop", "linux_desktop", "android_phone",
                "android_tablet", "iphone", "ipad", "desktop_web",
                "mobile_browser", "visual_pwa", "automation_cli", "agent_api"]
def main():
    body = json.loads(DOC.read_text(encoding="utf-8"))
    errors = []
    if body.get("schema_version") != "aural.journey.evaluation.r14":
        errors.append("unknown schema_version")
    if body.get("release_authority") != "HOLD":
        errors.append("non-qualified evaluation cannot authorize release")
    if set(body.get("platforms", [])) != APPROVED_OS:
        errors.append("platform inventory differs from required form-factors/channels")
    if set(body.get("states", [])) != STATES:
        errors.append("status vocabulary does not match conservative admission policy")
    cases = body.get("journeys", [])
    if len(cases) < 21:
        errors.append("required multi-audience journey inventory incomplete")
    by_audience = Counter()
    by_state = defaultdict(Counter)
    evidence = body.get("evidence_catalog", {})
    ids = set()
    for case in cases:
        cid = case.get("id", "?")
        if not re.fullmatch(r"[HAG][0-9]{2}", cid) or cid in ids:
            errors.append(f"{cid}: invalid or duplicate ID")
        ids.add(cid)
        actor = case.get("audience")
        if actor not in AUDIENCES:
            errors.append(f"{cid}: unknown audience")
        by_audience[actor] += 1
        if len(case.get("steps", [])) < 3:
            errors.append(f"{cid}: lacks beginning-middle-end journey steps")
        for required in ("goal", "entry", "positive", "negative", "precondition"):
            if not case.get(required):
                errors.append(f"{cid}: missing {required}")
        if case.get("authorization") not in ALLOWED_AUTH:
            errors.append(f"{cid}: unspecified authority")
        targets = case.get("support", {})
        if set(targets) != APPROVED_OS:
            errors.append(f"{cid}: missing complete cross-platform support classification")
        for name, level in targets.items():
            if level not in STATES:
                errors.append(f"{cid}:{name}: unknown status {level}")
            by_state[name][level] += 1
            if level == "real_device_qualified":
                errors.append(f"{cid}:{name}: no physical device qualification receipt established")
            if level in ("browser_tested", "emulated_browser") and name in {"android_phone","android_tablet","iphone","ipad"}:
                errors.append(f"{cid}:{name}: cannot equate emulated browser with device HIL")
            if level == "ci_qualified" and name == "agent_api":
                errors.append(f"{cid}: agent API not actually deployed/qualified")
        refs = case.get("evidence", [])
        if not refs or any(ref not in evidence for ref in refs):
            errors.append(f"{cid}: invalid/absent evidence key")
        if not case.get("releaseBlockers"):
            errors.append(f"{cid}: must disclose unqualified owner/device gates")
        if actor == "agent" and case.get("authorization") == "automation":
            errors.append(f"{cid}: agent can not impersonate OS human presence")
        if actor == "agent" and any(
            x in ("ci_qualified", "real_device_qualified") for x in
                (targets.get("agent_api"),)):
            errors.append(f"{cid}: no admitted agent interface in this repo")
    for actor in AUDIENCES:
        if by_audience[actor] < 5:
            errors.append(f"only {by_audience[actor]} {actor} journeys")
    if not KNOWN_STUDY.issubset(ids):
        errors.append("missing STOP, device, trial, a11y, agent-preemption critical paths")
    REPORT.mkdir(parents=True, exist_ok=True)
    gaps=[]
    for case in cases:
        live=[p for p,v in case["support"].items() if v == "real_device_qualified"]
        gaps.append({"id":case["id"],"audience":case["audience"],
                     "release_ready":bool(live) and not case["releaseBlockers"],
                     "blocking_gates":case["releaseBlockers"],
                     "supported_in_real_hardware":live,
                     "automation_or_agent_entry_status":case["support"].get(
                         "automation_cli" if case["audience"] == "automation" else "agent_api")})
    receipt={"schema_version":body["schema_version"],"journeys":len(cases),
             "audiences":dict(by_audience),"physical_hil_verified":0,
             "release_authorized":False,
             "quality_errors":errors,
             "support_totals":{k:dict(v) for k,v in by_state.items()},
             "open_gates":gaps}
    (REPORT/"machine-scorecard.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    lines=["# R14 UI/UX/DX journey evidence — source-scoped scorecard","",
           "**This is an evidence catalog, not user testing or product release acceptance.**",
           f"Total journeys: {len(cases)}; Human {by_audience['human']}, Automation {by_audience['automation']}, Agent {by_audience['agent']}.",
           "No real headphone, legacy gamepad, VoiceOver/TalkBack, iOS/iPadOS or signed deployment qualification exists.","",
           "## Evidence by platform","",
           "| Platform | Source compiled | Browser tested | Emulated browser | CI test harness | Input only | Unknown/not demonstrated |",
           "|---|---:|---:|---:|---:|---:|---:|"]
    for p in MATRIX_ORDER:
        c=by_state[p]
        lines.append("| "+p+" | "+str(c["source_compiled"])+" | "+
                     str(c["browser_tested"])+" | "+str(c["emulated_browser"])+" | "+
                     str(c["ci_qualified"]+c["test_harness_only"])+" | "+
                     str(c["input_only"])+" | "+str(c["not_demonstrated"]+c["planned"])+" |")
    lines += ["","## All journeys and evidence limitations","","| ID | Audience | Goal | Strongest implementation evidence | Unresolved barriers |","|---|---|---|---|---|"]
    rank={"not_demonstrated":0,"planned":1,"input_only":2,"test_harness_only":3,
          "source_compiled":4,"emulated_browser":5,"browser_tested":6,
          "ci_qualified":6,"real_device_qualified":7}
    for case in cases:
        top=max(case["support"].values(),key=rank.get)
        lines.append("| "+case["id"]+" | "+case["audience"]+" | "+case["goal"]+" | "+
                     top+"; "+",".join(case["evidence"])+" | "+
                     "; ".join(case["releaseBlockers"]).replace("|","/")+" |")
    lines += ["","## Admission","",
              f"Manifest lint: {'FAIL' if errors else 'PASS'} ({len(errors)} errors).",
              "**Product UX/agent/physical-audio release: HOLD.**",
              "Green matrix lint only establishes that gaps are explicitly recorded.",
              "Real HIL, localization/usability, signed platform builds and host STOP are not inferred.",
              ""]
    (REPORT/"human-scorecard.md").write_text("\n".join(lines),encoding="utf-8")
    if errors:
        for e in errors:
            print("FAIL:",e,file=sys.stderr)
        return 1
    print(f"PASS R14 evidence manifest: {len(cases)} structured journeys / 3 audiences / 12 surfaces")
    print("HOLD release: 0 real-device-verified journeys; agent control API not admitted")
    return 0
if __name__ == "__main__":
    sys.exit(main())

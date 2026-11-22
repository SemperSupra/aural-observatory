#!/usr/bin/env python3
"""Inventory actual Aural UI, native Android and automation/agent control surfaces.

Source strings are only static evidence. This tool must NEVER claim verified
permissions, audio effects, published agent endpoints or a real-device HIL.
"""
from pathlib import Path
import json, re, sys
from collections import Counter

root=Path(__file__).resolve().parents[3]
desktop=root/"src/AuralObservatory.SearchDesktop/Program.cs"
android=root/"src/AuralObservatory.Android/MainActivity.cs"
touch=root/"src/AuralObservatory.Android/NativeSpatialTouchSurface.cs"
core=root/"src/AuralObservatory.Core/SpatialTouchIntentProjector.cs"
dle=root/"docs/DLE.md"
files=[desktop,android,touch,core,dle]
errors=[]
for x in files:
    if not x.exists():
        errors.append(f"missing owning source: {x.relative_to(root)}")
def read(f):return f.read_text(encoding="utf-8") if f.exists() else ""
sdesktop, sandroid, stouch, score, sdle=map(read,files)
methods=re.findall(r'app\.Map(Get|Post|Put|Delete)\(\s*"(/api/[^"]+)"',sdesktop)
routes=[{"verb":method.upper(),"path":path} for method,path in methods]
for gate in [
    ('loopback-only host','http://127.0.0.1:0' in sdesktop),
    ('API bearer-like session token required','X-Aural-Token' in sdesktop and
     'Path.StartsWithSegments("/api")' in sdesktop),
    ('separate manual-sweep stop endpoint','/api/sweep-stop' in [x['path'] for x in routes]),
    ('manual-sweep separate from trial journal','Manual sweeps are deliberately separate' in sdesktop),
    ('explicit human session UI','Aural Observatory local session:' in sdesktop),
    ('native Android View MotionEvent','OnTouchEvent(MotionEvent?' in stouch),
    ('native Android Activity lifecycle STOP','protected override void OnPause' in sandroid and
     'StopPreview();' in sandroid),
    ('native touch never directly drives AudioTrack','AudioTrack' not in stouch),
    ('native view not a WebView','WebView' not in stouch and 'WebView' not in sandroid),
    ('native touch projection owned by core','class SpatialTouchIntentProjector' in score),
    ('no agent API route falsely declared',not any(
        r['path'].startswith('/api/agent') for r in routes)),
]:
    if not gate[1]:
        errors.append("SOURCE GATE FAILED: "+gate[0])
actions=[
 ("profile.validate","core/library","TEST_HARNESS_ONLY","NO_STABLE_EXTERNAL_CLI"),
 ("stimulus.generate-block","core/library","TEST_HARNESS_ONLY","NO_STABLE_EXTERNAL_CLI"),
 ("audio.start","desktop local UI and native Android","HUMAN_GESTURE_REQUIRED","NO_SIGNED_HOST_AGENT_PERMISSION"),
 ("audio.stop","desktop local UI and native Android","REQUEST_NOT_DEVICE_OBSERVATION","UNVERIFIED_PHYSICAL_STOP"),
 ("trial.begin","desktop token route and Android native UI","HUMAN_LOCAL","NO_AGENT_AUTHORITY"),
 ("observation.record","desktop local route and native owner","LOCAL_ONLY","PRIVACY_AND_CONSENT"),
 ("observation.export","desktop token route and local data","LOCAL_ONLY","SCOPED_AGENT_EXPORT_ABSENT"),
 ("sensor.connect","native BLE consent workcell","SOURCE_ONLY","REAL_SENSOR_HIL"),
 ("spatial.touch","Android-native input-only / browser overlay","INPUT_ONLY","NO_NATIVE_HRTF_EFFECT"),
 ("capabilities.read","none in Aural runtime","NOT_IMPLEMENTED","TYPE_AND_VERSION_REQUIRED"),
 ("session.observe","none versioned agent API","NOT_IMPLEMENTED","OS_PRINCIPAL_AND_LEASE_REQUIRED"),
 ("audio.propose","none versioned agent API","NOT_IMPLEMENTED","HUMAN_APPROVAL_REQUIRED"),
 ("human.preempt","policy, no shared agent session runtime","NOT_IMPLEMENTED","CAS_EPOCH_AND_HOST_STOP"),
 ("command.replay","candidate Audio Foundry only","NOT_IMPLEMENTED","DURABLE_IDEMPOTENT_EFFECT_RECONCILIATION"),
]
control=[{"operation":op,"current_surface":surface,"status":status,"blocker":blocker}
         for op,surface,status,blocker in actions]
agent_gap=[a["operation"] for a in control if a["status"]=="NOT_IMPLEMENTED"]
if not agent_gap: errors.append("false claim: agent API fully qualified")
out={"schema_version":"aural.r14.source-parity.v1",
     "source_refs":{str(x.relative_to(root)):True for x in files},
     "desktop_api_routes":routes,
     "counts":{"local_token_routes":len(routes),"agent_apis_qualified":0,
               "native_spatial_audio_qualified":0,
               "hardware_stop_qualified":0},
     "audiences":{
       "human":{"desktop":"local_web_with_token","android":"native_views_and_audio_track",
                "ios":"not_implemented","separate_phone_browser":"loopback_not_reachable"},
       "automation":{"core":"reproducible_test_harness","stable_cli":"not_demonstrated"},
       "agent":{"dedicated_api":"not_implemented",
                "user_preemption":"not_implemented_in_aural_multi_principal_runtime",
                "human_approval":"design_gate_not_control_plane"}
      },
     "capability_operations":control,"agent_gaps":agent_gap,"release":"HOLD","errors":errors}
folder=root/"artifacts/evaluation/r14/native-dx";folder.mkdir(parents=True,exist_ok=True)
(folder/"source-parity.json").write_text(json.dumps(out,indent=2)+"\n",encoding="utf-8")
markdown=["# R14 native and agent DX evidence from actual source","",
 "**Source audit, not a running agent API or complete Android UI test.**","",
 f"Found {len(routes)} token-gated local desktop API routes; dedicated agent control APIs qualified: 0.","",
 "| Operation | Current owner/surface | Status | Remaining gate |",
 "|---|---|---|---|"]
for x in control:
    markdown.append("| "+x["operation"]+" | "+x["current_surface"]+" | "+
                    x["status"]+" | "+x["blocker"]+" |")
markdown+=["","## Release vetoes","",
 "- User gesture/OS human presence cannot be synthesized from a session token or JSON.",
 "- Audio STOP request is not independently measured physical headphone silence.",
 "- Android touch display currently exposes no audible spatial HRTF effect.",
 "- Browsers on a different mobile device cannot connect to the 127.0.0.1 desktop host.",
 "- No versioned per-principal agent session/CAS/idempotency authority exists in Aural runtime.",
 "- Real TalkBack/VoiceOver, Android/iPad HIL and Bluetooth/gamepad UX remain unqualified.",
 "","Audit verdict: "+("FAIL source drift" if errors else "PASS source inventory, HOLD product admission"),
 ""]
(folder/"source-parity.md").write_text("\n".join(markdown),encoding="utf-8")
if errors:
    print("\n".join(errors),file=sys.stderr);sys.exit(1)
print(f"PASS R14 native DX source inventory: {len(routes)} real desktop routes, "
      f"{len(control)} operation rows, {len(agent_gap)} missing agent controls")
print("HOLD production: 0 qualified agent APIs, 0 verified physical STOP or Android spatial output")

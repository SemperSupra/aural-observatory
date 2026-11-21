using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using AuralObservatory.Core;
var active=new ExperimentArm("A","notch-legacy-v1",new string('a',64),.06);
var control=new ExperimentArm("B","control-no-notch-legacy-v1",new string('b',64),.06);
var protocol=new ExperimentProtocol(1,"synthetic-n1-r8","poststimulus_loudness_0_10",
 "active_minus_control",41,24,15,30,120,active,control);
var loaded=JsonSerializer.Deserialize<ExperimentProtocol>(
    System.IO.File.ReadAllText("examples/synthetic-r8-n1-protocol.json"),
    new JsonSerializerOptions{PropertyNameCaseInsensitive=true})
    ?? throw new Exception("Missing protocol fixture");
ExperimentPlanner.Validate(loaded);
Check(loaded.Pairs==protocol.Pairs && loaded.Active.StimulusSha256.Length==64,
 "versioned protocol fixture roundtrip");
var first=ExperimentPlanner.Build(protocol);
var again=ExperimentPlanner.Build(protocol);
Check(first.SequenceEqual(again),"preregistered randomization deterministic");
Check(first.Count==48,"24 paired blocks");
Check(first.GroupBy(x=>x.Pair).All(g=>g.Count()==2 && g.Select(x=>x.Condition).Distinct().Count()==2),
 "each block has exactly one active and one control");
Check(first.Select(x=>x.Sequence).SequenceEqual(Enumerable.Range(1,48)),"monotone trial sequences");
Check(!first.All(x=>x.Condition==ExperimentCondition.Active),"nondegenerate allocation");
try{ExperimentPlanner.Validate(protocol with { AutomaticallyAdjustGain=true});throw new Exception("FAIL auto gain accepted");}
catch(ArgumentException){Console.WriteLine("PASS: automatic gain prohibited");}
try{ExperimentPlanner.Validate(protocol with { Active=active with{DigitalCeiling=.5} });throw new Exception("FAIL high gain accepted");}
catch(ArgumentOutOfRangeException){Console.WriteLine("PASS: digital ceiling bounded");}
Console.WriteLine(JsonSerializer.Serialize(new {schemaVersion=1,type="synthetic-plan",protocol.ExperimentId,
    planned=first.Count,active=first.Count(x=>x.Condition==ExperimentCondition.Active),
    control=first.Count(x=>x.Condition==ExperimentCondition.Control)}));
static void Check(bool b,string msg){if(!b)throw new Exception("FAIL "+msg);Console.WriteLine("PASS "+msg);}

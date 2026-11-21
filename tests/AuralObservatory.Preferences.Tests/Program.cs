using AuralObservatory.Core;
using System;
using System.IO;
using System.Linq;

var initial=UserPreferences.Default;
Check(initial.Appearance==AppearanceMode.System,"system theme default");
Check(initial.FontScalePercent==100,"default text scale");
var edited=initial with { Appearance=AppearanceMode.Dark, FontScalePercent=180, HighContrast=true, FontFamily=FontFamilyChoice.Serif,
    ResponseMode=QuietInputMode.ReleaseWhenQuiet };
edited=UserPreferencesStore.SavePreset(edited,"First safe settings",DateTimeOffset.Parse("2026-11-21T00:00:00Z"));
edited=UserPreferencesStore.SavePreset(edited,"First safe settings",DateTimeOffset.Parse("2026-11-22T00:00:00Z"));
Check(edited.Presets.Count==1,"replacing same-name preset is idempotent");
var roundtrip=UserPreferencesStore.FromJson(UserPreferencesStore.ToJson(edited));
Check(roundtrip.Appearance==AppearanceMode.Dark && roundtrip.HighContrast && roundtrip.FontScalePercent==180,"appearance roundtrip");
Check(roundtrip.Presets.Single().Stimulus.Left.FrequencyHz==4000,"full ear config roundtrip");
var temp=Path.Combine(Path.GetTempPath(),"aural-test-"+Guid.NewGuid().ToString("N"),"preferences-v1.json");
try {
    Check(UserPreferencesStore.ReadFile(temp).Appearance==AppearanceMode.System,"fresh install defaults");
    UserPreferencesStore.WriteFile(temp,edited);
    Check(UserPreferencesStore.ReadFile(temp).Presets.Count==1,"preferences retained outside installed binary");
    var unsupported=edited with { SchemaVersion=999 };
    try {UserPreferencesStore.ToJson(unsupported);throw new Exception("accepted future schema");}
    catch(NotSupportedException) {Console.WriteLine("PASS: fail closed on future schema");}
    var corrupt="{not-json";
    File.WriteAllText(temp,corrupt);
    try {UserPreferencesStore.ReadFile(temp);throw new Exception("accepted corrupt data");}
    catch(System.Text.Json.JsonException){Check(File.ReadAllText(temp)==corrupt,"corrupt preferences preserved, not silently erased");}
    try{UserPreferencesStore.ToJson(edited with { FontScalePercent=300 }); throw new Exception("accepted bad font scale");}
    catch(ArgumentOutOfRangeException){Console.WriteLine("PASS: bounded font scale");}
}
finally {var dir=Path.GetDirectoryName(temp); if(dir is not null && Directory.Exists(dir)) Directory.Delete(dir,true);}
Console.WriteLine("PASS: all preferences schema/roundtrip/upgrade tests");
static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL "+message);Console.WriteLine("PASS "+message);}

using AuralObservatory.Core;
using System;
using System.Linq;

var a = new EarProfile(4000, 0.8, 1.0, NoiseColour.Pink);
var b = new EarProfile(6000, 0.6, 0.5, NoiseColour.Brown);
var config = new StimulusSettings(a, b, 4, 0.5, 12345);
var first = new StimulusEngine(config);
var second = new StimulusEngine(config);
short[] x = new short[4096], y = new short[4096];
first.Render(x, 2048); second.Render(y, 2048);
Check(x.SequenceEqual(y), "seed reproducibility");
Check(x.Max(z => Math.Abs((int)z)) <= 2622, "digital ceiling");
Check(x.Any(z => z != 0), "nonzero output");
var silentRight = new StimulusEngine(config with { Right = b with { Gain = 0 } });
var zbuf = new short[4096]; silentRight.Render(zbuf, 2048);
Check(Enumerable.Range(0, 2048).All(i => zbuf[2*i + 1] == 0), "ear isolation");
Check(Enumerable.Range(0, 2048).Any(i => zbuf[2*i] != 0), "left ear output");
foreach (var c in Enum.GetValues<NoiseColour>())
{
  var e = new StimulusEngine(config with { Left = a with { Colour = c }, Right = b with { Colour = c } });
  var buf = new short[4096]; e.Render(buf, 2048);
  Check(buf.Any(v => v != 0), $"color {c} output");
}
ExpectInvalid(config with { Left = a with { FrequencyHz = 24000 } });
ExpectInvalid(config with { ModulationDepth = 1.2 });

// R10: fixed-output-level exploratory sweeps, not an auto-start or clinical treatment pathway.
var rising = new AcousticSweep(AcousticSweepMode.ReferenceTone, 500, 8000, 8, SearchEar.Left);
var falling = new AcousticSweep(AcousticSweepMode.NotchCenter, 8000, 500, 15, SearchEar.Right);
Check(Math.Abs(rising.FrequencyAtFrame(0) - 500) < 0.0001, "rising first sample");
Check(Math.Abs(rising.FrequencyAtFrame(rising.TotalFrames - 1) - 8000) < 0.0001, "rising final sample");
Check(rising.FrequencyAtFrame(10000) < rising.FrequencyAtFrame(20000), "ascending logarithmic monotonic");
Check(falling.FrequencyAtFrame(10000) > falling.FrequencyAtFrame(20000), "descending logarithmic monotonic");
Check(!rising.Includes(SearchEar.Right) && rising.Includes(SearchEar.Left), "ear-scoped sweep");
try { rising.FrequencyAtFrame(rising.TotalFrames); throw new Exception("Outside sweep window accepted"); }
catch (ArgumentOutOfRangeException) { Console.WriteLine("PASS bounded frame indices"); }
foreach (var invalid in new[] {
    new AcousticSweep(AcousticSweepMode.ReferenceTone, 100, 2000),
    new AcousticSweep(AcousticSweepMode.ReferenceTone, 2000, 2000),
    new AcousticSweep(AcousticSweepMode.ReferenceTone, 2000, 8000, 120),
    new AcousticSweep(AcousticSweepMode.NotchCenter, double.NaN, 8000)
}) {
    try { new StimulusEngine(config with { Sweep = invalid }); throw new Exception("Invalid sweep accepted"); }
    catch (ArgumentOutOfRangeException) { Console.WriteLine("PASS fail-closed sweep configuration"); }
}
var referenceToneConfig = new StimulusSettings(
    new EarProfile(4000, 1, 0.5, NoiseColour.White, false),
    new EarProfile(4000, 1, 0.5, NoiseColour.White, false), Seed: 42u, Sweep: rising);
var toneEngine = new StimulusEngine(referenceToneConfig);
var toneBlock = new short[2048];
toneEngine.Render(toneBlock, 1024);
Check(Enumerable.Range(0, 1024).All(i => toneBlock[2*i + 1] == 0),
    "unselected ear emits zero in tone sweep");
Check(Enumerable.Range(0, 1024).Any(i => toneBlock[2*i] != 0),
    "selected ear emits continuous tone");
Check(toneBlock.Max(p => Math.Abs((int)p)) <= 2622, "tone remains under digital ceiling");
for (int k = 1; k < rising.TotalFrames / 1024; k++) toneEngine.Render(toneBlock, 1024);
toneEngine.Render(toneBlock, 1024);
Check(toneBlock.All(p => p == 0), "tone sweep ends with silence even if renderer called again");
var notchStart = new AcousticSweep(AcousticSweepMode.NotchCenter, 1000, 7000, 8, SearchEar.Left);
var white = new EarProfile(1000, 0.8, 0.6, NoiseColour.White, true);
var notchConfig = new StimulusSettings(white, white, Seed: 123u, Sweep: notchStart);
var dynamicNotch = new StimulusEngine(notchConfig);
var staticNotch = new StimulusEngine(notchConfig with { Sweep = null });
var varying = new short[8192]; var stationary = new short[8192];
for (int i = 0; i < 48; i++) {
    dynamicNotch.Render(varying, 4096);
    staticNotch.Render(stationary, 4096);
}
Check(Enumerable.Range(0, 4096).Any(i => varying[2*i] != stationary[2*i]),
    "moving notch changes actual shared-engine PCM");
Check(Enumerable.Range(0, 4096).All(i => varying[2*i + 1] == 0),
    "notch sweep isolates unselected right ear");
try {
    new StimulusEngine(notchConfig with { Left = white with { NotchEnabled = false } });
    throw new Exception("Disabled moving notch accepted");
} catch (ArgumentException) { Console.WriteLine("PASS moving notch requires enabled filter"); }

Console.WriteLine("All AuralObservatory Core checks passed.");
void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); }
void ExpectInvalid(StimulusSettings s) { try { new StimulusEngine(s); throw new Exception("Invalid config accepted"); } catch (ArgumentOutOfRangeException) { Console.WriteLine("PASS validation rejects invalid config"); } }

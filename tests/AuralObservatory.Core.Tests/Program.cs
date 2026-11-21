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
Console.WriteLine("All AuralObservatory Core checks passed.");
void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); }
void ExpectInvalid(StimulusSettings s) { try { new StimulusEngine(s); throw new Exception("Invalid config accepted"); } catch (ArgumentOutOfRangeException) { Console.WriteLine("PASS validation rejects invalid config"); } }

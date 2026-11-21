using AuralObservatory.Core;
using System.Linq;

static void Check(bool cond, string name)
{
    if (!cond) throw new Exception($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}");
}
var cfg = new StimulusSettings(new EarProfile(4000, 0.4), new EarProfile(5500, 0.6), 0, 0, 567);
var s = new QuietSearch(cfg, SearchEar.Left, 1234);
var t = new QuietSearch(cfg, SearchEar.Left, 1234);
Check(s.PlannedCandidates.Select(x => x.FrequencyHz).SequenceEqual(t.PlannedCandidates.Select(x => x.FrequencyHz)), "deterministic seeded plan");
Check(s.PlannedCandidates.Count == 10, "9 coarse plus repeated control");
Check(s.PlannedCandidates.All(x => x.Settings.Right == cfg.Right && x.Settings.Left.Gain == cfg.Left.Gain), "right ear and gain invariant");
var trial = s.Begin(1);
s.MarkQuiet(true, 2); s.MarkQuiet(false, 4); s.SoundStopped(16);
s.MarkQuiet(true, 16.5); s.MarkQuiet(false, 18.5);
s.Annotate("quieter after stop", 3);
var r = s.Finish(19);
Check(r.DuringQuietSeconds == 2 && r.AfterQuietSeconds == 2, "separate masking/residual intervals");
Check(r.Note == "quieter after stop" && r.LoudnessAfter == 3, "subjective annotation");
Check(s.State == SearchState.TrialComplete, "state finished trial");
var second = s.Begin(20);
Check(second.FrequencyHz != trial.FrequencyHz, "new trial selected");
s.MarkQuiet(true, 21); s.SoundStopped(22); s.Finish(23, interrupted: true);
Check(s.Results[1].DuringQuietSeconds == 0, "interrupted results excluded from scores");
try { s.Begin(2); throw new Exception("clock allowed to reverse"); } catch (ArgumentOutOfRangeException) { Console.WriteLine("PASS monotonic timestamp checks"); }

var holdQuiet = new QuietInputController(QuietInputMode.HoldWhenQuiet);
Check(holdQuiet.Process(true) == true, "hold mode down marks quiet");
Check(holdQuiet.Process(true) is null, "repeat down suppressed");
Check(holdQuiet.Process(false) == false, "hold mode release ends quiet");

var releaseQuiet = new QuietInputController(QuietInputMode.ReleaseWhenQuiet);
Check(!releaseQuiet.HasObservation && !releaseQuiet.IsQuiet, "inverse mode initial unarmed state is not quiet");
Check(releaseQuiet.Process(false) is null, "inverse idle release does not indicate quiet");
Check(releaseQuiet.Process(true) == false, "inverse press indicates tinnitus audible");
Check(releaseQuiet.Process(false) == true, "inverse release indicates quiet");
releaseQuiet.Cancel();
Check(!releaseQuiet.HasObservation && !releaseQuiet.IsQuiet, "inverse mode phase reset unarmed");

var toggle = new QuietInputController(QuietInputMode.ToggleQuiet);
Check(toggle.Process(true) == true && toggle.Process(false) is null, "toggle on only at down edge");
Check(toggle.Process(true) == false && toggle.Process(false) is null, "toggle off only at next down edge");
toggle.Reset(QuietInputMode.HoldWhenQuiet);
Check(toggle.Process(true) == true && toggle.Mode == QuietInputMode.HoldWhenQuiet, "reconfigurable mode");

Console.WriteLine("All search tests passed.");

using System.Diagnostics;

namespace AuralObservatory.Core;

/// <summary>Temporal category matters: masking DURING sound is not residual inhibition AFTER sound.</summary>
public enum ReliefPeriod { DuringStimulus, AfterStimulus }
public enum SearchState { Ready, Stimulating, PostStimulus, TrialComplete, Finished }
public enum SearchEar { Both, Left, Right }
public sealed record QuietInterval(ReliefPeriod Period, double StartSeconds, double EndSeconds);
public sealed record SearchCandidate(int Number, double FrequencyHz, StimulusSettings Settings, bool RepeatedControl, int Stage);
public sealed record SearchResult(SearchCandidate Candidate, IReadOnlyList<QuietInterval> Holds, double DuringQuietSeconds,
    double AfterQuietSeconds, double? LoudnessAfter, string? Note, bool Interrupted);

/// <summary>
/// Controlled exploratory frequency sweep. All gain values, notch widths, modulation, and DSP modes stay
/// at the validated baseline; never adjusts physical output level, never claims tinnitus treatment.
/// No wall clock is used for elapsed times. Caller supplies monotonic timestamps for deterministic tests.
/// </summary>
public sealed class QuietSearch
{
    private readonly StimulusSettings _baseline;
    private readonly SearchEar _ear;
    private readonly List<SearchCandidate> _candidates = new();
    private readonly List<SearchResult> _results = new();
    private readonly List<QuietInterval> _intervals = new();
    private readonly uint _seed;
    private double _startedAt, _transitionAt, _lastAt, _heldSince;
    private bool _held;
    private ReliefPeriod _heldPeriod;
    private int _index;
    private bool _refined;
    private double? _loudnessAfter;
    private string? _note;
    public SearchState State { get; private set; } = SearchState.Ready;
    public SearchCandidate? Current { get; private set; }
    public IReadOnlyList<SearchResult> Results => _results.AsReadOnly();
    public IReadOnlyList<SearchCandidate> PlannedCandidates => _candidates.AsReadOnly();

    public QuietSearch(StimulusSettings baseline, SearchEar ear = SearchEar.Both, uint seed = 0xB53A,
        double lowerHz = 500, double upperHz = 9000)
    {
        StimulusValidation.Validate(baseline);
        if (!double.IsFinite(lowerHz) || !double.IsFinite(upperHz) || lowerHz < 200 || upperHz > 10000 || upperHz <= lowerHz)
            throw new ArgumentOutOfRangeException(nameof(lowerHz));
        _baseline = baseline;
        _ear = ear;
        _seed = seed == 0 ? 1 : seed;
        // Nine logarithmically spaced frequencies and a repeated baseline anchor; deterministic seeded ordering.
        var values = Enumerable.Range(0, 9)
            .Select(i => lowerHz * Math.Pow(upperHz / lowerHz, i / 8d)).ToList();
        Shuffle(values, _seed);
        for (var i = 0; i < values.Count; i++) _candidates.Add(Candidate(values[i], false, 0));
        _candidates.Insert(5, Candidate(ear switch { SearchEar.Left => baseline.Left.FrequencyHz,
            SearchEar.Right => baseline.Right.FrequencyHz, _ => baseline.Left.FrequencyHz }, true, 0));
    }

    private SearchCandidate Candidate(double f, bool control, int stage)
    {
        var cfg = _baseline with
        {
            Left = _ear == SearchEar.Right ? _baseline.Left : _baseline.Left with { FrequencyHz = f },
            Right = _ear == SearchEar.Left ? _baseline.Right : _baseline.Right with { FrequencyHz = f }
        };
        StimulusValidation.Validate(cfg);
        return new SearchCandidate(_candidates.Count + 1, Math.Round(f, 2), cfg, control, stage);
    }

    private static void Shuffle(List<double> values, uint seed)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            int j = (int)(seed % (uint)(i + 1));
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    public SearchCandidate Begin(double seconds)
    {
        RequireTime(seconds);
        if (State is not (SearchState.Ready or SearchState.TrialComplete)) throw new InvalidOperationException("Finish current trial first.");
        if (_index == _candidates.Count && !_refined) AppendRefinement();
        if (_index >= _candidates.Count) { State = SearchState.Finished; throw new InvalidOperationException("Search finished."); }
        Current = _candidates[_index];
        _intervals.Clear(); _loudnessAfter = null; _note = null; _held = false;
        _startedAt = _transitionAt = _lastAt = seconds;
        State = SearchState.Stimulating;
        return Current;
    }

    public void MarkQuiet(bool pressed, double seconds)
    {
        RequireTime(seconds);
        if (State is not (SearchState.Stimulating or SearchState.PostStimulus)) return;
        var period = State == SearchState.Stimulating ? ReliefPeriod.DuringStimulus : ReliefPeriod.AfterStimulus;
        if (_held && (!pressed || period != _heldPeriod)) EndInterval(seconds);
        if (pressed && !_held)
        {
            _heldSince = seconds; _heldPeriod = period; _held = true;
        }
    }

    /// <summary>Called when renderer really stops (not just when its timer is scheduled to finish).</summary>
    public void SoundStopped(double seconds)
    {
        RequireTime(seconds);
        if (State != SearchState.Stimulating) return;
        EndInterval(seconds);
        _transitionAt = seconds;
        State = SearchState.PostStimulus;
    }

    public void Annotate(string? note, double? loudnessAfter = null)
    {
        if (loudnessAfter is < 0 or > 10 || (loudnessAfter is double v && !double.IsFinite(v)))
            throw new ArgumentOutOfRangeException(nameof(loudnessAfter));
        _note = note is null ? null : note.Length > 240 ? note[..240] : note;
        _loudnessAfter = loudnessAfter;
    }

    public SearchResult Finish(double seconds, bool interrupted = false)
    {
        RequireTime(seconds);
        if (State is not (SearchState.Stimulating or SearchState.PostStimulus) || Current is null)
            throw new InvalidOperationException("No active trial.");
        EndInterval(seconds);
        var duration = Math.Max(0, _transitionAt - _startedAt);
        // Discard button press durations on interrupted sessions from objective rank decisions.
        double during = interrupted ? 0 : _intervals.Where(x => x.Period == ReliefPeriod.DuringStimulus).Sum(x => x.EndSeconds - x.StartSeconds);
        double after = interrupted ? 0 : _intervals.Where(x => x.Period == ReliefPeriod.AfterStimulus).Sum(x => x.EndSeconds - x.StartSeconds);
        var result = new SearchResult(Current, _intervals.ToArray(), Math.Min(during, duration), after, _loudnessAfter, _note, interrupted);
        _results.Add(result);
        _index++;
        State = SearchState.TrialComplete;
        Current = null;
        return result;
    }

    private void AppendRefinement()
    {
        _refined = true;
        // A first-stage 'best' is exploratory, NOT clinical evidence. Only after-stimulus holds rank.
        var usable = _results.Where(x => !x.Interrupted && !x.Candidate.RepeatedControl).ToArray();
        if (usable.Length < 3) return;
        var best = usable.OrderByDescending(x => x.AfterQuietSeconds)
            .ThenBy(x => x.LoudnessAfter ?? double.PositiveInfinity).First();
        var center = best.Candidate.FrequencyHz;
        var candidates = new List<double> { center / Math.Pow(2, 0.25), center / Math.Pow(2, 0.125),
            center * Math.Pow(2, 0.125), center * Math.Pow(2, 0.25) };
        candidates = candidates.Where(f => f >= 200 && f <= 10000).ToList();
        Shuffle(candidates, _seed ^ 0xC0FFEEu);
        foreach (var f in candidates) _candidates.Add(Candidate(f, false, 1));
    }

    private void EndInterval(double seconds)
    {
        if (!_held) return;
        if (seconds > _heldSince) _intervals.Add(new QuietInterval(_heldPeriod, _heldSince, seconds));
        _held = false;
    }
    private void RequireTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < _lastAt) throw new ArgumentOutOfRangeException(nameof(seconds), "Monotonic time required.");
        _lastAt = seconds;
    }
    public static double NowSeconds() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
}

/// <summary>Voice text is a human annotation only. Never interpreted as an autonomous volume command.</summary>
public interface IVoiceAnnotationSource
{
    event EventHandler<string> TranscriptReceived;
    Task StartOnceAsync(CancellationToken ct = default);
    Task StopAsync();
}

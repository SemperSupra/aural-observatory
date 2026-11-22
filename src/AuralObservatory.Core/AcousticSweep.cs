using System;

namespace AuralObservatory.Core;

/// <summary>
/// Explicitly human-started, fixed-gain exploratory sweep metadata for the shared PCM renderer.
/// A tone match is subjective; it is neither an audiogram nor evidence of therapy.
/// </summary>
public enum AcousticSweepMode { ReferenceTone, NotchCenter }

/// <summary>
/// Bounded logarithmic frequency trajectory. The existing renderer and UI must still enforce
/// explicit user start, STOP, output routing and lifecycle cancellation. No autonomous playback.
/// </summary>
public sealed record AcousticSweep(
    AcousticSweepMode Mode,
    double StartHz,
    double EndHz,
    int DurationSeconds = 15,
    SearchEar Ear = SearchEar.Both)
{
    public int TotalFrames => checked(DurationSeconds * StimulusValidation.SampleRate);

    public void Validate()
    {
        if (!Enum.IsDefined(Mode) || !Enum.IsDefined(Ear))
            throw new ArgumentOutOfRangeException(nameof(Mode), "Unknown sweep mode/ear.");
        if (!double.IsFinite(StartHz) || !double.IsFinite(EndHz) ||
            StartHz < 200 || EndHz < 200 || StartHz > 10000 || EndHz > 10000 ||
            StartHz == EndHz)
            throw new ArgumentOutOfRangeException(nameof(StartHz), "Sweep must traverse distinct bounded frequencies.");
        if (DurationSeconds < 8 || DurationSeconds > 15)
            throw new ArgumentOutOfRangeException(nameof(DurationSeconds), "Sweep previews must fit the existing 15-second safety envelope.");
    }

    /// <summary>Inclusive endpoints at the first and last playable frames; no audio side effects.</summary>
    public double FrequencyAtFrame(long frame)
    {
        Validate();
        if (frame < 0 || frame >= TotalFrames)
            throw new ArgumentOutOfRangeException(nameof(frame));
        var progress = frame / (double)(TotalFrames - 1);
        return StartHz * Math.Exp(Math.Log(EndHz / StartHz) * progress);
    }

    public bool Includes(SearchEar channel) =>
        Ear == SearchEar.Both || Ear == channel;
}

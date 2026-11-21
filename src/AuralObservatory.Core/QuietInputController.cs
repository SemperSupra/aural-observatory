namespace AuralObservatory.Core;

/// <summary>
/// User response convention; this controls only interpretation of input, never gain or audio DSP.
/// "ReleaseWhenQuiet" is deliberately unarmed until a physical down edge establishes "audible".
/// </summary>
public enum QuietInputMode { HoldWhenQuiet = 0, ReleaseWhenQuiet = 1, ToggleQuiet = 2 }

/// <summary>Pure input-edge interpreter; a null result means no new reliable quiet-state observation.</summary>
public sealed class QuietInputController
{
    public QuietInputMode Mode { get; private set; }
    public bool IsPressed { get; private set; }
    public bool IsQuiet { get; private set; }
    public bool HasObservation { get; private set; }

    public QuietInputController(QuietInputMode mode = QuietInputMode.HoldWhenQuiet) => Reset(mode);

    public void Reset(QuietInputMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        Mode = mode; IsPressed = false; IsQuiet = false;
        HasObservation = mode != QuietInputMode.ReleaseWhenQuiet;
    }

    /// <summary>
    /// A physical down/up event. Repeated events are ignored; return null for unknown state.
    /// In toggle mode, only a down edge toggles, while an up edge is ignored.
    /// </summary>
    public bool? Process(bool pressed)
    {
        if (pressed == IsPressed) return null;
        IsPressed = pressed;
        switch (Mode)
        {
            case QuietInputMode.HoldWhenQuiet:
                IsQuiet = pressed; HasObservation = true; break;
            case QuietInputMode.ReleaseWhenQuiet:
                // Initial no-press isn't interpreted as "quiet".
                IsQuiet = !pressed; HasObservation = true; break;
            case QuietInputMode.ToggleQuiet:
                if (pressed) { IsQuiet = !IsQuiet; HasObservation = true; }
                else return null;
                break;
        }
        return HasObservation ? IsQuiet : null;
    }

    /// <summary>Abandon an input phase without emitting a false quiet interval.</summary>
    public void Cancel() => Reset(Mode);
}

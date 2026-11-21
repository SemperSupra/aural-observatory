using System;

namespace AuralObservatory.Core;

/// <summary>Experimental audio, not clinically calibrated or a medical treatment.</summary>
public enum NoiseColour { White, Pink, Brown, Blue, Violet }

public sealed record EarProfile(
    double FrequencyHz = 4000,
    double Gain = 1.0,
    double NotchWidthOctaves = 1.0,
    NoiseColour Colour = NoiseColour.Pink,
    bool NotchEnabled = true);

public sealed record StimulusSettings(
    EarProfile Left,
    EarProfile Right,
    double ModulationHz = 0,
    double ModulationDepth = 0,
    uint Seed = 0xA17D10u);

public static class StimulusValidation
{
    public const int SampleRate = 48000;
    public const double DigitalPeakCeiling = 0.08; // NOT headphone SPL; hardware volume remains uncontrolled.

    public static void Validate(StimulusSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateEar(settings.Left);
        ValidateEar(settings.Right);
        if (!double.IsFinite(settings.ModulationHz) || settings.ModulationHz < 0 || settings.ModulationHz > 20)
            throw new ArgumentOutOfRangeException(nameof(settings.ModulationHz));
        if (!double.IsFinite(settings.ModulationDepth) || settings.ModulationDepth < 0 || settings.ModulationDepth > 0.9)
            throw new ArgumentOutOfRangeException(nameof(settings.ModulationDepth));
    }

    private static void ValidateEar(EarProfile p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (!double.IsFinite(p.FrequencyHz) || p.FrequencyHz < 200 || p.FrequencyHz > 10000)
            throw new ArgumentOutOfRangeException(nameof(p.FrequencyHz));
        if (!double.IsFinite(p.Gain) || p.Gain < 0 || p.Gain > 1)
            throw new ArgumentOutOfRangeException(nameof(p.Gain));
        if (!double.IsFinite(p.NotchWidthOctaves) || p.NotchWidthOctaves < 0.1 || p.NotchWidthOctaves > 2)
            throw new ArgumentOutOfRangeException(nameof(p.NotchWidthOctaves));
        if (!Enum.IsDefined(p.Colour)) throw new ArgumentOutOfRangeException(nameof(p.Colour));
    }
}

/// <summary>
/// Reproducible source signal for acoustic comparison, using a seeded xorshift generator and a
/// second-order notch per ear. Noise colours are approximate, NOT standardized calibrated spectra.
/// No allocations in Render(), no network, microphones, or telemetry.
/// </summary>
public sealed class StimulusEngine
{
    private readonly EarChannel _left;
    private readonly EarChannel _right;
    private readonly StimulusSettings _settings;
    private long _sampleCounter;

    public StimulusEngine(StimulusSettings settings)
    {
        StimulusValidation.Validate(settings);
        _settings = settings;
        _left = new EarChannel(settings.Left, settings.Seed ^ 0xA5A5A5A5u);
        _right = new EarChannel(settings.Right, settings.Seed ^ 0x5A5A5A5Au);
    }

    /// <summary>Writes interleaved 16-bit PCM stereo; output is bounded to 8% digital full scale.</summary>
    public void Render(short[] output, int frames)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (frames <= 0 || frames > output.Length / 2 || frames > 8192)
            throw new ArgumentOutOfRangeException(nameof(frames));
        for (int i = 0; i < frames; i++)
        {
            // First 100ms ramp to suppress the initial transient.
            double ramp = Math.Min(1.0, _sampleCounter / (0.1 * StimulusValidation.SampleRate));
            double mod = _settings.ModulationHz <= 0 ? 1.0 :
                1.0 - _settings.ModulationDepth * 0.5 *
                (1.0 - Math.Cos(2.0 * Math.PI * _settings.ModulationHz * _sampleCounter / StimulusValidation.SampleRate));
            output[2 * i] = ConvertSample(_left.Sample() * ramp * mod);
            output[2 * i + 1] = ConvertSample(_right.Sample() * ramp * mod);
            _sampleCounter++;
        }
    }

    private static short ConvertSample(double sample)
    {
        double bounded = Math.Clamp(sample * StimulusValidation.DigitalPeakCeiling, -StimulusValidation.DigitalPeakCeiling, StimulusValidation.DigitalPeakCeiling);
        return (short)Math.Round(bounded * short.MaxValue);
    }

    private sealed class EarChannel
    {
        private readonly EarProfile _profile;
        private readonly NotchBiquad _notch;
        private uint _rng;
        private double _brown;
        private double _p0, _p1, _p2, _p3, _p4, _p5, _p6;
        private double _lastWhite, _olderWhite;

        internal EarChannel(EarProfile profile, uint seed)
        {
            _profile = profile;
            _rng = seed == 0 ? 1 : seed;
            _notch = new NotchBiquad(profile.FrequencyHz, profile.NotchWidthOctaves);
        }

        internal double Sample()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            double white = (_rng / (double)uint.MaxValue) * 2.0 - 1.0;
            double selected;
            switch (_profile.Colour)
            {
                case NoiseColour.Pink:
                    _p0 = 0.99886 * _p0 + white * 0.0555179;
                    _p1 = 0.99332 * _p1 + white * 0.0750759;
                    _p2 = 0.96900 * _p2 + white * 0.1538520;
                    _p3 = 0.86650 * _p3 + white * 0.3104856;
                    _p4 = 0.55000 * _p4 + white * 0.5329522;
                    _p5 = -0.7616 * _p5 - white * 0.0168980;
                    selected = (_p0 + _p1 + _p2 + _p3 + _p4 + _p5 + _p6 + white * 0.5362) * 0.11;
                    _p6 = white * 0.115926;
                    break;
                case NoiseColour.Brown:
                    _brown = (0.997 * _brown) + (0.04 * white);
                    selected = _brown;
                    break;
                case NoiseColour.Blue:
                    selected = (white - _lastWhite) * 0.5;
                    break;
                case NoiseColour.Violet:
                    selected = (white - 2.0 * _lastWhite + _olderWhite) * 0.25;
                    break;
                default:
                    selected = white * 0.65;
                    break;
            }
            _olderWhite = _lastWhite;
            _lastWhite = white;
            double filtered = _profile.NotchEnabled ? _notch.Process(selected) : selected;
            return filtered * _profile.Gain;
        }
    }

    private sealed class NotchBiquad
    {
        private readonly double _b0, _b1, _b2, _a1, _a2;
        private double _z1, _z2;

        internal NotchBiquad(double centerHz, double widthOctaves)
        {
            double w0 = 2.0 * Math.PI * centerHz / StimulusValidation.SampleRate;
            double q = Math.Sqrt(Math.Pow(2.0, widthOctaves)) / (Math.Pow(2.0, widthOctaves) - 1.0);
            double alpha = Math.Sin(w0) / (2.0 * q);
            double a0 = 1 + alpha;
            _b0 = 1 / a0;
            _b1 = -2 * Math.Cos(w0) / a0;
            _b2 = 1 / a0;
            _a1 = _b1;
            _a2 = (1 - alpha) / a0;
        }

        internal double Process(double x)
        {
            double y = _b0 * x + _z1;
            _z1 = _b1 * x - _a1 * y + _z2;
            _z2 = _b2 * x - _a2 * y;
            return y;
        }
    }
}

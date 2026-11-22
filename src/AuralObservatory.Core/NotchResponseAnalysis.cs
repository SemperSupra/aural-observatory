using System;

namespace AuralObservatory.Core;

/// <summary>
/// Digital, not acoustic, notch measurement for the same RBJ biquad coefficients used by
/// StimulusEngine. It is an independent numerical oracle, not a second audio renderer.
/// Nominal octave width is not a guaranteed -3 dB digital bandwidth near Nyquist.
/// </summary>
public sealed record DigitalNotchMeasurement(
    double CenterHz,
    double RequestedWidthOctaves,
    double MeasuredWidthOctaves,
    double Lower3dBHz,
    double Upper3dBHz,
    double Q,
    int SampleRateHz);

public static class NotchResponseAnalysis
{
    public static DigitalNotchMeasurement Measure(double centerHz, double widthOctaves,
        int sampleRateHz = StimulusValidation.SampleRate)
    {
        if (sampleRateHz < 8000 || sampleRateHz > 384000)
            throw new ArgumentOutOfRangeException(nameof(sampleRateHz));
        if (!double.IsFinite(centerHz) || centerHz <= 0 || centerHz >= sampleRateHz / 2d)
            throw new ArgumentOutOfRangeException(nameof(centerHz));
        if (!double.IsFinite(widthOctaves) || widthOctaves < 0.1 || widthOctaves > 2)
            throw new ArgumentOutOfRangeException(nameof(widthOctaves));

        double r = Math.Pow(2.0, widthOctaves);
        double q = Math.Sqrt(r) / (r - 1.0);
        double omega0 = 2.0 * Math.PI * centerHz / sampleRateHz;
        double alpha = Math.Sin(omega0) / (2.0 * q);
        double b1 = -2.0 * Math.Cos(omega0);
        double a0 = 1.0 + alpha, a1 = b1, a2 = 1.0 - alpha;

        double Magnitude(double hz)
        {
            double w = 2.0 * Math.PI * hz / sampleRateHz;
            double cos = Math.Cos(w), sin = Math.Sin(w);
            double cos2 = Math.Cos(2.0 * w), sin2 = Math.Sin(2.0 * w);
            double numeratorReal = 1.0 + b1 * cos + cos2;
            double numeratorImag = -b1 * sin - sin2;
            double denominatorReal = a0 + a1 * cos + a2 * cos2;
            double denominatorImag = -a1 * sin - a2 * sin2;
            return Math.Sqrt((numeratorReal * numeratorReal + numeratorImag * numeratorImag) /
                (denominatorReal * denominatorReal + denominatorImag * denominatorImag));
        }

        const double target = 0.7071067811865475244;
        double low = 0, high = centerHz;
        for (int i = 0; i < 64; i++)
        {
            double mid = (low + high) / 2;
            if (Magnitude(mid) > target) low = mid;
            else high = mid;
        }
        double lower = (low + high) / 2;
        low = centerHz; high = sampleRateHz / 2d;
        for (int i = 0; i < 64; i++)
        {
            double mid = (low + high) / 2;
            if (Magnitude(mid) < target) low = mid;
            else high = mid;
        }
        double upper = (low + high) / 2;
        return new DigitalNotchMeasurement(centerHz, widthOctaves,
            Math.Log2(upper / lower), lower, upper, q, sampleRateHz);
    }
}

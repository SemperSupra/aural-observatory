using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using AuralObservatory.Core;

// Numerical qualification of the real shared StimulusEngine (not a duplicate or a claimed headphone SPL measurement).
// Output is synthetic generated PCM. Physical transducer response and calibration are separate gates.
const int sr = StimulusValidation.SampleRate, n = 8192, frames = 5 * sr;
var probes = new[] { 320, 500, 800, 1250, 2000, 3150, 5000, 8000 };
var results = new List<object>();
foreach (NoiseColour colour in Enum.GetValues<NoiseColour>())
{
    var profile = new EarProfile(4000, 1, 1, colour, false);
    var without = Render(new StimulusSettings(profile, profile, Seed: 97531u));
    var withNotch = Render(new StimulusSettings(profile with { NotchEnabled = true },
        profile with { NotchEnabled = true }, Seed: 97531u));

    var values = probes.Select(f => new {
        frequencyHz = f, power = Power(without.left, f)
    }).ToArray();
    double alpha = -Slope(values.Select(x => (double)x.frequencyHz).ToArray(), values.Select(x => x.power).ToArray());
    double attenuation = Db(Power(withNotch.left, 4000) / Power(without.left, 4000));
    double offband = Db(Power(withNotch.left, 800) / Power(without.left, 800));
    double rightNotchAttenuation = Db(Power(withNotch.right, 4000) / Power(without.right, 4000));
    Check(attenuation < -6, $"{colour} left notch attenuates at center: {attenuation:F2} dB");
    Check(rightNotchAttenuation < -6, $"{colour} right notch attenuates at center: {rightNotchAttenuation:F2} dB");
    Check(Math.Abs(offband) < 4, $"{colour} far-from-notch transfer: {offband:F2} dB");
    Check(without.peak <= StimulusValidation.DigitalPeakCeiling + 0.0001,
        $"{colour} digital peak bounded: {without.peak:F4} FS");
    Check(double.IsFinite(alpha), $"{colour} PSD slope finite");
    results.Add(new {
        colour = colour.ToString(), sampleRateHz = sr, fftWindow = n, hannWindow = true,
        estimatedPsdExponent = alpha, expectedExponent = colour switch {
            NoiseColour.White => 0, NoiseColour.Pink => 1, NoiseColour.Brown => 2,
            NoiseColour.Blue => -1, NoiseColour.Violet => -2, _ => 0
        }, leftCenterAttenuationDb = attenuation, rightCenterAttenuationDb = rightNotchAttenuation,
        leftOffbandAttenuationDb = offband, peakFullScale = without.peak,
        leftRmsFullScale = Math.Sqrt(without.left.Select(x => x*x).Average()),
        rightRmsFullScale = Math.Sqrt(without.right.Select(x => x*x).Average()),
        outsideNotchFreqHz = 800,
        measuredBins = values.Select(v => new { v.frequencyHz, power = v.power }).ToArray()
    });
}
var eq = new StimulusSettings(new EarProfile(4000, 0.6, 1, NoiseColour.White, true),
    new EarProfile(6000, 0.4, 1, NoiseColour.Pink, true), 4, 0.5, 17);
var whole = new short[8192];
new StimulusEngine(eq).Render(whole, 4096);
var chunked = new short[8192];
var second = new StimulusEngine(eq);
for(int i=0;i<4;i++)
{
    var block = new short[2048];
    second.Render(block,1024);
    Array.Copy(block,0,chunked,i*2048,2048);
}
Check(whole.SequenceEqual(chunked), "block-size invariance across render boundaries");
Check(whole[0] == 0 && whole[1] == 0,"startup ramp begins at zero");
var silent = eq with { Right = eq.Right with { Gain = 0 } };
var b = new short[8192];
new StimulusEngine(silent).Render(b,4096);
Check(Enumerable.Range(0,4096).All(i=>b[2*i+1]==0),"zero-gain right channel isolation");
var evidence = new {
    schemaVersion = 1, engine = "AuralObservatory.Core.StimulusEngine",
    sampleFormat = "48 kHz signed PCM16 interleaved stereo",
    evidenceType = "generated-digital-audio-only",
    notPhysicalSpl = true, sourceSeed = 97531,
    analysis = "Hann-windowed Goertzel periodogram; overlapping frames excluded from first 4096-sample warmup",
    results
};
Directory.CreateDirectory("artifacts/audio");
File.WriteAllText("artifacts/audio/synthetic-spectra.json",
    JsonSerializer.Serialize(evidence,new JsonSerializerOptions{ WriteIndented = true }));
Console.WriteLine("PASS: generated artifacts/audio/synthetic-spectra.json");
Console.WriteLine("PASS: all synthetic DSP qualification assertions (headphone SPL NOT measured).");

(double[] left, double[] right, double peak) Render(StimulusSettings s)
{
    var engine = new StimulusEngine(s);
    var l=new double[frames]; var r=new double[frames];var block=new short[2048];
    double peak=0;
    for(int pos=0;pos<frames;pos+=1024)
    {
        var count=Math.Min(1024,frames-pos);
        engine.Render(block,count);
        for(int i=0;i<count;i++) {
            l[pos+i]=block[2*i]/32768.0;
            r[pos+i]=block[2*i+1]/32768.0;
            peak=Math.Max(peak,Math.Max(Math.Abs(l[pos+i]),Math.Abs(r[pos+i])));
        }
    }
    return (l,r,peak);
}
double Power(double[] signal, double hz)
{
    int k=(int)Math.Round(hz*n/sr); double coeff=2*Math.Cos(2*Math.PI*k/n);
    double total=0;int count=0;
    for(int start=n/2;start+n<=signal.Length;start+=n/2)
    {
        double s1=0,s2=0;
        for(int i=0;i<n;i++){
            double w=0.5-0.5*Math.Cos(2*Math.PI*i/(n-1));
            double s0=signal[start+i]*w+coeff*s1-s2;s2=s1;s1=s0;
        }
        double p=s1*s1+s2*s2-coeff*s1*s2;
        total+=Math.Max(0,p);count++;
    }
    if(count<2)throw new Exception("Insufficient frequency windows");
    return total/count;
}
static double Slope(double[] hz,double[] powers)
{
    var xs=hz.Select(Math.Log).ToArray();
    var ys=powers.Select(v=>Math.Log(Math.Max(v,1e-20))).ToArray();
    var xm=xs.Average(); var ym=ys.Average();
    return xs.Zip(ys,(x,y)=>(x-xm)*(y-ym)).Sum() / xs.Select(x=>(x-xm)*(x-xm)).Sum();
}
static double Db(double ratio)=>10*Math.Log10(Math.Max(ratio,1e-20));
static void Check(bool ok,string message){if(!ok) throw new Exception("FAIL: "+message);Console.WriteLine("PASS: "+message);}

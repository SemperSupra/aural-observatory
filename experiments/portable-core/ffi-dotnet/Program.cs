using System;
using System.Runtime.InteropServices;
using AuralObservatory.Core;

[StructLayout(LayoutKind.Sequential)]
struct NativeConfig
{
    public uint Size;
    public uint Version;
    public double StartHz, EndHz;
    public uint Seconds, Ear;
}
[StructLayout(LayoutKind.Sequential)]
struct NativeNotch
{
    public uint Size;
    public double LowerHz, UpperHz, Octaves;
}
static class Native
{
    private const string Dll = "aural-r11-candidate";
    [DllImport(Dll, EntryPoint = "aural_r11_api_version", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint Version();
    [DllImport(Dll, EntryPoint = "aural_r11_tone_create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Create(ref NativeConfig config, out nint handle);
    [DllImport(Dll, EntryPoint = "aural_r11_tone_render", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Render(nint handle, [Out] short[] samples, uint frames, uint capacitySamples);
    [DllImport(Dll, EntryPoint = "aural_r11_tone_stop", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Stop(nint handle);
    [DllImport(Dll, EntryPoint = "aural_r11_tone_destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Destroy(nint handle);
    [DllImport(Dll, EntryPoint = "aural_r11_measure_notch", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Measure(double centerHz, double widthOctaves, uint sampleRateHz, ref NativeNotch result);
}

static class Program
{
    static void Check(bool ok, string reason)
    {
        if (!ok) throw new InvalidOperationException("FAIL: " + reason);
    }
    static void Main()
    {
        Check(Native.Version() == 1, "ABI version");
        var cfg = new NativeConfig {
            Size = (uint)Marshal.SizeOf<NativeConfig>(), Version = 1,
            StartHz = 500, EndHz = 8000, Seconds = 8, Ear = 1
        };
        Check(Native.Create(ref cfg, out var handle) == 0 && handle != 0, "create native opaque handle");
        try
        {
            var samples = new short[2048];
            Check(Native.Render(handle, samples, 1024, (uint)samples.Length) == 0, "native render through P/Invoke");
            Check(samples[0] == 0 && samples[1] == 0, "startup ramp");
            bool sound = false;
            for (int i = 0; i < 1024; ++i)
            {
                Check(samples[2*i+1] == 0, "left ear isolation");
                Check(Math.Abs((int)samples[2*i]) <= 2622, "digital PCM ceiling");
                if (samples[2*i] != 0) sound = true;
            }
            Check(sound, "nonzero selected ear");
            Check(Native.Stop(handle) == 0 && Native.Stop(handle) == 0, "idempotent stop");
            Check(Native.Render(handle, samples, 1024, (uint)samples.Length) == 0, "render after stop");
            Check(Array.TrueForAll(samples, s => s == 0), "hard STOP mutes all samples");
        }
        finally { Native.Destroy(handle); }

        foreach (var (center, width) in new[] { (1000d, .25d), (4000d, .5d), (8000d, 1d), (10000d, .5d) })
        {
            var got = new NativeNotch { Size = (uint)Marshal.SizeOf<NativeNotch>() };
            Check(Native.Measure(center,width,48000,ref got)==0, "native notch magnitude");
            var oracle = NotchResponseAnalysis.Measure(center,width,48000);
            Check(Math.Abs(got.LowerHz - oracle.Lower3dBHz) < .0001, "native vs C# low -3dB edge");
            Check(Math.Abs(got.UpperHz - oracle.Upper3dBHz) < .0001, "native vs C# high -3dB edge");
            Check(Math.Abs(got.Octaves - oracle.MeasuredWidthOctaves) < .000001,
                "native vs C# digital bandwidth");
        }
        Console.WriteLine("PASS: C17 ABI called from .NET 10, STOP/PCM isolation and 4 native-versus-C# notch fixtures.");
        Console.WriteLine("NOT physical headphone, latency, Android/iOS FFI, WASM or clinical evidence.");
    }
}

using AuralObservatory.Sensors;
using System.Linq;

Check(new byte[]{0x00, 72}, m => m.BeatsPerMinute == 72 && !m.HasRr, "8-bit BPM without RR");
Check(new byte[]{0x01, 0x2C, 0x01}, m => m.BeatsPerMinute == 300 && !m.HasRr, "16-bit BPM");
Check(new byte[]{0x16, 60, 0x00,0x04, 0x00,0x04}, m => m.BeatsPerMinute == 60 && m.Contact == SensorContact.Detected && m.RrTicks1024.SequenceEqual(new ushort[]{1024,1024}), "contact and two RR readings");
Check(new byte[]{0x1e, 60, 0x2c,0x01, 0x00,0x04}, m => m.Contact == SensorContact.Detected && m.ExpendedEnergyKj == 300 && m.HasRr, "energy offset before RR");
Check(new byte[]{0x04, 62}, m => m.Contact == SensorContact.NotDetected, "contact not detected");
Check(new byte[]{0x00, 60}, m => m.Contact == SensorContact.Unknown, "contact unknown");
Reject(Array.Empty<byte>(), "empty");
Reject(new byte[]{0x01, 0x20}, "truncated 16-bit BPM");
Reject(new byte[]{0x08, 66, 0x20}, "truncated energy");
Reject(new byte[]{0x10, 66, 0x02}, "odd RR bytes");
Reject(new byte[]{0x10, 66}, "empty advertised RR");
Reject(new byte[]{0x00, 66, 0xff}, "unexpected data after BPM");
Reject(new byte[]{0x10, 66, 0, 0}, "zero RR invalid");
var stable = Enumerable.Repeat((ushort)1024, 50).ToArray();
var metrics = RrMetrics.CalculateRmssd(stable);
Assert(metrics.RmssdMs == 0, "stable RR yields 0 RMSSD after sufficient valid pairs");
Assert(RrMetrics.CalculateRmssd(new ushort[]{1024,1025}).RmssdMs is null, "short RR run cannot produce an RMSSD estimate");
Console.WriteLine("All sensor protocol checks passed.");

static void Check(byte[] bytes, Func<HeartRateMeasurement,bool> predicate, string label)
{
    Assert(HeartRateMeasurementParser.TryParse(bytes, out var m) && m is not null && predicate(m), label);
}
static void Reject(byte[] bytes, string label)
{
    Assert(!HeartRateMeasurementParser.TryParse(bytes, out _), label);
}
static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL " + message);
    Console.WriteLine("PASS " + message);
}

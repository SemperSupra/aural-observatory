using System.Buffers.Binary;
using System.Diagnostics;

namespace AuralObservatory.Sensors;

/// <summary>Transport-independent observation contract. Sensor values are NOT tinnitus efficacy markers.</summary>
public enum SensorContact { Unknown, Detected, NotDetected }
public enum SensorConnectionState { Disconnected, Selecting, Connecting, Streaming, Error }

public sealed record HeartRateMeasurement(
    ushort BeatsPerMinute,
    SensorContact Contact,
    IReadOnlyList<ushort> RrTicks1024,
    ushort? ExpendedEnergyKj,
    byte Flags)
{
    public IReadOnlyList<double> RrMilliseconds => RrTicks1024.Select(t => t * (1000d / 1024d)).ToArray();
    public bool HasRr => RrTicks1024.Count > 0;
}

/// <summary>Bluetooth SIG 0x2A37 parser: 8/16-bit BPM, contact status, energy, zero or more RR ticks.</summary>
public static class HeartRateMeasurementParser
{
    public static bool TryParse(ReadOnlySpan<byte> bytes, out HeartRateMeasurement? result)
    {
        result = null;
        if (bytes.Length < 2) return false;
        byte flags = bytes[0];
        int index = 1;
        bool wideBpm = (flags & 0x01) != 0;
        if (bytes.Length - index < (wideBpm ? 2 : 1)) return false;
        ushort bpm = wideBpm ? BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(index, 2)) : bytes[index];
        index += wideBpm ? 2 : 1;
        var contact = (flags & 0x04) == 0 ? SensorContact.Unknown :
            (flags & 0x02) != 0 ? SensorContact.Detected : SensorContact.NotDetected;
        ushort? energy = null;
        if ((flags & 0x08) != 0)
        {
            if (bytes.Length - index < 2) return false;
            energy = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(index, 2));
            index += 2;
        }
        bool hasRr = (flags & 0x10) != 0;
        if ((bytes.Length - index) % 2 != 0 && hasRr) return false;
        if (!hasRr && bytes.Length != index) return false;
        if (hasRr && bytes.Length == index) return false;
        var rr = new List<ushort>();
        if (hasRr)
        {
            for (; index < bytes.Length; index += 2)
            {
                ushort ticks = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(index, 2));
                if (ticks == 0) return false;
                rr.Add(ticks);
            }
        }
        result = new HeartRateMeasurement(bpm, contact, rr, energy, flags);
        return true;
    }
}

/// <summary>Host-receipt timestamps, not exact sensor-side beat timestamps.</summary>
public sealed record HeartRateObservation(
    string Transport,
    string DeviceName,
    DateTimeOffset ReceivedAtUtc,
    long HostMonotonicTicks,
    long Sequence,
    HeartRateMeasurement Measurement,
    long DroppedNotifications)
{
    public double HostMonotonicSeconds => HostMonotonicTicks / (double)Stopwatch.Frequency;
}

public interface IPhysiologicalSensor : IAsyncDisposable
{
    string PluginId { get; }
    string? DeviceName { get; }
    SensorConnectionState State { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<HeartRateObservation> ObserveAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
}

/// <summary>Observed RR statistical measure, NOT an objective tinnitus response indicator.</summary>
public static class RrMetrics
{
    public static (double? RmssdMs, int Accepted, int Rejected) CalculateRmssd(IEnumerable<ushort> rrTicks)
    {
        double? previous = null;
        double squares = 0;
        int pairs = 0, accepted = 0, rejected = 0;
        foreach (ushort ticks in rrTicks)
        {
            double ms = ticks * 1000d / 1024;
            if (ms < 300 || ms > 2000) { rejected++; previous = null; continue; }
            accepted++;
            if (previous.HasValue)
            {
                double difference = ms - previous.Value;
                // Flag possible ectopy/motion artifacts rather than silently smoothing.
                if (Math.Abs(difference) <= 0.20 * previous.Value)
                {
                    squares += difference * difference;
                    pairs++;
                }
                else rejected++;
            }
            previous = ms;
        }
        return (pairs >= 30 ? Math.Sqrt(squares / pairs) : null, accepted, rejected);
    }
}

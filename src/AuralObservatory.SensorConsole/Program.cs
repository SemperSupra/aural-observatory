using System.Globalization;
using AuralObservatory.Sensors;
using AuralObservatory.Sensors.Ble;
using InTheHand.Bluetooth;

if (args.Length > 2 || args.Length == 1 && args[0] is not "--csv")
{
    Console.Error.WriteLine("Usage: dotnet run --project src/AuralObservatory.SensorConsole [--csv <local-file.csv>]");
    return 2;
}
if (args.Length == 1)
{
    Console.Error.WriteLine("--csv requires a destination path.");
    return 2;
}
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

Console.WriteLine("Aural Observatory | BLE Heart Rate diagnostics");
Console.WriteLine("Wear/moisten your chest strap, ensure Bluetooth is enabled, select the device when prompted.");
Console.WriteLine("Live physiological readings are not tinnitus improvement measurements. Ctrl+C to stop.");
// A console has no owner window for an OS Bluetooth picker; enumerate instead.
var sensor = new BleHeartRateSensor(SelectSensorAsync);
StreamWriter? csv = null;
try
{
    if (args.Length == 2)
    {
        var full = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        bool exists = File.Exists(full) && new FileInfo(full).Length > 0;
        csv = new StreamWriter(new FileStream(full, FileMode.Append, FileAccess.Write, FileShare.Read));
        if (!exists) await csv.WriteLineAsync("utc,monotonic_seconds,sequence,transport,bpm,contact,rr_ms,rr_present,invalid_packets,dropped_packets");
        Console.WriteLine("Local-only logging requested: " + full);
    }
    await sensor.ConnectAsync(cancellation.Token);
    Console.WriteLine("Connected: " + sensor.DeviceName);
    await foreach (var obs in sensor.ObserveAsync(cancellation.Token))
    {
        var m = obs.Measurement;
        string rr = m.HasRr ? string.Join(";", m.RrMilliseconds.Select(x => x.ToString("F2", CultureInfo.InvariantCulture))) : "";
        Console.WriteLine($"{obs.ReceivedAtUtc:HH:mm:ss.fff} HR={m.BeatsPerMinute} BPM RR={(m.HasRr ? rr + " ms" : "NOT ADVERTISED")} contact={m.Contact} bad={sensor.InvalidPackets} dropped={sensor.DroppedPackets}");
        if (csv is not null)
        {
            await csv.WriteLineAsync(string.Join(",", obs.ReceivedAtUtc.ToString("O"),
                obs.HostMonotonicSeconds.ToString("F6", CultureInfo.InvariantCulture),
                obs.Sequence, obs.Transport, m.BeatsPerMinute, m.Contact, rr,
                m.HasRr, sensor.InvalidPackets, sensor.DroppedPackets));
            await csv.FlushAsync();
        }
    }
}
catch (OperationCanceledException) { Console.WriteLine("Stopped."); }
catch (Exception ex) { Console.Error.WriteLine("Sensor error: " + ex.Message); return 1; }
finally
{
    if (csv is not null) await csv.DisposeAsync();
    await sensor.DisposeAsync();
}
return 0;

static async Task<BluetoothDevice?> SelectSensorAsync(CancellationToken cancellationToken)
{
    var options = new RequestDeviceOptions { Timeout = TimeSpan.FromSeconds(15) };
    options.Filters.Add(new BluetoothLEScanFilter { Services = { GattServiceUuids.HeartRate } });
    Console.WriteLine("Scanning nearby BLE heart-rate devices for up to 15 seconds...");
    var devices = await Bluetooth.ScanForDevicesAsync(options, cancellationToken);
    if (devices.Count == 0)
    {
        Console.WriteLine("No devices advertising Heart Rate Service. Retrying without a service filter...");
        options.Filters.Clear();
        options.AcceptAllDevices = true;
        devices = await Bluetooth.ScanForDevicesAsync(options, cancellationToken);
    }
    if (devices.Count == 0) throw new InvalidOperationException("No BLE devices detected. Wear strap, check Bluetooth and retry.");
    var list = devices.OrderBy(d => d.Name).ToArray();
    for (int i = 0; i < list.Length; i++)
        Console.WriteLine($"  {i + 1}. {list[i].Name} {(list[i].IsPaired ? "(paired)" : "")}");
    Console.Write("Device number (blank to cancel): ");
    string? answer = Console.ReadLine();
    if (!int.TryParse(answer, out int selected) || selected < 1 || selected > list.Length)
        return null;
    return list[selected - 1];
}

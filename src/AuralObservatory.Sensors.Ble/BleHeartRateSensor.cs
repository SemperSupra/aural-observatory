using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AuralObservatory.Sensors;
using InTheHand.Bluetooth;

namespace AuralObservatory.Sensors.Ble;

/// <summary>
/// BLE SIG Heart Rate Service provider. Compatible with any standards-conforming HRM, not Garmin-specific.
/// User initiates discovery; no telemetry or remote access. No proprietary ECG channel requested.
/// </summary>
public sealed class BleHeartRateSensor : IPhysiologicalSensor
{
    public const string Id = "org.sempersupra.aural.sensors.ble.heart-rate.v1";
    public string PluginId => Id;
    public string? DeviceName { get; private set; }
    public SensorConnectionState State { get; private set; } = SensorConnectionState.Disconnected;
    public long InvalidPackets => Interlocked.Read(ref _invalid);
    public long DroppedPackets => Interlocked.Read(ref _dropped);

    private BluetoothDevice? _device;
    private GattCharacteristic? _characteristic;
    private Channel<HeartRateObservation>? _channel;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly Func<CancellationToken, Task<BluetoothDevice?>>? _deviceSelector;

    public BleHeartRateSensor(Func<CancellationToken, Task<BluetoothDevice?>>? deviceSelector = null)
    {
        _deviceSelector = deviceSelector;
    }
    private long _sequence, _invalid, _dropped;
    private bool _disposed;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (State != SensorConnectionState.Disconnected)
                throw new InvalidOperationException("Disconnect before connecting again.");
            State = SensorConnectionState.Selecting;
            var options = new RequestDeviceOptions { Timeout = TimeSpan.FromSeconds(15) };
            options.Filters.Add(new BluetoothLEScanFilter { Services = { GattServiceUuids.HeartRate } });
            // Platforms implement their own selection UI/scan; the user must authorize the connection.
            var device = _deviceSelector is null
                ? await Bluetooth.RequestDeviceAsync(options).WaitAsync(cancellationToken).ConfigureAwait(false)
                : await _deviceSelector(cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (device is null) throw new OperationCanceledException("No Bluetooth heart-rate sensor selected.");
            _device = device;
            DeviceName = string.IsNullOrWhiteSpace(device.Name) ? "BLE heart-rate sensor" : device.Name;
            State = SensorConnectionState.Connecting;
            device.GattServerDisconnected += OnDisconnected;
            await device.Gatt.ConnectAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var service = await device.Gatt.GetPrimaryServiceAsync(GattServiceUuids.HeartRate).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (service is null) throw new InvalidOperationException("GATT Heart Rate Service 0x180D absent.");
            var characteristic = await service.GetCharacteristicAsync(BluetoothUuid.FromShortId(0x2A37))
                .WaitAsync(cancellationToken).ConfigureAwait(false);
            if (characteristic is null) throw new InvalidOperationException("Heart Rate Measurement 0x2A37 absent.");
            _characteristic = characteristic;
            _sequence = 0; _invalid = 0; _dropped = 0;
            _channel = Channel.CreateBounded<HeartRateObservation>(new BoundedChannelOptions(256)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite
            });
            characteristic.CharacteristicValueChanged += OnValueChanged;
            await characteristic.StartNotificationsAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            State = SensorConnectionState.Streaming;
        }
        catch
        {
            State = SensorConnectionState.Error;
            await CleanupAsync().ConfigureAwait(false);
            State = SensorConnectionState.Disconnected;
            throw;
        }
        finally { _lifecycle.Release(); }
    }

    private void OnValueChanged(object? sender, GattCharacteristicValueChangedEventArgs e)
    {
        // Callback is owned by the native BLE stack: do not block or do file I/O here.
        var arrival = Stopwatch.GetTimestamp();
        var utc = DateTimeOffset.UtcNow;
        if (e.Error is not null || e.Value is not { Length: > 0 } bytes ||
            !HeartRateMeasurementParser.TryParse(bytes, out var reading) || reading is null)
        {
            Interlocked.Increment(ref _invalid);
            return;
        }
        var result = new HeartRateObservation("ble-gatt", DeviceName ?? "BLE sensor", utc, arrival,
            Interlocked.Increment(ref _sequence), reading, Interlocked.Read(ref _dropped));
        if (_channel?.Writer.TryWrite(result) != true) Interlocked.Increment(ref _dropped);
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        State = SensorConnectionState.Disconnected;
        _channel?.Writer.TryComplete();
    }

    public async IAsyncEnumerable<HeartRateObservation> ObserveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ch = _channel ?? throw new InvalidOperationException("Call ConnectAsync first.");
        await foreach (var reading in ch.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            yield return reading;
    }

    public async Task DisconnectAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try { await CleanupAsync().ConfigureAwait(false); State = SensorConnectionState.Disconnected; }
        finally { _lifecycle.Release(); }
    }

    private async Task CleanupAsync()
    {
        var characteristic = _characteristic;
        _characteristic = null;
        if (characteristic is not null)
        {
            characteristic.CharacteristicValueChanged -= OnValueChanged;
            try { await characteristic.StopNotificationsAsync().ConfigureAwait(false); } catch { /* disconnected */ }
        }
        var device = _device;
        _device = null;
        if (device is not null)
        {
            device.GattServerDisconnected -= OnDisconnected;
            try { device.Gatt.Disconnect(); } catch { /* disconnected */ }
            try { device.Dispose(); } catch { /* disconnected */ }
        }
        _channel?.Writer.TryComplete();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await DisconnectAsync().ConfigureAwait(false);
        _disposed = true;
        _lifecycle.Dispose();
    }
}

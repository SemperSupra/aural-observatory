using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Android.Content.PM;
using AuralObservatory.Core;
using AuralObservatory.Sensors;
using AuralObservatory.Sensors.Ble;

namespace AuralObservatoryMobile;

/// <summary>Actual Android Views + native AudioTrack. No WebView, browser runtime, server, or network permission.</summary>
[Activity(Label = "Aural Observatory", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    private EditText _leftHz = null!, _rightHz = null!, _leftGain = null!, _rightGain = null!;
    private EditText _leftWidth = null!, _rightWidth = null!, _amHz = null!, _amDepth = null!;
    private EditText _before = null!, _after = null!;
    private Spinner _leftNoise = null!, _rightNoise = null!;
    private CheckBox _leftNotch = null!, _rightNotch = null!;
    private TextView _status = null!;
    private Button _play = null!;
    private CancellationTokenSource? _cancellation;
    private AudioTrack? _track;
    private bool _recordable;
    private string _configurationSnapshot = "";
    private readonly object _audioLock = new();
    private readonly object _sensorLogLock = new();
    private BleHeartRateSensor? _sensor;
    private CancellationTokenSource? _sensorCts;
    private bool _sensorConnecting;
    private TextView _sensorStatus = null!;
    private CheckBox _recordSensor = null!;
    private readonly System.Collections.Generic.Queue<ushort> _rrWindow = new();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var outer = new ScrollView(this) { FillViewport = true };
        var layout = new LinearLayout(this) { Orientation = Android.Widget.Orientation.Vertical };
        layout.SetPadding(32, 28, 32, 36);
        outer.AddView(layout);
        SetContentView(outer);

        AddText(layout, "AURAL OBSERVATORY", 24);
        AddText(layout, "EXPERIMENTAL · Not a medical device · No demonstrated tinnitus treatment efficacy.", 12);
        AddText(layout, "Use comfortable volume. Output is digitally attenuated but NOT sound-pressure calibrated. Stop if uncomfortable. Headphones recommended; never increase volume to compensate for poor audibility.", 12);

        AddText(layout, "LEFT EAR", 19);
        _leftHz = AddNumber(layout, "Pitch / notch center (Hz)", "4000");
        _leftGain = AddNumber(layout, "Gain fraction 0–1", "0.6");
        _leftWidth = AddNumber(layout, "Notch width (octaves)", "1.0");
        _leftNoise = AddNoisePicker(layout);
        _leftNotch = AddCheck(layout, "Enable notch", true);

        AddText(layout, "RIGHT EAR", 19);
        _rightHz = AddNumber(layout, "Pitch / notch center (Hz)", "6000");
        _rightGain = AddNumber(layout, "Gain fraction 0–1", "0.6");
        _rightWidth = AddNumber(layout, "Notch width (octaves)", "1.0");
        _rightNoise = AddNoisePicker(layout);
        _rightNotch = AddCheck(layout, "Enable notch", true);

        AddText(layout, "TEMPORAL MODULATION", 19);
        _amHz = AddNumber(layout, "Amplitude modulation (Hz; 0–20)", "0");
        _amDepth = AddNumber(layout, "Modulation depth (0–0.9)", "0");
        _status = AddText(layout, "Idle. Audio plays only after pressing Start.", 13);

        _play = AddButton(layout, "START 15-SECOND PREVIEW", () => StartPreview());
        AddButton(layout, "STOP", StopPreview);
        AddText(layout, "LIVE HEART-RATE SENSOR (OPTIONAL)", 19);
        AddText(layout, "Garmin HRM-Pro Plus / compatible BLE 0x180D strap. Live BPM and optional RR intervals. These do NOT detect tinnitus suppression. No upload or cloud connection.", 12);
        _sensorStatus = AddText(layout, "Sensor disconnected. Wear and moisten the chest strap before connecting.", 13);
        AddButton(layout, "CONNECT BLE HEART-RATE SENSOR", () => _ = ConnectSensorAsync());
        AddButton(layout, "DISCONNECT SENSOR", () => _ = DisconnectSensorAsync());
        _recordSensor = AddCheck(layout, "Record sensor readings locally ONLY while a preview plays", false);
        AddButton(layout, "SHARE SENSOR READINGS AS CSV", ShareSensorObservations);
        AddText(layout, "SELF-OBSERVATION (0–10)", 19);
        _before = AddNumber(layout, "Tinnitus loudness BEFORE playback", "");
        _after = AddNumber(layout, "Tinnitus loudness AFTER playback", "");
        AddButton(layout, "SAVE OBSERVATION LOCALLY", SaveObservation);
        AddButton(layout, "SHARE OBSERVATIONS AS CSV", ShareObservations);
        AddText(layout, "Ratings describe personal experience; heart rate and HRV do not prove tinnitus suppression. The optional BLE sensor needs explicit connection and recording consent. No microphone, location readings, or network service is used.", 12);
    }

    private void StartPreview()
    {
        if (_cancellation != null) return;
        try
        {
            var settings = CaptureSettings();
            var engine = new StimulusEngine(settings);
            _configurationSnapshot = string.Join(",", settings.Left.FrequencyHz, settings.Right.FrequencyHz,
                settings.Left.Colour, settings.Right.Colour, settings.Left.NotchEnabled, settings.Right.NotchEnabled,
                settings.ModulationHz, settings.ModulationDepth);
            // Native Android streaming audio; the Android audio mixer still controls headphone SPL.
            const int sr = StimulusValidation.SampleRate;
            int min = AudioTrack.GetMinBufferSize(sr, ChannelOut.Stereo, Android.Media.Encoding.Pcm16bit);
            if (min <= 0) throw new InvalidOperationException("Audio output is unavailable.");
            var track = new AudioTrack(Android.Media.Stream.Music, sr, ChannelOut.Stereo,
                Android.Media.Encoding.Pcm16bit, Math.Max(min, 4096), AudioTrackMode.Stream);
            if (track.State != AudioTrackState.Initialized)
            {
                track.Release();
                throw new InvalidOperationException("AudioTrack did not initialize.");
            }
            var cts = new CancellationTokenSource();
            lock (_audioLock) { _track = track; _cancellation = cts; }
            _recordable = false;
            _play.Enabled = false;
            _status.Text = "Playing 15-second experimental preview. STOP is available.";
            _ = Task.Run(() => RenderPlayback(engine, track, cts));
        }
        catch (Exception ex) { _status.Text = "Unable to start: " + ex.Message; }
    }

    private void RenderPlayback(StimulusEngine engine, AudioTrack track, CancellationTokenSource cts)
    {
        const int framesPerBlock = 1024;
        const int durationFrames = 15 * StimulusValidation.SampleRate;
        var buffer = new short[framesPerBlock * 2];
        bool completed = false;
        try
        {
            track.Play();
            int written = 0;
            while (written < durationFrames && !cts.IsCancellationRequested)
            {
                int frames = Math.Min(framesPerBlock, durationFrames - written);
                engine.Render(buffer, frames);
                // End-of-session fade is applied separately from the engine's startup ramp.
                for (int i = 0; i < frames; i++)
                {
                    double ramp = Math.Min(1.0, (durationFrames - written - i) / (0.1 * StimulusValidation.SampleRate));
                    buffer[2 * i] = (short)Math.Round(buffer[2 * i] * ramp);
                    buffer[2 * i + 1] = (short)Math.Round(buffer[2 * i + 1] * ramp);
                }
                int count = 0;
                while (count < frames * 2 && !cts.IsCancellationRequested)
                {
                    int accepted = track.Write(buffer, count, frames * 2 - count);
                    if (accepted <= 0) throw new IOException("Android AudioTrack write failed: " + accepted);
                    count += accepted;
                }
                written += count / 2;
            }
            completed = !cts.IsCancellationRequested && written >= durationFrames;
        }
        catch (Exception ex)
        {
            RunOnUiThread(() => _status.Text = "Audio error: " + ex.Message);
        }
        finally
        {
            try { track.Pause(); track.Flush(); track.Release(); } catch { /* cleanup only */ }
            lock (_audioLock) { if (ReferenceEquals(_track, track)) { _track = null; _cancellation = null; } }
            cts.Dispose();
            bool finalCompleted = completed;
            RunOnUiThread(() =>
            {
                _play.Enabled = true;
                _recordable = finalCompleted;
                if (finalCompleted) _status.Text = "15-second preview completed. Rate symptoms after silence.";
                else if (_status.Text?.StartsWith("Audio error:", StringComparison.Ordinal) != true)
                    _status.Text = "Stopped; incomplete preview not eligible for outcome logging.";
            });
        }
    }

    private void StopPreview()
    {
        lock (_audioLock)
        {
            _cancellation?.Cancel();
            try { _track?.Pause(); _track?.Flush(); } catch { /* device may already be stopped */ }
        }
    }

    protected override void OnPause()
    {
        StopPreview();
        _ = DisconnectSensorAsync(); // foreground-only capture; no silent background sampling
        base.OnPause();
    }
    protected override void OnDestroy() { StopPreview(); _ = DisconnectSensorAsync(); base.OnDestroy(); }

    private bool EnsureBlePermission()
    {
        string[] required = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? new[] { "android.permission.BLUETOOTH_SCAN", "android.permission.BLUETOOTH_CONNECT" }
            : new[] { Android.Manifest.Permission.AccessFineLocation! };
        if (required.All(permission => CheckSelfPermission(permission) == Permission.Granted)) return true;
        _sensorStatus.Text = "Android needs BLE permission to discover your strap. Approve, then tap Connect again.";
        RequestPermissions(required, 201);
        return false;
    }

    private async Task ConnectSensorAsync()
    {
        if (_sensorConnecting || _sensor is not null || !EnsureBlePermission()) return;
        _sensorConnecting = true;
        var sensor = new BleHeartRateSensor();
        var cts = new CancellationTokenSource();
        _sensorCts = cts;
        try
        {
            _sensorStatus.Text = "Choose a nearby BLE heart-rate device…";
            await sensor.ConnectAsync(cts.Token);
            _sensor = sensor;
            _sensorStatus.Text = "Connected: " + sensor.DeviceName + ". Waiting for heart-rate packets…";
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var observation in sensor.ObserveAsync(cts.Token))
                    {
                        RunOnUiThread(() => ShowSensorObservation(observation));
                    }
                }
                catch (System.OperationCanceledException) { }
                catch (Exception ex) { RunOnUiThread(() => _sensorStatus.Text = "Sensor stream ended: " + ex.Message); }
            });
        }
        catch (System.OperationCanceledException) { _sensorStatus.Text = "Sensor selection cancelled."; await sensor.DisposeAsync(); }
        catch (Exception ex) { _sensorStatus.Text = "BLE connection failed: " + ex.Message; await sensor.DisposeAsync(); }
        finally
        {
            if (_sensor is null)
            {
                if (ReferenceEquals(_sensorCts, cts)) _sensorCts = null;
                cts.Dispose();
            }
            _sensorConnecting = false;
        }
    }

    private void ShowSensorObservation(HeartRateObservation observation)
    {
        var m = observation.Measurement;
        foreach (ushort ticks in m.RrTicks1024)
        {
            _rrWindow.Enqueue(ticks);
            if (_rrWindow.Count > 600) _rrWindow.Dequeue();
        }
        var analysis = RrMetrics.CalculateRmssd(_rrWindow);
        string hrv = analysis.RmssdMs is double v ? $"{v:F1} ms (exploratory)" : "insufficient clean beats";
        _sensorStatus.Text = $"{observation.DeviceName}: {m.BeatsPerMinute} BPM | RR: {(m.HasRr ? string.Join("/", m.RrMilliseconds.Select(x => x.ToString("F0"))) + " ms" : "not provided")} | RMSSD: {hrv} | contact: {m.Contact}";
        if (!_recordSensor.Checked || _cancellation is null) return;
        string path = Path.Combine(FilesDir!.AbsolutePath, "ble_sensor_samples.csv");
        string rr = string.Join(";", m.RrMilliseconds.Select(x => x.ToString("F3", CultureInfo.InvariantCulture)));
        string line = string.Join(",", observation.ReceivedAtUtc.ToString("O"),
            observation.HostMonotonicSeconds.ToString("F6", CultureInfo.InvariantCulture),
            observation.Sequence, m.BeatsPerMinute, m.Contact, rr, m.HasRr, observation.DroppedNotifications);
        _ = Task.Run(() =>
        {
            lock (_sensorLogLock)
            {
                if (!File.Exists(path)) File.WriteAllText(path, "received_utc,host_monotonic_s,sequence,bpm,contact,rr_ms,has_rr,dropped_notifications\n");
                File.AppendAllText(path, line + "\n");
            }
        });
    }

    private async Task DisconnectSensorAsync()
    {
        _sensorCts?.Cancel();
        var sensor = _sensor;
        _sensor = null;
        _sensorCts = null;
        _rrWindow.Clear();
        if (sensor is not null)
        {
            try { await sensor.DisposeAsync(); } catch { /* already disconnected */ }
        }
        if (!IsFinishing && _sensorStatus is not null) _sensorStatus.Text = "Sensor disconnected.";
    }

    private void ShareSensorObservations()
    {
        string path = Path.Combine(FilesDir!.AbsolutePath, "ble_sensor_samples.csv");
        if (!File.Exists(path)) { _sensorStatus.Text = "No sensor recordings yet. Enable opt-in recording during a preview."; return; }
        var intent = new Intent(Intent.ActionSend);
        intent.SetType("text/csv");
        intent.PutExtra(Intent.ExtraText, File.ReadAllText(path));
        StartActivity(Intent.CreateChooser(intent, "Export local HR and RR observations"));
    }

    private StimulusSettings CaptureSettings()
    {
        EarProfile Left() => new(Parse(_leftHz), Parse(_leftGain), Parse(_leftWidth),
            (NoiseColour)_leftNoise.SelectedItemPosition, _leftNotch.Checked);
        EarProfile Right() => new(Parse(_rightHz), Parse(_rightGain), Parse(_rightWidth),
            (NoiseColour)_rightNoise.SelectedItemPosition, _rightNotch.Checked);
        return new StimulusSettings(Left(), Right(), Parse(_amHz), Parse(_amDepth));
    }

    private void SaveObservation()
    {
        if (!_recordable) { _status.Text = "Complete a preview before saving an outcome."; return; }
        try
        {
            double before = Parse(_before), after = Parse(_after);
            if (before < 0 || before > 10 || after < 0 || after > 10)
                throw new ArgumentOutOfRangeException(nameof(before), "Ratings must be 0–10.");
            var line = string.Join(",", DateTimeOffset.UtcNow.ToString("O"),
                before.ToString(CultureInfo.InvariantCulture), after.ToString(CultureInfo.InvariantCulture),
                _configurationSnapshot);
            File.AppendAllText(Path.Combine(FilesDir!.AbsolutePath, "observations.csv"), line + "\n");
            _recordable = false;
            _status.Text = "Observation stored on device. No network transfer.";
        }
        catch (Exception ex) { _status.Text = "Could not save: " + ex.Message; }
    }

    private void ShareObservations()
    {
        var path = Path.Combine(FilesDir!.AbsolutePath, "observations.csv");
        if (!File.Exists(path)) { _status.Text = "No observations yet."; return; }
        var rows = File.ReadAllText(path);
        var header = "timestamp_utc,before_0_10,after_0_10,left_hz,right_hz,left_noise,right_noise,left_notch,right_notch,am_hz,am_depth\n";
        var intent = new Intent(Intent.ActionSend);
        intent.SetType("text/csv");
        intent.PutExtra(Intent.ExtraText, header + rows);
        StartActivity(Intent.CreateChooser(intent, "Export observations"));
    }

    private static double Parse(EditText field) => double.Parse(field.Text ?? "", CultureInfo.InvariantCulture);
    private TextView AddText(LinearLayout p, string t, float size)
    {
        var v = new TextView(this) { Text = t, TextSize = size };
        v.SetPadding(0, 12, 0, 6);
        p.AddView(v);
        return v;
    }
    private EditText AddNumber(LinearLayout p, string label, string value)
    {
        AddText(p, label, 13);
        var v = new EditText(this) { Text = value };
        v.InputType = Android.Text.InputTypes.ClassNumber |
            Android.Text.InputTypes.NumberFlagDecimal | Android.Text.InputTypes.NumberFlagSigned;
        p.AddView(v);
        return v;
    }
    private Spinner AddNoisePicker(LinearLayout p)
    {
        AddText(p, "Noise color (spectra are approximations)", 13);
        var s = new Spinner(this);
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem,
            Enum.GetNames<NoiseColour>());
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        s.Adapter = adapter;
        s.SetSelection((int)NoiseColour.Pink);
        p.AddView(s);
        return s;
    }
    private CheckBox AddCheck(LinearLayout p, string label, bool isChecked)
    {
        var v = new CheckBox(this) { Text = label, Checked = isChecked };
        p.AddView(v);
        return v;
    }
    private Button AddButton(LinearLayout p, string name, Action action)
    {
        var btn = new Button(this) { Text = name };
        btn.Click += (_, _) => action();
        p.AddView(btn);
        return btn;
    }
}

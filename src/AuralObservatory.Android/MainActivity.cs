using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Speech;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Android.Graphics;
using Path = System.IO.Path;
using Android.Content.Res;
using Android.Util;
using System.Collections.Generic;
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
    private EditText _sweepStartHz = null!, _sweepEndHz = null!, _sweepDuration = null!;
    private Spinner _sweepMode = null!, _sweepEar = null!;
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
    private QuietSearch? _search;
    private TextView _searchStatus = null!;
    private EditText _voiceNote = null!;
    private Button _quietHold = null!;
    private Spinner _quietMode = null!;
    private readonly QuietInputController _quietInput = new();
    private double _lastSoundStoppedAt;
    private UserPreferences _preferences = UserPreferences.Default;
    private string _preferencesPath = "";
    private LinearLayout _rootPanel = null!;
    private Spinner _appearanceChoice = null!, _fontChoice = null!, _sizeChoice = null!, _presetChoice = null!;
    private CheckBox _contrastChoice = null!, _motionChoice = null!;
    private EditText _presetName = null!;
    private readonly Dictionary<TextView,float> _baseTextSizes = new();



    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _preferencesPath = Path.Combine(FilesDir!.AbsolutePath, "preferences-v1.json");
        string? preferenceError = null;
        try { _preferences = UserPreferencesStore.ReadFile(_preferencesPath); }
        catch(Exception ex) { preferenceError = ex.Message; } // Leave invalid/future preferences intact; never erase silently.
        var outer = new ScrollView(this) { FillViewport = true };
        var layout = new LinearLayout(this) { Orientation = Android.Widget.Orientation.Vertical };
        _rootPanel = layout;
        layout.SetPadding(32, 28, 32, 36);
        outer.AddView(layout);
        SetContentView(outer);

        AddText(layout, "AURAL OBSERVATORY", 24);
        AddText(layout, "EXPERIMENTAL · Not a medical device · No demonstrated tinnitus treatment efficacy.", 12);
        AddText(layout, "Use comfortable volume. Output is digitally attenuated but NOT sound-pressure calibrated. Stop if uncomfortable. Headphones recommended; never increase volume to compensate for poor audibility.", 12);

        var appearancePanel = AddExpandableSection(layout,"Appearance and accessibility",false);
        _appearanceChoice = AddChoice(appearancePanel,"Appearance",new[]{"System","Light","Dark"},(int)_preferences.Appearance);
        _fontChoice = AddChoice(appearancePanel,"Font family",new[]{"System","Sans serif","Serif","Monospace"},(int)_preferences.FontFamily);
        _sizeChoice = AddChoice(appearancePanel,"Text scale",new[]{"80%","100%","125%","150%","200%"},
            _preferences.FontScalePercent switch{80=>0,100=>1,125=>2,150=>3,200=>4,_=>1});
        _contrastChoice = AddCheck(appearancePanel,"High contrast",_preferences.HighContrast);
        _motionChoice = AddCheck(appearancePanel,"Reduce motion",_preferences.ReduceMotion);
        AddButton(appearancePanel,"SAVE APPEARANCE AND AUDIO SETTINGS",SavePreferences);
        _presetName = new EditText(this){Hint="New preset name (up to 64 characters)"};

        appearancePanel.AddView(_presetName);
        AddButton(appearancePanel,"SAVE NAMED AUDIO PRESET",SaveNamedPreset);
        _presetChoice = AddChoice(appearancePanel,"Saved audio presets",new[]{"None saved"},0);
        AddButton(appearancePanel,"LOAD SELECTED PRESET",LoadSelectedPreset);
        AddButton(appearancePanel,"EXPORT SETTINGS / PRESETS AS JSON",ExportPreferences);
        AddButton(appearancePanel,"IMPORT SETTINGS / PRESETS FROM JSON",ImportPreferences);
        AddText(appearancePanel,"User data survives normal installed upgrades but may be removed by uninstall or debug-signature changes. Export before replacing development builds.",12);
        if (preferenceError != null) AddText(appearancePanel,
            "Saved preferences could not be read and were preserved: "+preferenceError,12);
        var soundPanel = AddExpandableSection(layout,"Sound configuration and preview",true);
        AddText(soundPanel, "LEFT EAR", 19);
        _leftHz = AddNumber(soundPanel, "Pitch / notch center (Hz)", "4000");
        _leftGain = AddNumber(soundPanel, "Gain fraction 0–1", "0.6");
        _leftWidth = AddNumber(soundPanel, "Notch width (octaves)", "1.0");
        _leftNoise = AddNoisePicker(soundPanel);
        _leftNotch = AddCheck(soundPanel, "Enable notch", true);

        AddText(soundPanel, "RIGHT EAR", 19);
        _rightHz = AddNumber(soundPanel, "Pitch / notch center (Hz)", "6000");
        _rightGain = AddNumber(soundPanel, "Gain fraction 0–1", "0.6");
        _rightWidth = AddNumber(soundPanel, "Notch width (octaves)", "1.0");
        _rightNoise = AddNoisePicker(soundPanel);
        _rightNotch = AddCheck(soundPanel, "Enable notch", true);

        AddText(soundPanel, "TEMPORAL MODULATION", 19);
        _amHz = AddNumber(soundPanel, "Amplitude modulation (Hz; 0–20)", "0");
        _amDepth = AddNumber(soundPanel, "Modulation depth (0–0.9)", "0");
        _status = AddText(soundPanel, "Idle. Audio plays only after pressing Start.", 13);
        _status.AccessibilityLiveRegion = AccessibilityLiveRegion.Polite;

        _play = AddButton(soundPanel, "START 15-SECOND PREVIEW", () => StartPreview());
        AddButton(soundPanel, "STOP", StopPreview);
        AddText(soundPanel, "OPTIONAL SINGLE SWEEP (not a hearing test or treatment)", 17);
        AddText(soundPanel, "Low device volume; 8–15 seconds only. Select direction by setting start and end frequencies. The separate randomized quiet search is unchanged.", 12);
        _sweepMode = AddChoice(soundPanel, "Sweep signal", new[] { "Reference tone", "Moving notch on existing noise" }, 0);
        _sweepEar = AddChoice(soundPanel, "Sweep ear", new[] { "Both", "Left", "Right" }, 0);
        _sweepStartHz = AddNumber(soundPanel, "Sweep start Hz (200–10000)", "500");
        _sweepEndHz = AddNumber(soundPanel, "Sweep end Hz (200–10000)", "8000");
        _sweepDuration = AddNumber(soundPanel, "Sweep seconds (8–15)", "10");
        AddButton(soundPanel, "START ONE-TIME SWEEP", StartSweepPreview);
        var sensorPanel = AddExpandableSection(layout,"Sensors and BLE measurements",false);
        AddText(sensorPanel, "LIVE HEART-RATE SENSOR (OPTIONAL)", 19);
        AddText(sensorPanel, "Garmin HRM-Pro Plus / compatible BLE 0x180D strap. Live BPM and optional RR intervals. These do NOT detect tinnitus suppression. No upload or cloud connection.", 12);
        _sensorStatus = AddText(sensorPanel, "Sensor disconnected. Wear and moisten the chest strap before connecting.", 13);
        _sensorStatus.AccessibilityLiveRegion = AccessibilityLiveRegion.Polite;
        AddButton(sensorPanel, "CONNECT BLE HEART-RATE SENSOR", () => _ = ConnectSensorAsync());
        AddButton(sensorPanel, "DISCONNECT SENSOR", () => _ = DisconnectSensorAsync());
        _recordSensor = AddCheck(sensorPanel, "Record sensor readings locally ONLY while a preview plays", false);
        AddButton(sensorPanel, "SHARE SENSOR READINGS AS CSV", ShareSensorObservations);
        var searchPanel = AddExpandableSection(layout,"Quiet search and response",true);
        AddText(searchPanel, "QUIET SEARCH (EXPERIMENTAL)", 19);
        AddText(searchPanel, "Randomized fixed-level frequency trials. Hold QUIET only while tinnitus SEEMS absent. During sound = masking; after sound = possible residual inhibition. Never increases volume. Not evidence of efficacy.", 12);
        AddText(searchPanel, "Response style (choose before starting search)", 13);
        _quietMode = new Spinner(this);
        var modeAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem,
            new[] { "Hold when quiet", "Hold while audible; RELEASE when quiet", "Tap to toggle quiet" });
        modeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _quietMode.Adapter = modeAdapter;
        searchPanel.AddView(_quietMode);
        _searchStatus = AddText(searchPanel, "No search started. Each candidate plays for 15 seconds; mark quiet during and after sound.", 13);
        _searchStatus.AccessibilityLiveRegion = AccessibilityLiveRegion.Polite;
        AddButton(searchPanel, "START / NEXT SEARCH TRIAL", SearchNext);
        _quietHold = AddButton(searchPanel, "PRESS AND HOLD WHEN QUIET", () => {
            // TalkBack/Switch Access expose click actions rather than raw pointer presses.
            if (_quietInput.Mode == QuietInputMode.ToggleQuiet) { QuietButton(true); QuietButton(false); }
            else QuietButton(!_quietInput.IsPressed);
        });
        _quietHold.Touch += (_, e) =>
        {
            if (e.Event is null) return;
            if (e.Event.ActionMasked == MotionEventActions.Down) { QuietButton(true); e.Handled = true; }
            else if (e.Event.ActionMasked == MotionEventActions.Up)
            { QuietButton(false); e.Handled = true; }
            else if (e.Event.ActionMasked == MotionEventActions.Cancel)
            { if (_search?.State is SearchState.Stimulating or SearchState.PostStimulus) _search.MarkQuiet(false, QuietSearch.NowSeconds()); _quietInput.Cancel(); e.Handled = true; }
        };
        _voiceNote = new EditText(this) { Hint = "Optional note, or dictate after playback" };
        searchPanel.AddView(_voiceNote);
        AddButton(searchPanel, "DICTATE OPTIONAL NOTE (SYSTEM SPEECH)", DictateNote);
        AddButton(searchPanel, "END SEARCH / SAVE CURRENT TRIAL", EndSearch);
        AddButton(searchPanel, "NEW SEARCH (KEEP SAVED RESULTS)", ResetSearch);
        AddText(searchPanel, "Voice transcription is provided by the installed Android speech service and may use the network. It is opt-in and disabled during playback. No voice-controlled gain or treatment changes.", 12);
        var observationPanel = AddExpandableSection(layout,"Observations and export",false);
        AddText(observationPanel, "SELF-OBSERVATION (0–10)", 19);
        _before = AddNumber(observationPanel, "Tinnitus loudness BEFORE playback", "");
        _after = AddNumber(observationPanel, "Tinnitus loudness AFTER playback", "");
        AddButton(observationPanel, "SAVE OBSERVATION LOCALLY", SaveObservation);
        AddButton(observationPanel, "SHARE OBSERVATIONS AS CSV", ShareObservations);
        AddText(observationPanel, "Ratings describe personal experience; heart rate and HRV do not prove tinnitus suppression. The optional BLE sensor needs explicit connection and recording consent. No microphone, location readings, or network service is used.", 12);
        ApplyStimulusToFields(_preferences.LastStimulus);
        _quietMode.SetSelection((int)_preferences.ResponseMode);
        RefreshPresetChoices();
        ApplyAppearance();
    }

    private void StartSweepPreview()
    {
        if (_cancellation is not null) { _status.Text = "Stop current playback before starting a sweep."; return; }
        if (_search?.State is SearchState.Stimulating or SearchState.PostStimulus)
        { _status.Text = "Finish or stop the quiet-search trial before using manual sweep."; return; }
        try
        {
            if (!int.TryParse(_sweepDuration.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds))
                throw new ArgumentException("Sweep seconds must be an integer from 8 to 15.");
            var mode = _sweepMode.SelectedItemPosition == 0
                ? AcousticSweepMode.ReferenceTone : AcousticSweepMode.NotchCenter;
            var ear = _sweepEar.SelectedItemPosition switch
            {
                1 => SearchEar.Left,
                2 => SearchEar.Right,
                _ => SearchEar.Both
            };
            var sweep = new AcousticSweep(mode, Parse(_sweepStartHz), Parse(_sweepEndHz), seconds, ear);
            sweep.Validate();
            StartPreview(sweep);
        }
        catch (Exception ex) { _status.Text = "Sweep rejected: " + ex.Message; }
    }

    private void StartPreview(AcousticSweep? sweep = null)
    {
        if (_cancellation != null) return;
        try
        {
            var settings = CaptureSettings() with { Sweep = sweep };
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
            _status.Text = sweep is null
                ? "Playing 15-second experimental preview. STOP is available."
                : $"Playing {sweep.DurationSeconds}-second manual {sweep.Mode} sweep. STOP is available.";
            _ = Task.Run(() => RenderPlayback(engine, track, cts, sweep));
        }
        catch (Exception ex)
        {
            if (_search?.State == SearchState.Stimulating) _search.Finish(QuietSearch.NowSeconds(), interrupted: true);
            _status.Text = "Unable to start: " + ex.Message;
        }
    }

    private void RenderPlayback(StimulusEngine engine, AudioTrack track, CancellationTokenSource cts,
        AcousticSweep? sweep)
    {
        const int framesPerBlock = 1024;
        int durationFrames = sweep?.TotalFrames ?? 15 * StimulusValidation.SampleRate;
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
                _recordable = finalCompleted && sweep is null;
                if (_search?.State == SearchState.Stimulating)
                {
                    if (finalCompleted)
                    {
                        _lastSoundStoppedAt = QuietSearch.NowSeconds();
                        _search.SoundStopped(_lastSoundStoppedAt);
                        _quietInput.Cancel(); // A new response phase must be deliberately armed.
                        _searchStatus.Text = "SOUND STOPPED. Re-arm the selected response style to mark post-sound quiet. Wait at least 10 seconds before advancing.";
                    }
                    else
                    {
                        _search.Finish(QuietSearch.NowSeconds(), interrupted: true);
                        _searchStatus.Text = "Audio interrupted: trial excluded from ranking.";
                    }
                }
                if (finalCompleted) _status.Text = sweep is null
                    ? "15-second preview completed. Rate symptoms after silence."
                    : "Manual sweep completed. No clinical inference or quiet-search score was recorded.";
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
        if (_search?.State is SearchState.Stimulating or SearchState.PostStimulus)
            _search.MarkQuiet(false, QuietSearch.NowSeconds());
        _quietInput.Cancel();
        _ = DisconnectSensorAsync(); // foreground-only capture; no silent background sampling
        base.OnPause();
    }
    protected override void OnDestroy() { StopPreview(); _ = DisconnectSensorAsync(); base.OnDestroy(); }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (keyCode == Keycode.Space && _search?.State is SearchState.Stimulating or SearchState.PostStimulus)
        { QuietButton(true); return true; }
        return base.OnKeyDown(keyCode, e);
    }
    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
    {
        if (keyCode == Keycode.Space && _search is not null)
        { QuietButton(false); return true; }
        return base.OnKeyUp(keyCode, e);
    }

    private void QuietButton(bool down)
    {
        if (_search?.State is not (SearchState.Stimulating or SearchState.PostStimulus)) return;
        var quiet = _quietInput.Process(down);
        if (quiet.HasValue) _search.MarkQuiet(quiet.Value, QuietSearch.NowSeconds());
        _quietHold.Text = _quietInput.Mode switch
        {
            QuietInputMode.ReleaseWhenQuiet => down ? "AUDIBLE — RELEASE WHEN QUIET" : "PRESS WHILE AUDIBLE; RELEASE WHEN QUIET",
            QuietInputMode.ToggleQuiet => _quietInput.IsQuiet ? "QUIET ACTIVE — TAP TO END" : "TAP WHEN QUIET (TOGGLE)",
            _ => down ? "QUIET ACTIVE — RELEASE TO END" : "PRESS AND HOLD WHEN QUIET"
        };
    }

    private void SearchNext()
    {
        if (_cancellation is not null) { _searchStatus.Text = "Stop or finish current playback first."; return; }
        try
        {
            _search ??= new QuietSearch(CaptureSettings(), SearchEar.Both);
            if (_search.State == SearchState.Ready) _quietMode.Enabled = false;
            if (_search.State == SearchState.PostStimulus)
            {
                if (QuietSearch.NowSeconds() - _lastSoundStoppedAt < 10)
                { _searchStatus.Text = "Observe at least 10 seconds of silence before advancing."; return; }
                CompleteSearchTrial(false);
            }
            if (_search.State == SearchState.Finished) { _searchStatus.Text = "Search finished. Reset app to start a new search."; return; }
            _quietInput.Reset((QuietInputMode)_quietMode.SelectedItemPosition);
            var candidate = _search.Begin(QuietSearch.NowSeconds());
            _leftHz.Text = candidate.Settings.Left.FrequencyHz.ToString("F2", CultureInfo.InvariantCulture);
            _rightHz.Text = candidate.Settings.Right.FrequencyHz.ToString("F2", CultureInfo.InvariantCulture);
            _searchStatus.Text = $"Trial {candidate.Number} / {candidate.Stage} at {candidate.FrequencyHz:F0} Hz (fixed level). Mode: {_quietInput.Mode}. Track quiet while playing and after stopping.";
            StartPreview();
        }
        catch (Exception ex) { _searchStatus.Text = "Search error: " + ex.Message; }
    }

    private void CompleteSearchTrial(bool interrupted)
    {
        if (_search?.State is not (SearchState.Stimulating or SearchState.PostStimulus)) return;
        _search.MarkQuiet(false, QuietSearch.NowSeconds());
        _quietInput.Cancel();
        double? after = double.TryParse(_after.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0 && v <= 10 ? v : null;
        _search.Annotate(_voiceNote.Text, after);
        var result = _search.Finish(QuietSearch.NowSeconds(), interrupted);
        string line = string.Join(",", DateTimeOffset.UtcNow.ToString("O"), result.Candidate.Number,
            result.Candidate.FrequencyHz.ToString("F2", CultureInfo.InvariantCulture),
            result.Candidate.Stage, result.Candidate.RepeatedControl,
            result.DuringQuietSeconds.ToString("F3", CultureInfo.InvariantCulture),
            result.AfterQuietSeconds.ToString("F3", CultureInfo.InvariantCulture),
            result.Interrupted, _quietInput.Mode, result.LoudnessAfter?.ToString(CultureInfo.InvariantCulture) ?? "",
            "\"" + (result.Note ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"");
        string file = Path.Combine(FilesDir!.AbsolutePath, "quiet_search_r7.csv");
        if (!File.Exists(file)) File.WriteAllText(file, "timestamp_utc,trial,hz,stage,control,during_quiet_s,after_quiet_s,interrupted,input_mode,loudness_after,note\n");
        File.AppendAllText(file, line + "\n");
        _voiceNote.Text = ""; _after.Text = "";
        _searchStatus.Text = $"Trial {result.Candidate.Number} saved locally. During: {result.DuringQuietSeconds:F1}s; AFTER silence: {result.AfterQuietSeconds:F1}s.";
    }
    private void EndSearch()
    {
        if (_cancellation is not null) { StopPreview(); _searchStatus.Text = "Stopping playback. Interrupted trial will not count."; return; }
        if (_search?.State == SearchState.PostStimulus) CompleteSearchTrial(false);
        else _searchStatus.Text = "No completed search trial to save.";
    }
    private void ResetSearch()
    {
        if (_cancellation is not null || _search?.State is SearchState.Stimulating or SearchState.PostStimulus)
        { _searchStatus.Text="Stop/finish the current audio and post-stimulus interval first."; return; }
        try {
            _search = new QuietSearch(CaptureSettings(), SearchEar.Both);
            _quietInput.Cancel();
            _quietMode.Enabled = true;
            _searchStatus.Text="New randomized frequency search ready; prior results remain in your user-data journal.";
        }catch(Exception ex){_searchStatus.Text="Cannot reset: "+ex.Message;}
    }

    private void DictateNote()
    {
        if (_cancellation is not null) { _searchStatus.Text = "Stop playback before using speech recognition."; return; }
        try
        {
            var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraPreferOffline, true); // A hint, not a guarantee.
            intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
            StartActivityForResult(intent, 470);
        }
        catch (Exception ex) { _searchStatus.Text = "No compatible system speech recognizer: " + ex.Message; }
    }
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode is 471 or 472) { HandlePreferencesDocument(requestCode, resultCode, data); return; }
        if (requestCode != 470 || resultCode != Result.Ok || data is null) return;
        var phrases = data.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
        if (phrases is { Count: > 0 }) _voiceNote.Text = phrases[0];
    }

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

    private LinearLayout AddExpandableSection(LinearLayout parent,string title,bool expanded)
    {
        var section = new LinearLayout(this) { Orientation = Android.Widget.Orientation.Vertical };
        Button? button = null;
        button = AddButton(parent,title + (expanded ? " (collapse)" : " (expand)"),()=> {
            bool open = section.Visibility == ViewStates.Gone;
            section.Visibility = open ? ViewStates.Visible : ViewStates.Gone;
            button!.Text = title + (open ? " (collapse)" : " (expand)");
            button.ContentDescription = button.Text;
        });
        button.ContentDescription = button.Text;
        parent.AddView(section);
        section.Visibility = expanded ? ViewStates.Visible : ViewStates.Gone;
        return section;
    }
    private Spinner AddChoice(LinearLayout parent,string label,string[] options,int selected)
    {
        AddText(parent,label,14);
        var spinner=new Spinner(this);
        var adapter=new ArrayAdapter<string>(this,Android.Resource.Layout.SimpleSpinnerItem,options);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        spinner.Adapter=adapter;
        spinner.SetSelection(Math.Clamp(selected,0,options.Length-1));
        spinner.ContentDescription=label;
        parent.AddView(spinner);
        return spinner;
    }
    private void ApplyStimulusToFields(StimulusSettings s)
    {
        _leftHz.Text=s.Left.FrequencyHz.ToString(CultureInfo.InvariantCulture);
        _rightHz.Text=s.Right.FrequencyHz.ToString(CultureInfo.InvariantCulture);
        _leftGain.Text=s.Left.Gain.ToString(CultureInfo.InvariantCulture);
        _rightGain.Text=s.Right.Gain.ToString(CultureInfo.InvariantCulture);
        _leftWidth.Text=s.Left.NotchWidthOctaves.ToString(CultureInfo.InvariantCulture);
        _rightWidth.Text=s.Right.NotchWidthOctaves.ToString(CultureInfo.InvariantCulture);
        _leftNoise.SetSelection((int)s.Left.Colour);
        _rightNoise.SetSelection((int)s.Right.Colour);
        _leftNotch.Checked=s.Left.NotchEnabled;
        _rightNotch.Checked=s.Right.NotchEnabled;
        _amHz.Text=s.ModulationHz.ToString(CultureInfo.InvariantCulture);
        _amDepth.Text=s.ModulationDepth.ToString(CultureInfo.InvariantCulture);
    }
    private void RefreshPresetChoices()
    {
        var labels=new List<string>{"Select a saved preset"};
        labels.AddRange(_preferences.Presets.Select(p=>p.Name));
        var adapter=new ArrayAdapter<string>(this,Android.Resource.Layout.SimpleSpinnerItem,labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _presetChoice.Adapter=adapter;
    }
    private bool CanChangeProfile()
    {
        if(_cancellation is null && _search?.State is not (SearchState.Stimulating or SearchState.PostStimulus)) return true;
        _status.Text="Finish/stop the current experiment before changing appearance or presets.";
        return false;
    }
    private void ExportPreferences()
    {
        if(!CanChangeProfile())return;
        try {
            // A user-chosen Storage Access Framework document, no broad file permission.
            var intent=new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/json");
            intent.PutExtra(Intent.ExtraTitle,"aural-observatory-preferences-v1.json");
            StartActivityForResult(intent,472);
        }catch(Exception ex){_status.Text="Settings export failed: "+ex.Message;}
    }
    private void ImportPreferences()
    {
        if(!CanChangeProfile())return;
        try{
            var intent=new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/json");
            StartActivityForResult(intent,471);
        }catch(Exception ex){_status.Text="Settings import failed: "+ex.Message;}
    }
    private void HandlePreferencesDocument(int requestCode,Result resultCode,Intent? data)
    {
        if(resultCode!=Result.Ok || data?.Data is null)return;
        if(!CanChangeProfile())return;
        try{
            using var stream=(requestCode==471
                ? ContentResolver!.OpenInputStream(data.Data)
                : ContentResolver!.OpenOutputStream(data.Data))
                ?? throw new IOException("Document stream unavailable");
            if(requestCode==471){
                using var reader=new StreamReader(stream,System.Text.Encoding.UTF8);
                var buffer=new char[65537];
                int count=reader.ReadBlock(buffer,0,buffer.Length);
                if(count>=buffer.Length)throw new IOException("Preferences JSON exceeds 64 KiB");
                var incoming=UserPreferencesStore.FromJson(new string(buffer,0,count));
                // No app settings changed until the entire document validates.
                UserPreferencesStore.WriteFile(_preferencesPath,incoming);
                _preferences=incoming;
                ApplyStimulusToFields(incoming.LastStimulus);
                _quietMode.SetSelection((int)incoming.ResponseMode);
                _appearanceChoice.SetSelection((int)incoming.Appearance);
                _fontChoice.SetSelection((int)incoming.FontFamily);
                int index=Array.IndexOf(new[]{80,100,125,150,200},incoming.FontScalePercent);
                _sizeChoice.SetSelection(index<0?1:index);
                _contrastChoice.Checked=incoming.HighContrast;
                _motionChoice.Checked=incoming.ReduceMotion;
                RefreshPresetChoices();
                ApplyAppearance();
                _status.Text="Preferences imported after schema validation; previous data preserved until import completed.";
            } else {
                var bytes=System.Text.Encoding.UTF8.GetBytes(UserPreferencesStore.ToJson(_preferences));
                stream.Write(bytes,0,bytes.Length);
                stream.Flush();
                _status.Text="Settings/presets exported to the location you selected.";
            }
        }catch(Exception ex){_status.Text="Settings document operation failed: "+ex.Message;}
    }

    private void SavePreferences()
    {
        if(!CanChangeProfile())return;
        try{
            var scales=new[]{80,100,125,150,200};
            var next=_preferences with {
                Appearance=(AppearanceMode)_appearanceChoice.SelectedItemPosition,
                FontFamily=(FontFamilyChoice)_fontChoice.SelectedItemPosition,
                FontScalePercent=scales[_sizeChoice.SelectedItemPosition],
                HighContrast=_contrastChoice.Checked,
                ReduceMotion=_motionChoice.Checked,
                ResponseMode=(QuietInputMode)_quietMode.SelectedItemPosition,
                LastStimulus=CaptureSettings()
            };
            UserPreferencesStore.WriteFile(_preferencesPath,next);
            _preferences=next;
            ApplyAppearance();
            _status.Text="Settings saved to local user profile, separate from application binaries.";
        }catch(Exception ex){_status.Text="Settings not saved: "+ex.Message;}
    }
    private void SaveNamedPreset()
    {
        if(!CanChangeProfile())return;
        try{
            var next=_preferences with {LastStimulus=CaptureSettings()};
            next=UserPreferencesStore.SavePreset(next,_presetName.Text??"",DateTimeOffset.UtcNow);
            UserPreferencesStore.WriteFile(_preferencesPath,next);
            _preferences=next;
            RefreshPresetChoices();
            _status.Text="Named audio preset saved locally.";
        }catch(Exception ex){_status.Text="Preset not saved: "+ex.Message;}
    }
    private void LoadSelectedPreset()
    {
        if(!CanChangeProfile())return;
        int n=_presetChoice.SelectedItemPosition-1;
        if(n<0||n>=_preferences.Presets.Count){_status.Text="Select a saved preset first.";return;}
        try{
            var profile=_preferences.Presets[n].Stimulus;
            StimulusValidation.Validate(profile);
            ApplyStimulusToFields(profile);
            _preferences=_preferences with {LastStimulus=profile};
            UserPreferencesStore.WriteFile(_preferencesPath,_preferences);
            _status.Text="Preset loaded; the next experiment will use these settings.";
        }catch(Exception ex){_status.Text="Preset not loaded: "+ex.Message;}
    }
    private void ApplyAppearance()
    {
        var scheme=_preferences.Appearance;
        bool systemDark=(((int)Resources!.Configuration!.UiMode)&0x30)==0x20;
        bool dark=scheme==AppearanceMode.Dark || (scheme==AppearanceMode.System && systemDark);
        var bg=Color.ParseColor(_preferences.HighContrast?(dark?"#000000":"#FFFFFF"):(dark?"#17212E":"#F6F8FB"));
        var fg=Color.ParseColor(_preferences.HighContrast?(dark?"#FFFFFF":"#000000"):(dark?"#F3F7FC":"#17202E"));
        var accent=Color.ParseColor(dark?"#A4CAEF":"#213E64");
        var buttonText=Color.ParseColor(dark?"#111C2B":"#FFFFFF");
        _rootPanel.SetBackgroundColor(bg);
        float scale=_preferences.FontScalePercent/100f;
        var font=_preferences.FontFamily switch {
            FontFamilyChoice.Serif=>Typeface.Serif,
            FontFamilyChoice.Monospace=>Typeface.Monospace,
            FontFamilyChoice.SansSerif=>Typeface.SansSerif,
            _=>Typeface.Default
        };
        void Paint(View view)
        {
            if(view is TextView t){
                t.SetTextColor(fg);
                t.SetTypeface(font,TypefaceStyle.Normal);
                float baseSp=_baseTextSizes.TryGetValue(t,out var value)?value:(t is EditText?16:14);
                t.SetTextSize(ComplexUnitType.Sp,baseSp*scale);
                if(t is EditText input) input.SetHintTextColor(fg);
                if(t is Button button){
                    button.BackgroundTintList=ColorStateList.ValueOf(accent);
                    button.SetTextColor(buttonText);
                }
            }
            if(view is Android.Views.ViewGroup group)
                for(int i=0;i<group.ChildCount;i++)Paint(group.GetChildAt(i)!);
        }
        Paint(_rootPanel);
    }

    private static double Parse(EditText field) => double.Parse(field.Text ?? "", CultureInfo.InvariantCulture);
    private TextView AddText(LinearLayout p, string t, float size)
    {
        var v = new TextView(this) { Text = t, TextSize = size };
        v.SetPadding(0, 12, 0, 6);
        p.AddView(v);
        _baseTextSizes[v] = size;
        return v;
    }
    private EditText AddNumber(LinearLayout p, string label, string value)
    {
        AddText(p, label, 13);
        var v = new EditText(this) { Text = value };
        v.Hint = label;
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
        btn.SetMinHeight((int)Math.Ceiling(48 * Resources!.DisplayMetrics!.Density));
        btn.Click += (_, _) => action();
        p.AddView(btn);
        return btn;
    }
}

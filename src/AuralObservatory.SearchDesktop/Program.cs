using AuralObservatory.Core;
using AuralObservatory.SearchDesktop;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Net;

// Local-only browser frontend, with native C# DSP. No browser-side sound synthesis or Internet access.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:0"); // OS-selected ephemeral port; bound to local loopback only.
var app = builder.Build();
var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var gate = new object();
var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuralObservatory");
Directory.CreateDirectory(dataDir);
using var desktopSession = DesktopSession.TryAcquire(dataDir);
if(desktopSession is null)
{
    Console.Error.WriteLine("An Aural Observatory desktop session is already running for this OS user.");
    return; // Never bind a competing session or overwrite user preferences.
}
var preferencesPath = Path.Combine(dataDir, "preferences-v1.json");
UserPreferences preferences;
try { preferences = UserPreferencesStore.ReadFile(preferencesPath); }
catch (Exception ex) {
    // Fail visibly, preserve invalid or future-version document; do NOT erase evidence.
    Console.Error.WriteLine("Preferences unreadable (left unchanged): " + ex.Message);
    preferences = UserPreferences.Default;
}
var search = new QuietSearch(preferences.LastStimulus);
var audio = Array.Empty<byte>();
var lastStopped = 0d;
var currentAudioId = 0;
var currentInputMode = preferences.ResponseMode;
var logPath = Path.Combine(dataDir, "quiet_search_r7.csv");
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.CacheControl = "no-store";
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'none'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "connect-src 'self'; media-src 'self' blob:; img-src 'self' data:; font-src 'self'; " +
        "base-uri 'none'; frame-ancestors 'none'; form-action 'none'";
    // Host and Origin checks block local-service DNS rebinding and cross-origin execution.
    if (!string.Equals(ctx.Request.Host.Host, "127.0.0.1", StringComparison.Ordinal) ||
        ctx.Request.Host.Port is null)
    { ctx.Response.StatusCode = 403; return; }
    if (ctx.Request.Headers.TryGetValue("Origin", out var requestOrigins) && requestOrigins.Count > 0)
    {
        var expected = "http://127.0.0.1:" + ctx.Request.Host.Port.Value;
        if (requestOrigins.Count != 1 ||
            !string.Equals(requestOrigins.ToString(), expected, StringComparison.Ordinal))
        { ctx.Response.StatusCode = 403; return; }
    }
    if (ctx.Request.Path.StartsWithSegments("/api") &&
        ctx.Request.Headers["X-Aural-Token"] != token)
    { ctx.Response.StatusCode = 403; return; }
    await next();
});
app.MapGet("/", () =>
{
    var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html"))
        .Replace("__SESSION_TOKEN__", token, StringComparison.Ordinal);
    return Results.Content(html, "text/html; charset=utf-8");
});
app.MapGet("/api/preferences", () =>
{
    lock(gate) return Results.Json(preferences);
});
app.MapPost("/api/preferences", (UserPreferences input) =>
{
    lock(gate) {
        if(search.State is SearchState.Stimulating or SearchState.PostStimulus)
            return Results.Conflict(new { error = "Stop the active trial before changing preferences" });
        try {
            UserPreferencesStore.WriteFile(preferencesPath, input);
            preferences = input;
            if(search.State == SearchState.Ready && !search.Results.Any()) search = new QuietSearch(input.LastStimulus);
            return Results.Ok(new { saved = true, appliesToNewSearch = true });
        }
        catch(Exception ex) when(ex is ArgumentException or NotSupportedException or FormatException)
        { return Results.BadRequest(new { error=ex.Message }); }
    }
});
app.MapPost("/api/preset", (PresetInput input) =>
{
    lock(gate) {
        if(search.State is SearchState.Stimulating or SearchState.PostStimulus)
            return Results.Conflict(new { error = "Finish the trial before saving a preset" });
        try {
            var next = UserPreferencesStore.SavePreset(preferences,input.Name,DateTimeOffset.UtcNow);
            UserPreferencesStore.WriteFile(preferencesPath,next);
            preferences=next;
            return Results.Json(next.Presets.Select(p=>new {p.Name,p.CreatedUtc}));
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or FormatException)
        {return Results.BadRequest(new {error=ex.Message});}
    }
});
app.MapPost("/api/preset/load", (PresetInput input) =>
{
    lock(gate) {
        if(search.State is SearchState.Stimulating or SearchState.PostStimulus)
            return Results.Conflict(new { error = "Finish the current trial first" });
        var match=preferences.Presets.FirstOrDefault(p=>string.Equals(p.Name,input.Name,StringComparison.OrdinalIgnoreCase));
        if(match is null) return Results.NotFound(new { error = "Preset not found" });
        var next=preferences with { LastStimulus = match.Stimulus };
        UserPreferencesStore.WriteFile(preferencesPath,next);
        preferences=next;
        if(search.State==SearchState.Ready && !search.Results.Any()) search=new QuietSearch(next.LastStimulus);
        return Results.Ok(new { loaded=true, requiresFreshSearch = search.Results.Any() });
    }
});
app.MapPost("/api/reset", () =>
{
    lock(gate) {
        if(search.State is SearchState.Stimulating or SearchState.PostStimulus)
            return Results.Conflict(new { error = "Finish or stop the active trial before restarting" });
        search = new QuietSearch(preferences.LastStimulus);
        audio=Array.Empty<byte>(); currentAudioId++;
        return Results.Ok(new {ready=true, priorObservationsRetained=true});
    }
});
app.MapPost("/api/exit", () =>
{
    // Only a token-authorized local page can reach this endpoint.
    app.Lifetime.StopApplication();
    return Results.Ok(new { stopping=true });
});
app.MapPost("/api/next", (ModeInput input) =>
{
    lock (gate)
    {
        if (search.State == SearchState.Stimulating) return Results.Conflict(new { error = "Sound still playing" });
        if (search.State == SearchState.PostStimulus)
        {
            if (QuietSearch.NowSeconds() - lastStopped < 10) return Results.Conflict(new { error = "Observe at least 10 seconds after sound stops" });
            Store(search.Finish(QuietSearch.NowSeconds()));
        }
        if (!Enum.TryParse<QuietInputMode>(input.Mode, false, out var chosenMode) || !Enum.IsDefined(chosenMode))
            return Results.BadRequest(new { error = "Unknown input mode" });
        currentInputMode = chosenMode;
        SearchCandidate trial;
        try { trial = search.Begin(QuietSearch.NowSeconds()); }
        catch (InvalidOperationException) { return Results.Ok(new { finished = true, summary = search.Results.Select(r => new { r.Candidate.FrequencyHz, r.AfterQuietSeconds, r.DuringQuietSeconds }) }); }
        audio = MakeWave(trial.Settings);
        currentAudioId++;
        return Results.Ok(new { finished = false, trial = trial.Number, hz = trial.FrequencyHz, control = trial.RepeatedControl,
            stage = trial.Stage, source = "/api/audio/" + currentAudioId });
    }
});
app.MapGet("/api/audio/{id:int}", (int id) =>
{
    lock (gate)
    {
        if (search.State != SearchState.Stimulating || id != currentAudioId) return Results.NotFound();
        return Results.File(audio, "audio/wav", enableRangeProcessing: false);
    }
});
app.MapPost("/api/hold", (HoldInput input) =>
{
    lock (gate) { search.MarkQuiet(input.Down, QuietSearch.NowSeconds()); return Results.Ok(); }
});
app.MapPost("/api/ended", () =>
{
    lock (gate) { search.SoundStopped(lastStopped = QuietSearch.NowSeconds()); return Results.Ok(); }
});
app.MapPost("/api/annotate", (Annotation input) =>
{
    lock (gate) { search.Annotate(input.Note, input.LoudnessAfter); return Results.Ok(); }
});
app.MapPost("/api/interrupt", () =>
{
    lock (gate)
    {
        if (search.State is SearchState.Stimulating or SearchState.PostStimulus)
            Store(search.Finish(QuietSearch.NowSeconds(), interrupted: true));
        return Results.Ok();
    }
});
app.MapGet("/api/export", () =>
{
    lock (gate) return Results.Text(File.Exists(logPath) ? File.ReadAllText(logPath) : "No observations yet", "text/plain");
});
await app.StartAsync();
var addresses=app.Services.GetRequiredService<IServer>().Features
    .Get<IServerAddressesFeature>()?.Addresses;
var assigned=addresses?.FirstOrDefault(a=>a.StartsWith("http://127.0.0.1:",StringComparison.Ordinal));
if(assigned is null || !Uri.TryCreate(assigned,UriKind.Absolute,out var boundUrl) || boundUrl.Port==0)
    throw new InvalidOperationException("Kestrel did not provide the actual loopback address.");
Console.WriteLine("Aural Observatory local session: " + boundUrl.AbsoluteUri);
Console.WriteLine("Local user data: " + dataDir);
if(DesktopSession.ShouldLaunchBrowser(args,Environment.GetEnvironmentVariable))
    DesktopSession.TryLaunchBrowser(boundUrl,Console.WriteLine);
else Console.WriteLine("Browser launch disabled/unavailable; open the URL above manually.");
await app.WaitForShutdownAsync();

void Store(SearchResult result)
{
    var text = string.Join(",", DateTimeOffset.UtcNow.ToString("O"), result.Candidate.Number,
        result.Candidate.FrequencyHz.ToString("F2", CultureInfo.InvariantCulture), result.Candidate.Stage,
        result.Candidate.RepeatedControl,
        result.DuringQuietSeconds.ToString("F3", CultureInfo.InvariantCulture),
        result.AfterQuietSeconds.ToString("F3", CultureInfo.InvariantCulture), result.Interrupted, currentInputMode,
        result.LoudnessAfter?.ToString(CultureInfo.InvariantCulture) ?? "",
        Csv(result.Note ?? "")) + "\n";
    if (!File.Exists(logPath)) File.WriteAllText(logPath,
        "timestamp_utc,trial,hz,stage,control,during_quiet_s,after_quiet_s,interrupted,input_mode,loudness_after,note\n");
    File.AppendAllText(logPath, text);
}
static string Csv(string value) => "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
static byte[] MakeWave(StimulusSettings settings)
{
    const int frames = 15 * StimulusValidation.SampleRate;
    var engine = new StimulusEngine(settings);
    using var memory = new MemoryStream(44 + frames * 4);
    using var writer = new BinaryWriter(memory, Encoding.ASCII, leaveOpen: true);
    writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + frames * 4);
    writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
    writer.Write((short)2); writer.Write(StimulusValidation.SampleRate); writer.Write(StimulusValidation.SampleRate * 4);
    writer.Write((short)4); writer.Write((short)16);
    writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(frames * 4);
    var buffer = new short[2048];
    for (int pos = 0; pos < frames; pos += 1024)
    {
        var count = Math.Min(1024, frames - pos);
        engine.Render(buffer, count);
        for (int i = 0; i < count; i++)
        {
            var fade = Math.Min(1.0, (frames - pos - i) / (0.1 * StimulusValidation.SampleRate));
            writer.Write((short)Math.Round(buffer[i * 2] * fade));
            writer.Write((short)Math.Round(buffer[i * 2 + 1] * fade));
        }
    }
    return memory.ToArray();
}
record PresetInput(string Name);
record ModeInput(string Mode);
record HoldInput(bool Down);
record Annotation(string? Note, double? LoudnessAfter);

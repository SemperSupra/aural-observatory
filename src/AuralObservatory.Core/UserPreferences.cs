using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AuralObservatory.Core;

public enum AppearanceMode { System, Light, Dark }
public enum FontFamilyChoice { System, SansSerif, Serif, Monospace }

/// <summary>A named configuration snapshot, not a clinical treatment recommendation.</summary>
public sealed record AudioPreset(string Name, StimulusSettings Stimulus, DateTimeOffset CreatedUtc);

/// <summary>
/// Local-first, migratable document. No biometric measurements or credentials are permitted here.
/// Persisted under OS user-owned data, independently of executable/package installation.
/// </summary>
public sealed record UserPreferences(
    int SchemaVersion,
    AppearanceMode Appearance,
    FontFamilyChoice FontFamily,
    int FontScalePercent,
    bool HighContrast,
    bool ReduceMotion,
    QuietInputMode ResponseMode,
    StimulusSettings LastStimulus,
    List<AudioPreset> Presets)
{
    public static UserPreferences Default => new(1, AppearanceMode.System, FontFamilyChoice.System,
        100, false, false, QuietInputMode.HoldWhenQuiet,
        new StimulusSettings(new EarProfile(4000, .6),new EarProfile(6000, .6)),
        new List<AudioPreset>());
}

public static class UserPreferencesStore
{
    public const int CurrentSchema = 1;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static UserPreferences FromJson(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var parsed = JsonSerializer.Deserialize<UserPreferences>(text, Options)
            ?? throw new FormatException("Preferences document is empty");
        Validate(parsed);
        return parsed;
    }
    public static string ToJson(UserPreferences preferences)
    {
        Validate(preferences);
        return JsonSerializer.Serialize(preferences, Options);
    }
    public static UserPreferences ReadFile(string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        return File.Exists(file) ? FromJson(File.ReadAllText(file)) : UserPreferences.Default;
    }

    /// <summary>
    /// Create-only temp file then same-directory replacement. A corrupt existing document is never
    /// overwritten by automatic recovery; UI must offer explicit repair/export.
    /// </summary>
    public static void WriteFile(string file, UserPreferences value)
    {
        var text = ToJson(value);
        var folder = Path.GetDirectoryName(Path.GetFullPath(file)) ?? throw new IOException("Missing profile directory");
        Directory.CreateDirectory(folder);
        var temp = Path.Combine(folder, ".aural-preferences-" + Guid.NewGuid().ToString("N") + ".tmp");
        try {
            File.WriteAllText(temp, text);
            File.Move(temp, file, overwrite: true);
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }

    public static UserPreferences SavePreset(UserPreferences prefs, string name, DateTimeOffset now)
    {
        Validate(prefs);
        name = (name ?? "").Trim();
        if (name.Length is < 1 or > 64 || name.Any(char.IsControl))
            throw new ArgumentException("Preset name must be 1–64 printable characters", nameof(name));
        if(prefs.Presets.Count >= 50 && !prefs.Presets.Any(x=>string.Equals(x.Name,name,StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Maximum 50 presets");
        var copy = prefs.Presets.Where(x=>!string.Equals(x.Name,name,StringComparison.OrdinalIgnoreCase)).ToList();
        copy.Add(new AudioPreset(name,prefs.LastStimulus,now));
        return prefs with { Presets=copy };
    }
    public static void Validate(UserPreferences p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if(p.SchemaVersion != CurrentSchema) throw new NotSupportedException(
            $"Preferences schema {p.SchemaVersion} requires a migration; current is {CurrentSchema}");
        if(!Enum.IsDefined(p.Appearance) || !Enum.IsDefined(p.FontFamily) || !Enum.IsDefined(p.ResponseMode))
            throw new ArgumentOutOfRangeException(nameof(p),"Unknown appearance, font, or input mode");
        if(p.FontScalePercent is < 80 or > 200) throw new ArgumentOutOfRangeException(nameof(p.FontScalePercent));
        StimulusValidation.Validate(p.LastStimulus);
        if(p.Presets is null || p.Presets.Count>50) throw new ArgumentOutOfRangeException(nameof(p.Presets));
        foreach(var preset in p.Presets) {
            if(preset is null || string.IsNullOrWhiteSpace(preset.Name) || preset.Name.Length > 64 ||
               preset.Name.Any(char.IsControl)) throw new FormatException("Invalid preset name");
            StimulusValidation.Validate(preset.Stimulus);
        }
        if(p.Presets.Select(x=>x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=p.Presets.Count)
            throw new FormatException("Duplicate preset names");
    }
}

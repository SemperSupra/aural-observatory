using AuralObservatory.DataSources;
using System.Text.Json;

if (args.Length != 3 || !Enum.TryParse<GarminDataset>(args[0], true, out var dataset) ||
    !DateOnly.TryParseExact(args[1], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None, out var date))
{
    Console.Error.WriteLine("Usage: dotnet run --project src/AuralObservatory.GarminConsole -- <Hrv|HeartRate|Sleep|Stress|BodyBattery|DailySummary> <YYYY-MM-DD> <LOCAL-OUTPUT.json>");
    return 2;
}
var destination = Path.GetFullPath(args[2]);
if (File.Exists(destination)) { Console.Error.WriteLine("Refusing to overwrite existing observation file."); return 3; }
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GARMIN_PERSONAL_API_KEY")))
{
    Console.Error.WriteLine("Set GARMIN_PERSONAL_API_KEY securely in the local process environment (never commit it).");
    return 4;
}
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
var dataSource = new GarminPersonalSource(http, _ => Task.FromResult(Environment.GetEnvironmentVariable("GARMIN_PERSONAL_API_KEY") ?? ""));
try
{
    var snapshot = await dataSource.GetDayAsync(dataset, date);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    await using var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    await JsonSerializer.SerializeAsync(file, snapshot, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine("Saved one personal Garmin data snapshot to selected local path. No telemetry sent to Aural Observatory.");
    return 0;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine("Garmin API HTTP request failed (check credentials and endpoint): " + ex.StatusCode);
    return 5;
}

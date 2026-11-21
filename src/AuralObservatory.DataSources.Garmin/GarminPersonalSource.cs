using System.Net.Http.Headers;
using System.Text.Json;

namespace AuralObservatory.DataSources;

public enum GarminDataset { HeartRate, Hrv, Sleep, Stress, BodyBattery, DailySummary }
public sealed record HistoricalDataSnapshot(string Provider, GarminDataset Dataset, DateOnly Date,
    DateTimeOffset RetrievedAtUtc, JsonElement OriginalResponse);

/// <summary>Read-only personal Garmin Connect history; NOT live physiological measurements.</summary>
public interface IHistoricalDataSource
{
    string SourceId { get; }
    Task<HistoricalDataSnapshot> GetDayAsync(GarminDataset dataset, DateOnly date, CancellationToken cancellationToken = default);
}

/// <summary>
/// Personal-only Garmin API. Key is passed by caller from local secret store and never logged or persisted here.
/// Caller owns HttpClient; no SDK redistribution and no multiuser authentication assumptions.
/// </summary>
public sealed class GarminPersonalSource(HttpClient client, Func<CancellationToken, Task<string>> apiKeyProvider) : IHistoricalDataSource
{
    public string SourceId => "garmin-connect-personal-v1";
    private const string Root = "https://personal.connectapi.garmin.com";
    private const string TokenPath = "/personalapitoken-service/personal-api/personal-api-key/token";

    public async Task<HistoricalDataSnapshot> GetDayAsync(GarminDataset dataset, DateOnly date, CancellationToken ct = default)
    {
        var key = await apiKeyProvider(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Personal API key not configured.");
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, Root + TokenPath);
        tokenRequest.Headers.TryAddWithoutValidation("Authorization", "ApiKey " + key.Trim());
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        using var tokenResponse = await client.SendAsync(tokenRequest, ct).ConfigureAwait(false);
        tokenResponse.EnsureSuccessStatusCode();
        var jwt = (await tokenResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false)).Trim();
        if (jwt.Length == 0 || jwt.Length > 8192) throw new InvalidOperationException("Garmin returned an invalid bearer token.");
        var d = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var endpoint = dataset switch
        {
            GarminDataset.HeartRate => $"/wellness-service/wellness/dailyHeartRate?date={d}",
            GarminDataset.Hrv => $"/hrv-service/hrv/{d}",
            GarminDataset.Sleep => $"/sleep-service/sleep/dailySleep?date={d}",
            GarminDataset.Stress => $"/usersummary-service/stats/stress/daily/{d}/{d}",
            GarminDataset.BodyBattery => $"/wellness-service/wellness/bodyBattery/reports/daily?startDate={d}&endDate={d}",
            GarminDataset.DailySummary => $"/usersummary-service/v2/dailysummaries?startDate={d}&endDate={d}",
            _ => throw new ArgumentOutOfRangeException(nameof(dataset))
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, Root + endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        return new HistoricalDataSnapshot(SourceId, dataset, date, DateTimeOffset.UtcNow, json.RootElement.Clone());
    }
}

using AuralObservatory.DataSources;
using System.Net;
using System.Net.Http;

var seen = new List<string>();
var handler = new FakeHttp(req =>
{
    var url = req.RequestUri!.ToString();
    seen.Add($"{req.Method} {url}");
    if (req.Method == HttpMethod.Post)
    {
        if (req.Headers.GetValues("Authorization").Single() != "ApiKey synthetic-key") throw new Exception("Wrong API key header");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("synthetic.jwt.test") };
    }
    if (req.Headers.Authorization?.ToString() != "Bearer synthetic.jwt.test") throw new Exception("Wrong bearer auth");
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"test\":true}") };
});
using var http = new HttpClient(handler);
var source = new GarminPersonalSource(http, _ => Task.FromResult("synthetic-key"));
var date = new DateOnly(2026, 11, 21);
foreach (var ds in Enum.GetValues<GarminDataset>())
{
    var snap = await source.GetDayAsync(ds, date);
    if (snap.Dataset != ds || !snap.OriginalResponse.GetProperty("test").GetBoolean()) throw new Exception("Data source failed");
}
if (seen.Count != 12 || seen.Any(x => x.Contains("synthetic-key"))) throw new Exception("HTTP calls not as expected");
Console.WriteLine("PASS Garmin personal API client: 6 datasets, auth exchange, and no network in synthetic tests");
internal sealed class FakeHttp(Func<HttpRequestMessage,HttpResponseMessage> callback) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(callback(request));
}

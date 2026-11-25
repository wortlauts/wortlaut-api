using System.Net;
using System.Text.Json;

namespace Wortlaut.Api.Tests.Integration;

internal sealed record ApiResponse(HttpStatusCode Status, JsonElement Body)
{
    public static async Task<ApiResponse> GetAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var body = text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
        return new ApiResponse(response.StatusCode, body);
    }

    public IReadOnlyList<string> HitIds() =>
        Body.GetProperty("hits").EnumerateArray().Select(hit => hit.GetProperty("id").GetString()!).ToList();

    public IReadOnlyDictionary<string, long> Facet(string name) =>
        Body.GetProperty("facets").GetProperty(name).EnumerateArray()
            .ToDictionary(facet => facet.GetProperty("value").GetString()!, facet => facet.GetProperty("count").GetInt64());
}

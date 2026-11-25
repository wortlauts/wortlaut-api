using System.Net;

namespace Wortlaut.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class TrendEndpointTests(ApiFixture api)
{
    [Fact]
    public async Task PerYear_CountsMatchingAndAllSpeechesIncludingEmptyYears()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/trends?q=Digitalisierung&bucket=year");

        Assert.Equal("year", response.Body.GetProperty("bucket").GetString());
        Assert.Equal(
            [("2022", 1L, 1L), ("2023", 0L, 0L), ("2024", 0L, 0L), ("2025", 2L, 2L), ("2026", 0L, 3L)],
            Points(response).Select(point => (point.Key, point.Count, point.Total)));
    }

    [Fact]
    public async Task PerLegislativePeriod_ListsThePeriodsInOrder()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/trends?q=Digitalisierung&bucket=legislative_period");

        Assert.Equal("legislative_period", response.Body.GetProperty("bucket").GetString());
        Assert.Equal(
            [("20", 1L, 1L), ("21", 2L, 5L)],
            Points(response).Select(point => (point.Key, point.Count, point.Total)));
    }

    [Fact]
    public async Task Normalised_ReportsTheShareOfSpeechesContainingTheTerm()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/trends?q=Digitalisierung&bucket=year&normalize=true");

        Assert.True(response.Body.GetProperty("normalized").GetBoolean());
        var ratios = Points(response).ToDictionary(point => point.Key, point => point.Ratio);
        Assert.Equal(1.0, ratios["2025"]);
        Assert.Equal(0.0, ratios["2026"]);
        Assert.Equal(0.0, ratios["2023"]);
    }

    [Fact]
    public async Task WithoutNormalisation_NoRatioIsReported()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/trends?q=Digitalisierung");

        Assert.All(Points(response), point => Assert.Null(point.Ratio));
    }

    [Fact]
    public async Task FactionFilter_NarrowsTheTotals()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/trends?q=Digitalisierung&faction=AfD");

        Assert.Equal(
            [("2025", 1L, 1L)],
            Points(response).Where(point => point.Total > 0).Select(point => (point.Key, point.Count, point.Total)));
    }

    [Fact]
    public async Task MissingTerm_IsAValidationProblem()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/trends");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.True(response.Body.GetProperty("errors").TryGetProperty("q", out _));
    }

    [Fact]
    public async Task UnknownBucket_IsAValidationProblem()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/trends?q=Wort&bucket=decade");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.True(response.Body.GetProperty("errors").TryGetProperty("bucket", out _));
    }

    private static List<(string Key, long Count, long Total, double? Ratio)> Points(ApiResponse response) =>
        response.Body.GetProperty("points").EnumerateArray()
            .Select(point => (
                point.GetProperty("key").GetString()!,
                point.GetProperty("count").GetInt64(),
                point.GetProperty("total").GetInt64(),
                point.TryGetProperty("ratio", out var ratio) ? ratio.GetDouble() : (double?)null))
            .ToList();
}

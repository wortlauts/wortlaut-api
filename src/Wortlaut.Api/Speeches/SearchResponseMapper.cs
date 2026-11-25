using System.Text.Json;
using Wortlaut.Api.Http;

namespace Wortlaut.Api.Speeches;

internal static class SearchResponseMapper
{
    public static SearchResponse Map(JsonElement result, SearchRequest request)
    {
        var hits = result.GetProperty("hits");
        var aggregations = result.GetProperty("aggregations");

        return new SearchResponse(
            Total: hits.GetProperty("total").GetProperty("value").GetInt64(),
            Page: request.Page,
            PageSize: request.PageSize,
            Hits: hits.GetProperty("hits").EnumerateArray().Select(MapHit).ToList(),
            Facets: new Facets(
                Faction: FacetValues(aggregations.GetProperty("faction")),
                LegislativePeriod: FacetValues(aggregations.GetProperty("legislative_period")),
                Reaction: FacetValues(aggregations.GetProperty("reaction").GetProperty("types"))));
    }

    public static IReadOnlyList<FacetValue> FacetValues(JsonElement termsAggregation) =>
        termsAggregation.GetProperty("buckets").EnumerateArray()
            .Select(bucket => new FacetValue(KeyOf(bucket), bucket.GetProperty("doc_count").GetInt64()))
            .ToList();

    public static string KeyOf(JsonElement bucket)
    {
        var key = bucket.GetProperty("key");
        return key.ValueKind == JsonValueKind.String ? key.GetString()! : key.GetRawText();
    }

    private static SearchHit MapHit(JsonElement hit)
    {
        var speech = hit.GetProperty("_source").Deserialize<SearchHit>(ApiJson.Options)!;
        return speech with { Highlights = HighlightsOf(hit) };
    }

    private static List<string> HighlightsOf(JsonElement hit) =>
        hit.TryGetProperty("highlight", out var highlight)
            ? highlight.EnumerateObject()
                .SelectMany(field => field.Value.EnumerateArray())
                .Select(fragment => fragment.GetString()!)
                .ToList()
            : [];
}

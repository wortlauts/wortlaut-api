using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Trends;

internal static class TrendQueryBuilder
{
    private const int MaxLegislativePeriods = 50;

    public static object Build(TrendRequest request)
    {
        var buckets = BucketAggregation(request.Bucket);
        buckets["aggs"] = new { matching = new { filter = TextQuery.Build(request.Query, request.Mode) } };

        return new
        {
            size = 0,
            query = new { @bool = new { filter = FactionFilter(request.Faction) } },
            aggs = new { buckets },
        };
    }

    private static Dictionary<string, object> BucketAggregation(TrendBucket bucket) => bucket switch
    {
        TrendBucket.LegislativePeriod => new()
        {
            ["terms"] = new { field = "legislative_period", size = MaxLegislativePeriods, order = new { _key = "asc" } },
        },
        _ => new()
        {
            ["date_histogram"] = new { field = "date", calendar_interval = "year", format = "yyyy", min_doc_count = 0 },
        },
    };

    private static object[] FactionFilter(string? faction) =>
        faction is null ? [] : [new { term = TextQuery.Field("speaker.faction", faction) }];
}

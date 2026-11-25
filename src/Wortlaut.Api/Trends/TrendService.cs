using System.Text.Json;
using Wortlaut.Api.Elasticsearch;
using Wortlaut.Api.Http;
using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Trends;

public sealed class TrendService(ISpeechIndex index)
{
    public async Task<TrendResponse> GetAsync(TrendRequest request, CancellationToken cancellationToken)
    {
        var result = await index.SearchAsync(TrendQueryBuilder.Build(request), cancellationToken);

        var points = result.GetProperty("aggregations").GetProperty("buckets").GetProperty("buckets").EnumerateArray()
            .Select(bucket => ToPoint(bucket, request))
            .ToList();

        var bucketName = JsonNamingPolicy.SnakeCaseLower.ConvertName(request.Bucket.ToString());
        return new TrendResponse(bucketName, request.Normalize, points);
    }

    private static TrendPoint ToPoint(JsonElement bucket, TrendRequest request)
    {
        var total = bucket.GetProperty("doc_count").GetInt64();
        var count = bucket.GetProperty("matching").GetProperty("doc_count").GetInt64();
        double? ratio = request.Normalize ? (total == 0 ? 0 : (double)count / total) : null;

        return new TrendPoint(KeyOf(bucket, request.Bucket), count, total, ratio);
    }

    private static string KeyOf(JsonElement bucket, TrendBucket kind) =>
        kind == TrendBucket.Year
            ? bucket.GetProperty("key_as_string").GetString()!
            : SearchResponseMapper.KeyOf(bucket);
}

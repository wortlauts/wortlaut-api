using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Trends;

public sealed record TrendRequest(string Query, SearchMode Mode, TrendBucket Bucket, bool Normalize, string? Faction);

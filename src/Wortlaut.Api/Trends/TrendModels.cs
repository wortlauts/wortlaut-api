namespace Wortlaut.Api.Trends;

public sealed record TrendPoint(string Key, long Count, long Total, double? Ratio);

public sealed record TrendResponse(string Bucket, bool Normalized, IReadOnlyList<TrendPoint> Points);

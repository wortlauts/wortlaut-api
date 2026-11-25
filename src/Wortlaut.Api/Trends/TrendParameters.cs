using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Wortlaut.Api.Http;
using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Trends;

public sealed record TrendParameters(
    [FromQuery(Name = "q")] string? Query,
    [FromQuery(Name = "mode")] string? Mode,
    [FromQuery(Name = "bucket")] string? Bucket,
    [FromQuery(Name = "normalize")] bool? Normalize,
    [FromQuery(Name = "faction")] string? Faction)
{
    public bool TryCreateRequest(
        [NotNullWhen(true)] out TrendRequest? request,
        out IDictionary<string, string[]> errors)
    {
        var parameterErrors = new ParameterErrors();

        var mode = parameterErrors.ParseEnum("mode", Mode, SearchMode.Stem);
        var bucket = parameterErrors.ParseEnum("bucket", Bucket, TrendBucket.Year);
        if (string.IsNullOrWhiteSpace(Query))
        {
            parameterErrors.Add("q", "A search term is required.");
        }

        errors = parameterErrors.ToDictionary();
        request = parameterErrors.Any
            ? null
            : new TrendRequest(
                Query!.Trim(), mode, bucket, Normalize ?? false, string.IsNullOrWhiteSpace(Faction) ? null : Faction.Trim());
        return request is not null;
    }
}

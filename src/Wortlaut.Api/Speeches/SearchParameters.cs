using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Wortlaut.Api.Http;

namespace Wortlaut.Api.Speeches;

public sealed record SearchParameters(
    [FromQuery(Name = "q")] string? Query,
    [FromQuery(Name = "mode")] string? Mode,
    [FromQuery(Name = "from")] DateOnly? From,
    [FromQuery(Name = "to")] DateOnly? To,
    [FromQuery(Name = "faction")] string? Faction,
    [FromQuery(Name = "speaker")] string? Speaker,
    [FromQuery(Name = "legislative_period")] int? LegislativePeriod,
    [FromQuery(Name = "reaction")] string? Reaction,
    [FromQuery(Name = "sort")] string? Sort,
    [FromQuery(Name = "page")] int? Page,
    [FromQuery(Name = "page_size")] int? PageSize)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private const int MaxResultWindow = 10_000;

    public bool TryCreateRequest(
        [NotNullWhen(true)] out SearchRequest? request,
        out IDictionary<string, string[]> errors)
    {
        var parameterErrors = new ParameterErrors();

        var mode = parameterErrors.ParseEnum("mode", Mode, SearchMode.Stem);
        var sort = parameterErrors.ParseEnum("sort", Sort, SearchSort.Relevance);
        var reaction = string.IsNullOrWhiteSpace(Reaction)
            ? (ReactionType?)null
            : parameterErrors.ParseEnum("reaction", Reaction, ReactionType.Applause);
        var page = parameterErrors.ParseRange("page", Page, fallback: 1, min: 1, max: MaxResultWindow);
        var pageSize = parameterErrors.ParseRange("page_size", PageSize, DefaultPageSize, min: 1, MaxPageSize);

        if (page * pageSize > MaxResultWindow)
        {
            parameterErrors.Add("page", $"Results beyond the first {MaxResultWindow} hits cannot be paged.");
        }

        if (From > To)
        {
            parameterErrors.Add("from", "Must not be after 'to'.");
        }

        errors = parameterErrors.ToDictionary();
        request = parameterErrors.Any
            ? null
            : new SearchRequest(
                NullIfBlank(Query), mode, From, To, NullIfBlank(Faction), NullIfBlank(Speaker),
                LegislativePeriod, reaction, sort, page, pageSize);
        return request is not null;
    }

    private static string? NullIfBlank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

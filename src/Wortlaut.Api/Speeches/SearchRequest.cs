namespace Wortlaut.Api.Speeches;

public sealed record SearchRequest(
    string? Query,
    SearchMode Mode,
    DateOnly? From,
    DateOnly? To,
    string? Faction,
    string? Speaker,
    int? LegislativePeriod,
    ReactionType? Reaction,
    SearchSort Sort,
    int Page,
    int PageSize);

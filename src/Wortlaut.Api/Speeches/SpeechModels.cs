namespace Wortlaut.Api.Speeches;

public sealed record SpeakerInfo(string Id, string Name, string? Faction, string? Role);

public sealed record ReactionInfo(string Type, string? Source, string? Text);

public sealed record RightsInfo(string License, string Attribution, IReadOnlyList<string> AllowedActions);

public sealed record SpeechDetail(
    string Id,
    int LegislativePeriod,
    int Session,
    DateOnly Date,
    string? AgendaItem,
    string Delivery,
    SpeakerInfo Speaker,
    string Text,
    IReadOnlyList<ReactionInfo> Reactions,
    int WordCount,
    string SourceUrl,
    RightsInfo Rights);

public sealed record SearchHit
{
    public required string Id { get; init; }

    public required int LegislativePeriod { get; init; }

    public required int Session { get; init; }

    public required DateOnly Date { get; init; }

    public string? AgendaItem { get; init; }

    public required string Delivery { get; init; }

    public required SpeakerInfo Speaker { get; init; }

    public required int WordCount { get; init; }

    public required string SourceUrl { get; init; }

    public IReadOnlyList<string> Highlights { get; init; } = [];
}

public sealed record FacetValue(string Value, long Count);

public sealed record Facets(
    IReadOnlyList<FacetValue> Faction,
    IReadOnlyList<FacetValue> LegislativePeriod,
    IReadOnlyList<FacetValue> Reaction);

public sealed record SearchResponse(
    long Total,
    int Page,
    int PageSize,
    IReadOnlyList<SearchHit> Hits,
    Facets Facets);

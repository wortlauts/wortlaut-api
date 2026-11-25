using System.Text.Json;
using System.Text.Json.Nodes;
using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Tests.Speeches;

public class SearchQueryBuilderTests
{
    [Fact]
    public void StemMode_UsesTheStemmedFieldWithAllWordsRequired()
    {
        var query = Build(Request(query: "Digitalisierung der Verwaltung", mode: SearchMode.Stem));

        Assert.Equal("and", query["query"]!["bool"]!["must"]![0]!["match"]!["text"]!["operator"]!.GetValue<string>());
        Assert.Equal("Digitalisierung der Verwaltung", query["query"]!["bool"]!["must"]![0]!["match"]!["text"]!["query"]!.GetValue<string>());
    }

    [Fact]
    public void PhraseMode_MatchesThePhraseOnTheExactField()
    {
        var query = Build(Request(query: "soziale Marktwirtschaft", mode: SearchMode.Phrase));

        Assert.Equal(
            "soziale Marktwirtschaft",
            query["query"]!["bool"]!["must"]![0]!["match_phrase"]!["text.exact"]!.GetValue<string>());
    }

    [Fact]
    public void FuzzyMode_AllowsTyposOnTheExactField()
    {
        var query = Build(Request(query: "Digitalisirung", mode: SearchMode.Fuzzy));

        Assert.Equal(
            "AUTO",
            query["query"]!["bool"]!["must"]![0]!["match"]!["text.exact"]!["fuzziness"]!.GetValue<string>());
    }

    [Fact]
    public void WithoutQuery_EverythingMatchesAndNothingIsHighlighted()
    {
        var query = Build(Request(query: null));

        Assert.NotNull(query["query"]!["bool"]!["must"]![0]!["match_all"]);
        Assert.Null(query["highlight"]);
    }

    [Theory]
    [InlineData(SearchMode.Stem, "text")]
    [InlineData(SearchMode.Phrase, "text.exact")]
    [InlineData(SearchMode.Fuzzy, "text.exact")]
    public void Highlighting_UsesTheFieldThatWasSearched(SearchMode mode, string expectedField)
    {
        var query = Build(Request(query: "Wort", mode: mode));

        var field = Assert.Single(query["highlight"]!["fields"]!.AsObject());
        Assert.Equal(expectedField, field.Key);
        Assert.Equal("html", query["highlight"]!["encoder"]!.GetValue<string>());
    }

    [Fact]
    public void Filters_AreCombinedInTheFilterClause()
    {
        var query = Build(Request(
            faction: "SPD", speaker: "Muster", legislativePeriod: 21, reaction: ReactionType.Heckle,
            from: new DateOnly(2026, 1, 1), to: new DateOnly(2026, 12, 31)));

        var filters = query["query"]!["bool"]!["filter"]!.AsArray().Select(filter => filter!.ToJsonString()).ToList();

        Assert.Contains("""{"term":{"speaker.faction":"SPD"}}""", filters);
        Assert.Contains("""{"match_phrase":{"speaker.name":"Muster"}}""", filters);
        Assert.Contains("""{"term":{"legislative_period":21}}""", filters);
        Assert.Contains("""{"range":{"date":{"gte":"2026-01-01","lte":"2026-12-31"}}}""", filters);
        Assert.Contains("""{"nested":{"path":"reactions","query":{"term":{"reactions.type":"heckle"}}}}""", filters);
    }

    [Fact]
    public void WithoutFilters_TheFilterClauseIsEmpty()
    {
        Assert.Empty(Build(Request())["query"]!["bool"]!["filter"]!.AsArray());
    }

    [Fact]
    public void Paging_IsTranslatedIntoFromAndSize()
    {
        var query = Build(Request(page: 3, pageSize: 10));

        Assert.Equal(20, query["from"]!.GetValue<int>());
        Assert.Equal(10, query["size"]!.GetValue<int>());
    }

    [Fact]
    public void RelevanceSort_StartsWithTheScore()
    {
        var sort = Build(Request(sort: SearchSort.Relevance))["sort"]!.AsArray();

        Assert.Equal("_score", sort[0]!.GetValue<string>());
    }

    [Fact]
    public void DateSort_OrdersNewestFirst()
    {
        var sort = Build(Request(sort: SearchSort.Date))["sort"]!.AsArray();

        Assert.Equal("desc", sort[0]!["date"]!["order"]!.GetValue<string>());
    }

    [Fact]
    public void FullTextAndReactionsAreNotReturnedInTheHitList()
    {
        var excluded = Build(Request())["_source"]!["excludes"]!.AsArray().Select(field => field!.GetValue<string>());

        Assert.Contains("text", excluded);
        Assert.Contains("reactions", excluded);
    }

    [Fact]
    public void FacetsAreRequestedForFactionPeriodAndReaction()
    {
        var aggregations = Build(Request())["aggs"]!.AsObject().Select(aggregation => aggregation.Key);

        Assert.Equal(["faction", "legislative_period", "reaction"], aggregations.Order());
    }

    private static JsonNode Build(SearchRequest request) =>
        JsonSerializer.SerializeToNode(SearchQueryBuilder.Build(request))!;

    private static SearchRequest Request(
        string? query = null,
        SearchMode mode = SearchMode.Stem,
        DateOnly? from = null,
        DateOnly? to = null,
        string? faction = null,
        string? speaker = null,
        int? legislativePeriod = null,
        ReactionType? reaction = null,
        SearchSort sort = SearchSort.Relevance,
        int page = 1,
        int pageSize = 20) =>
        new(query, mode, from, to, faction, speaker, legislativePeriod, reaction, sort, page, pageSize);
}

using System.Net;

namespace Wortlaut.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SearchEndpointTests(ApiFixture api)
{
    [Fact]
    public async Task StemMode_FindsOtherInflectionsOfTheWord()
    {
        var response = await Search("q=Verwaltung&mode=stem");

        Assert.Equal(["21-10-00100", "21-10-00200"], response.HitIds().Order());
    }

    [Fact]
    public async Task PhraseMode_OnlyFindsTheExactWords()
    {
        var response = await Search("q=Verwaltung&mode=phrase");

        Assert.Equal(["21-10-00200"], response.HitIds());
    }

    [Fact]
    public async Task PhraseMode_FindsAWordSequence()
    {
        var response = await Search("q=soziale%20Marktwirtschaft&mode=phrase");

        Assert.Equal(["21-10-00100", "21-20-00100"], response.HitIds().Order());
    }

    [Fact]
    public async Task FuzzyMode_ToleratesATypo()
    {
        var response = await Search("q=Digitalisirung&mode=fuzzy");

        Assert.Equal(3, response.Body.GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task Hits_CarryHighlightedFragments()
    {
        var response = await Search("q=Digitalisierung");

        var highlights = response.Body.GetProperty("hits")[0].GetProperty("highlights");
        Assert.Contains("<mark>Digitalisierung</mark>", highlights[0].GetString());
    }

    [Fact]
    public async Task Hits_DoNotCarryTheFullText()
    {
        var response = await Search("q=Digitalisierung");

        Assert.False(response.Body.GetProperty("hits")[0].TryGetProperty("text", out _));
    }

    [Fact]
    public async Task WithoutQuery_AllSpeechesAreListed()
    {
        var response = await Search(string.Empty);

        Assert.Equal(6, response.Body.GetProperty("total").GetInt64());
    }

    [Theory]
    [InlineData("faction=SPD", 2)]
    [InlineData("legislative_period=20", 1)]
    [InlineData("from=2026-01-01", 3)]
    [InlineData("to=2025-12-31", 3)]
    [InlineData("from=2025-11-14&to=2025-11-14", 2)]
    [InlineData("speaker=Muster", 2)]
    [InlineData("reaction=heckle", 1)]
    [InlineData("reaction=laughter", 1)]
    [InlineData("reaction=objection", 1)]
    [InlineData("faction=SPD&legislative_period=21", 1)]
    public async Task Filters_NarrowTheResult(string filter, int expectedTotal)
    {
        var response = await Search(filter);

        Assert.Equal(expectedTotal, response.Body.GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task Facets_CountTheMatchingSpeeches()
    {
        var response = await Search(string.Empty);

        Assert.Equal(
            new Dictionary<string, long> { ["SPD"] = 2, ["AfD"] = 1, ["CDU/CSU"] = 1, ["Die Linke"] = 1 },
            response.Facet("faction"));
        Assert.Equal(new Dictionary<string, long> { ["21"] = 5, ["20"] = 1 }, response.Facet("legislative_period"));
        Assert.Equal(
            new Dictionary<string, long> { ["applause"] = 3, ["heckle"] = 1, ["laughter"] = 1, ["objection"] = 1 },
            response.Facet("reaction"));
    }

    [Fact]
    public async Task DateSort_ListsTheNewestSpeechesFirst()
    {
        var response = await Search("sort=date");

        Assert.Equal("2026-03-05", response.Body.GetProperty("hits")[0].GetProperty("date").GetString());
        Assert.Equal("2022-06-01", response.Body.GetProperty("hits")[5].GetProperty("date").GetString());
    }

    [Fact]
    public async Task Paging_ReturnsTheRequestedPageAndTheTotal()
    {
        var response = await Search("sort=date&page=2&page_size=4");

        Assert.Equal(6, response.Body.GetProperty("total").GetInt64());
        Assert.Equal(2, response.Body.GetProperty("page").GetInt32());
        Assert.Equal(2, response.HitIds().Count);
    }

    [Fact]
    public async Task SpeakerWithoutFaction_IsReturnedWithoutOne()
    {
        var response = await Search("speaker=Minister");

        var speaker = response.Body.GetProperty("hits")[0].GetProperty("speaker");
        Assert.False(speaker.TryGetProperty("faction", out _));
        Assert.Equal("Bundesminister BMF", speaker.GetProperty("role").GetString());
    }

    [Theory]
    [InlineData("mode=nope", "mode")]
    [InlineData("page_size=0", "page_size")]
    [InlineData("page=0", "page")]
    [InlineData("from=2026-06-01&to=2026-01-01", "from")]
    [InlineData("reaction=cheers", "reaction")]
    public async Task InvalidParameters_AreAnsweredWithAValidationProblem(string query, string parameter)
    {
        var response = await Search(query);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.True(response.Body.GetProperty("errors").TryGetProperty(parameter, out _));
    }

    private Task<ApiResponse> Search(string query) =>
        ApiResponse.GetAsync(api.Api, $"/v1/speeches/search?{query}");
}

using System.Net;

namespace Wortlaut.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SpeakerEndpointTests(ApiFixture api)
{
    [Fact]
    public async Task Suggestions_AreMatchedByTheBeginningOfAnyNamePart()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/speakers/suggest?q=Mus");

        var suggestion = Assert.Single(response.Body.EnumerateArray());
        Assert.Equal("100", suggestion.GetProperty("id").GetString());
        Assert.Equal("Dr. Anna Muster", suggestion.GetProperty("name").GetString());
        Assert.Equal("SPD", suggestion.GetProperty("faction").GetString());
    }

    [Fact]
    public async Task ASpeakerWithSeveralSpeeches_IsSuggestedOnce()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/speakers/suggest?q=Anna");

        Assert.Single(response.Body.EnumerateArray());
    }

    [Fact]
    public async Task UnknownName_YieldsNoSuggestions()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/speakers/suggest?q=Zzz");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Empty(response.Body.EnumerateArray());
    }

    [Theory]
    [InlineData("")]
    [InlineData("?q=a")]
    public async Task TooShortPrefix_IsAValidationProblem(string query)
    {
        var response = await ApiResponse.GetAsync(api.Api, $"/v1/speakers/suggest{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }
}

using System.Net;

namespace Wortlaut.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SpeechEndpointTests(ApiFixture api)
{
    [Fact]
    public async Task Speech_IsReturnedWithTextReactionsSourceAndRights()
    {
        var response = await ApiResponse.GetAsync(api.Api, $"/v1/speeches/{SpeechSeed.AnnaMusterId}");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var speech = response.Body;
        Assert.Equal("Dr. Anna Muster", speech.GetProperty("speaker").GetProperty("name").GetString());
        Assert.Contains("soziale Marktwirtschaft", speech.GetProperty("text").GetString());
        Assert.Equal(2, speech.GetProperty("reactions").GetArrayLength());
        Assert.Equal("heckle", speech.GetProperty("reactions")[1].GetProperty("type").GetString());
        Assert.Equal("https://dserver.bundestag.de/btp/21/21010.pdf", speech.GetProperty("source_url").GetString());
        Assert.Equal("Deutscher Bundestag", speech.GetProperty("rights").GetProperty("attribution").GetString());
        Assert.Equal(["read", "quote"], speech.GetProperty("rights").GetProperty("allowed_actions").EnumerateArray().Select(action => action.GetString()));
    }

    [Fact]
    public async Task InternalFields_AreNotExposed()
    {
        var response = await ApiResponse.GetAsync(api.Api, $"/v1/speeches/{SpeechSeed.AnnaMusterId}");

        Assert.False(response.Body.TryGetProperty("content_hash", out _));
    }

    [Fact]
    public async Task UnknownSpeech_IsAnsweredWithAProblemDetail()
    {
        var response = await ApiResponse.GetAsync(api.Api, "/v1/speeches/99-99-99999");

        Assert.Equal(HttpStatusCode.NotFound, response.Status);
        Assert.Equal("Speech not found", response.Body.GetProperty("title").GetString());
    }
}

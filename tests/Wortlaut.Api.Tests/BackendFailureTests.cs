using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wortlaut.Api.Elasticsearch;

namespace Wortlaut.Api.Tests;

public sealed class BackendFailureTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(host => host.ConfigureServices(services =>
            services.AddScoped<ISpeechIndex, UnreachableIndex>()));

    public void Dispose() => factory.Dispose();

    [Theory]
    [InlineData("/v1/speeches/search?q=Wort")]
    [InlineData("/v1/speeches/21-10-00100")]
    [InlineData("/v1/trends?q=Wort")]
    [InlineData("/v1/speakers/suggest?q=Muster")]
    public async Task WhenTheIndexIsDown_TheApiAnswersWithServiceUnavailable(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("The search is temporarily unavailable.", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WhenTheIndexIsDown_TheApiIsUnhealthy()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task TheErrorDoesNotRevealInternals()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/v1/speeches/search?q=Wort", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("elasticsearch", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class UnreachableIndex : ISpeechIndex
    {
        public Task<JsonElement> SearchAsync(object query, CancellationToken cancellationToken) => throw Unreachable();

        public Task<JsonElement?> FindSpeechAsync(string id, CancellationToken cancellationToken) => throw Unreachable();

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(false);

        private static SpeechIndexException Unreachable() => new("Elasticsearch is not reachable.");
    }
}

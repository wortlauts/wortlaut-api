using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Elasticsearch;

namespace Wortlaut.Api.Tests.Integration;

public sealed class ApiFixture : IAsyncLifetime
{
    private const string Image = "docker.elastic.co/elasticsearch/elasticsearch:9.5.7";
    private const string TemplateName = "wortlaut-speeches";
    private const string IndexName = "speeches-v1";
    private const string AliasName = "speeches";

    private readonly ElasticsearchContainer container = new ElasticsearchBuilder(Image).Build();
    private WebApplicationFactory<Program>? factory;

    public HttpClient Api { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        await SeedAsync();

        var connection = new Uri(container.GetConnectionString());
        var credentials = connection.UserInfo.Split(':');
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Elasticsearch:Url"] = connection.GetLeftPart(UriPartial.Authority),
                ["Elasticsearch:Alias"] = AliasName,
                ["Elasticsearch:Username"] = credentials[0],
                ["Elasticsearch:Password"] = credentials[1],
            }));
            host.ConfigureServices(AcceptTheSelfSignedCertificateOfTheTestContainer);
        });
        Api = factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        Api.Dispose();
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    private async Task SeedAsync()
    {
        using var elasticsearch = CreateElasticsearchClient();
        var template = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "contracts", "index-template.json"));

        await SendAsync(elasticsearch, HttpMethod.Put, $"_index_template/{TemplateName}", template, "application/json");
        await SendAsync(elasticsearch, HttpMethod.Put, IndexName, body: null, "application/json");
        await SendAsync(elasticsearch, HttpMethod.Put, $"{IndexName}/_alias/{AliasName}", body: null, "application/json");

        var bulk = new StringBuilder();
        foreach (var speech in SpeechSeed.All)
        {
            var id = JsonSerializer.SerializeToElement(speech).GetProperty("id").GetString();
            bulk.Append(JsonSerializer.Serialize(new { index = new { _index = IndexName, _id = id } })).Append('\n');
            bulk.Append(JsonSerializer.Serialize(speech)).Append('\n');
        }

        var result = await SendAsync(elasticsearch, HttpMethod.Post, "_bulk?refresh=true", bulk.ToString(), "application/x-ndjson");
        if (JsonDocument.Parse(result).RootElement.GetProperty("errors").GetBoolean())
        {
            throw new InvalidOperationException($"The seed data does not fit the index template: {result}");
        }
    }

    private HttpClient CreateElasticsearchClient()
    {
        var connection = new Uri(container.GetConnectionString());
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(connection.UserInfo));
        var client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            BaseAddress = new Uri(connection.GetLeftPart(UriPartial.Authority)),
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    }

    private static async Task<string> SendAsync(HttpClient client, HttpMethod method, string path, string? body, string contentType)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : new StringContent(body, Encoding.UTF8, contentType),
        };
        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode
            ? content
            : throw new InvalidOperationException($"{method} {path} failed with {(int)response.StatusCode}: {content}");
    }

    private static void AcceptTheSelfSignedCertificateOfTheTestContainer(IServiceCollection services) =>
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        }));
}

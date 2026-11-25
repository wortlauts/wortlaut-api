using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Wortlaut.Api.Elasticsearch;

internal sealed class EsSpeechIndex(HttpClient http, IOptions<EsOptions> options) : ISpeechIndex
{
    private static readonly JsonSerializerOptions QueryJson = new();

    private readonly string alias = options.Value.Alias;

    public async Task<JsonElement> SearchAsync(object query, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Post, $"{alias}/_search", JsonContent.Create(query, options: QueryJson), cancellationToken);
        return await ReadBodyAsync(response, cancellationToken);
    }

    public async Task<JsonElement?> FindSpeechAsync(string id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Get, $"{alias}/_doc/{Uri.EscapeDataString(id)}", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var document = await ReadBodyAsync(response, cancellationToken);
        return document.GetProperty("_source");
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Get, "_cluster/health", content: null, cancellationToken);
            var health = await ReadBodyAsync(response, cancellationToken);
            return health.GetProperty("status").GetString() != "red";
        }
        catch (SpeechIndexException)
        {
            return false;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                var status = (int)response.StatusCode;
                response.Dispose();
                throw new SpeechIndexException($"Elasticsearch answered {method} {path} with status {status}.");
            }

            return response;
        }
        catch (HttpRequestException exception)
        {
            throw new SpeechIndexException($"Elasticsearch is not reachable: {exception.Message}", exception);
        }
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }
}

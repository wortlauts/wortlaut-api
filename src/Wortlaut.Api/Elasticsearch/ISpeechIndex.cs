using System.Text.Json;

namespace Wortlaut.Api.Elasticsearch;

public interface ISpeechIndex
{
    Task<JsonElement> SearchAsync(object query, CancellationToken cancellationToken);

    Task<JsonElement?> FindSpeechAsync(string id, CancellationToken cancellationToken);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}

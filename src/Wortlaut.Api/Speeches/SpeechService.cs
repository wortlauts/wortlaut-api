using System.Text.Json;
using Wortlaut.Api.Elasticsearch;
using Wortlaut.Api.Http;

namespace Wortlaut.Api.Speeches;

public sealed class SpeechService(ISpeechIndex index)
{
    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var result = await index.SearchAsync(SearchQueryBuilder.Build(request), cancellationToken);
        return SearchResponseMapper.Map(result, request);
    }

    public async Task<SpeechDetail?> FindAsync(string id, CancellationToken cancellationToken)
    {
        var source = await index.FindSpeechAsync(id, cancellationToken);
        return source?.Deserialize<SpeechDetail>(ApiJson.Options);
    }
}

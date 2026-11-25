using System.Text.Json;
using Wortlaut.Api.Elasticsearch;
using Wortlaut.Api.Http;
using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Speakers;

public sealed class SpeakerService(ISpeechIndex index)
{
    public const int MinPrefixLength = 2;
    public const int MaxSuggestions = 10;

    public async Task<IReadOnlyList<SpeakerSuggestion>> SuggestAsync(string prefix, CancellationToken cancellationToken)
    {
        var result = await index.SearchAsync(Query(prefix), cancellationToken);

        return result.GetProperty("aggregations").GetProperty("speakers").GetProperty("buckets").EnumerateArray()
            .Select(bucket => bucket.GetProperty("speaker").GetProperty("hits").GetProperty("hits")[0].GetProperty("_source").GetProperty("speaker"))
            .Select(speaker => speaker.Deserialize<SpeakerInfo>(ApiJson.Options)!)
            .Select(speaker => new SpeakerSuggestion(speaker.Id, speaker.Name, speaker.Faction))
            .ToList();
    }

    private static object Query(string prefix) => new
    {
        size = 0,
        query = new { match_phrase_prefix = TextQuery.Field("speaker.name", prefix) },
        aggs = new
        {
            speakers = new
            {
                terms = new { field = "speaker.id", size = MaxSuggestions },
                aggs = new { speaker = new { top_hits = new { size = 1, _source = new[] { "speaker" } } } },
            },
        },
    };
}

using System.Text.Json;
using System.Text.Json.Nodes;
using Wortlaut.Api.Http;
using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Tests.Contracts;

public class IndexTemplateContractTests
{
    private static readonly string[] FieldsUsedInQueries =
    [
        "id",
        "legislative_period",
        "date",
        "text",
        "text.exact",
        "speaker.id",
        "speaker.name",
        "speaker.faction",
        "reactions.type",
    ];

    [Fact]
    public void EveryFieldTheApiReadsIsMappedInThePinnedTemplate()
    {
        var detail = new SpeechDetail(
            "21-10-00100", 21, 10, new DateOnly(2026, 1, 1), "Thema", "plenary",
            new SpeakerInfo("1", "Name", "SPD", "Rolle"), "Text",
            [new ReactionInfo("applause", "SPD", "Text")], 1, "https://example.org",
            new RightsInfo("Lizenz", "Quelle", ["read"]));
        var document = JsonSerializer.SerializeToNode(detail, ApiJson.Options)!.AsObject();

        var unmapped = UnmappedPaths(document, Mappings(), prefix: string.Empty).ToList();

        Assert.Empty(unmapped);
    }

    [Fact]
    public void EveryFieldTheApiQueriesIsMappedInThePinnedTemplate()
    {
        var unmapped = FieldsUsedInQueries.Where(field => !IsMapped(Mappings(), field.Split('.'))).ToList();

        Assert.Empty(unmapped);
    }

    [Fact]
    public void TheFieldsOfASearchHitAreMappedInThePinnedTemplate()
    {
        var hit = new SearchHit
        {
            Id = "1", LegislativePeriod = 21, Session = 1, Date = new DateOnly(2026, 1, 1), AgendaItem = "Thema",
            Delivery = "plenary", Speaker = new SpeakerInfo("1", "Name", "SPD", "Rolle"), WordCount = 1, SourceUrl = "u",
        };
        var document = JsonSerializer.SerializeToNode(hit, ApiJson.Options)!.AsObject();
        document.Remove("highlights");

        Assert.Empty(UnmappedPaths(document, Mappings(), prefix: string.Empty));
    }

    private static JsonObject Mappings()
    {
        var template = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contracts", "index-template.json"));
        return JsonNode.Parse(template)!["template"]!["mappings"]!["properties"]!.AsObject();
    }

    private static bool IsMapped(JsonObject properties, IReadOnlyList<string> path)
    {
        if (properties[path[0]] is not JsonObject field)
        {
            return false;
        }

        if (path.Count == 1)
        {
            return true;
        }

        var children = field["properties"] ?? field["fields"];
        return children is JsonObject childObject && IsMapped(childObject, path.Skip(1).ToList());
    }

    private static IEnumerable<string> UnmappedPaths(JsonObject document, JsonObject properties, string prefix)
    {
        foreach (var (name, value) in document)
        {
            var path = prefix + name;
            if (properties[name] is not JsonObject field)
            {
                yield return path;
                continue;
            }

            var children = value switch
            {
                JsonObject child => [child],
                JsonArray array => array.OfType<JsonObject>().ToList(),
                _ => new List<JsonObject>(),
            };

            foreach (var child in children)
            {
                foreach (var unmapped in UnmappedPaths(child, field["properties"]?.AsObject() ?? [], path + "."))
                {
                    yield return unmapped;
                }
            }
        }
    }
}

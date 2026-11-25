namespace Wortlaut.Api.Speeches;

internal static class SearchQueryBuilder
{
    private const int FacetSize = 20;
    private const int FragmentSize = 200;
    private const int FragmentsPerHit = 3;

    private static readonly string[] FieldsNotReturnedInHits = ["text", "reactions", "rights", "content_hash"];

    public static Dictionary<string, object> Build(SearchRequest request)
    {
        var query = new Dictionary<string, object>
        {
            ["from"] = (request.Page - 1) * request.PageSize,
            ["size"] = request.PageSize,
            ["track_total_hits"] = true,
            ["_source"] = new { excludes = FieldsNotReturnedInHits },
            ["query"] = new { @bool = new { must = new[] { TextQuery.Build(request.Query, request.Mode) }, filter = Filters(request) } },
            ["sort"] = Sort(request.Sort),
            ["aggs"] = Facets(),
        };

        if (request.Query is not null)
        {
            query["highlight"] = Highlight(request.Mode);
        }

        return query;
    }

    private static List<object> Filters(SearchRequest request)
    {
        var filters = new List<object>();

        if (request.Faction is not null)
        {
            filters.Add(new { term = TextQuery.Field("speaker.faction", request.Faction) });
        }

        if (request.Speaker is not null)
        {
            filters.Add(new { match_phrase = TextQuery.Field("speaker.name", request.Speaker) });
        }

        if (request.LegislativePeriod is not null)
        {
            filters.Add(new { term = TextQuery.Field("legislative_period", request.LegislativePeriod) });
        }

        if (request.From is not null || request.To is not null)
        {
            filters.Add(new { range = TextQuery.Field("date", DateRange(request.From, request.To)) });
        }

        if (request.Reaction is not null)
        {
            var reactionType = request.Reaction.Value.ToString().ToLowerInvariant();
            filters.Add(new { nested = new { path = "reactions", query = new { term = TextQuery.Field("reactions.type", reactionType) } } });
        }

        return filters;
    }

    private static Dictionary<string, string> DateRange(DateOnly? from, DateOnly? to)
    {
        var range = new Dictionary<string, string>();
        if (from is not null)
        {
            range["gte"] = from.Value.ToString("yyyy-MM-dd");
        }

        if (to is not null)
        {
            range["lte"] = to.Value.ToString("yyyy-MM-dd");
        }

        return range;
    }

    private static object[] Sort(SearchSort sort) => sort switch
    {
        SearchSort.Date => [new { date = new { order = "desc" } }, new { id = new { order = "asc" } }],
        _ => ["_score", new { date = new { order = "desc" } }, new { id = new { order = "asc" } }],
    };

    private static object Facets() => new
    {
        faction = new { terms = new { field = "speaker.faction", size = FacetSize } },
        legislative_period = new { terms = new { field = "legislative_period", size = FacetSize, order = new { _key = "desc" } } },
        reaction = new
        {
            nested = new { path = "reactions" },
            aggs = new { types = new { terms = new { field = "reactions.type", size = FacetSize } } },
        },
    };

    private static object Highlight(SearchMode mode) => new
    {
        encoder = "html",
        pre_tags = new[] { "<mark>" },
        post_tags = new[] { "</mark>" },
        fields = TextQuery.Field(
            TextQuery.HighlightField(mode),
            new { fragment_size = FragmentSize, number_of_fragments = FragmentsPerHit }),
    };
}

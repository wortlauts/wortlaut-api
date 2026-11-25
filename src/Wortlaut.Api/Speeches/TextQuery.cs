namespace Wortlaut.Api.Speeches;

internal static class TextQuery
{
    private const string StemmedField = "text";
    private const string ExactField = "text.exact";

    public static object Build(string? query, SearchMode mode)
    {
        if (query is null)
        {
            return new { match_all = new { } };
        }

        return mode switch
        {
            SearchMode.Phrase => new { match_phrase = Field(ExactField, query) },
            SearchMode.Fuzzy => new { match = Field(ExactField, new { query, fuzziness = "AUTO", @operator = "and" }) },
            _ => new { match = Field(StemmedField, new { query, @operator = "and" }) },
        };
    }

    public static string HighlightField(SearchMode mode) => mode == SearchMode.Stem ? StemmedField : ExactField;

    public static Dictionary<string, object> Field(string name, object value) => new() { [name] = value };
}

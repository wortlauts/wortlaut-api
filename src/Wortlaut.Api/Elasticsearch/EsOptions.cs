namespace Wortlaut.Api.Elasticsearch;

public sealed class EsOptions
{
    public const string SectionName = "Elasticsearch";

    public Uri Url { get; set; } = new("http://localhost:9200");

    public string Alias { get; set; } = "speeches";

    public string? Username { get; set; }

    public string? Password { get; set; }
}

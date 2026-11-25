using System.Text.Json;

namespace Wortlaut.Api.Http;

internal sealed class ParameterErrors
{
    private readonly Dictionary<string, string[]> errors = [];

    public bool Any => errors.Count > 0;

    public IDictionary<string, string[]> ToDictionary() => errors;

    public void Add(string parameter, string message) => errors[parameter] = [message];

    public T ParseEnum<T>(string parameter, string? raw, T fallback)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (Enum.TryParse<T>(raw.Replace("_", string.Empty), ignoreCase: true, out var value) && Enum.IsDefined(value))
        {
            return value;
        }

        var allowed = Enum.GetNames<T>().Select(JsonNamingPolicy.SnakeCaseLower.ConvertName);
        Add(parameter, $"Allowed values: {string.Join(", ", allowed)}.");
        return fallback;
    }

    public int ParseRange(string parameter, int? raw, int fallback, int min, int max)
    {
        var value = raw ?? fallback;
        if (value >= min && value <= max)
        {
            return value;
        }

        Add(parameter, $"Must be between {min} and {max}.");
        return fallback;
    }
}

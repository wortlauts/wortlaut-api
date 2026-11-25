using Microsoft.AspNetCore.Mvc;

namespace Wortlaut.Api.Speakers;

internal static class SpeakerEndpoints
{
    public static void MapSpeakerEndpoints(this RouteGroupBuilder api) =>
        api.MapGet("/speakers/suggest", async ([FromQuery(Name = "q")] string? query, SpeakerService service, CancellationToken cancellationToken) =>
            query is { Length: >= SpeakerService.MinPrefixLength }
                ? Results.Ok(await service.SuggestAsync(query.Trim(), cancellationToken))
                : Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["q"] = [$"At least {SpeakerService.MinPrefixLength} characters are required."],
                }))
            .WithTags("Speakers")
            .WithName("SuggestSpeakers")
            .WithSummary("Suggest speakers by the beginning of their name")
            .Produces<IReadOnlyList<SpeakerSuggestion>>()
            .ProducesValidationProblem();
}

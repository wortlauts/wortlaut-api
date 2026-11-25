using Microsoft.AspNetCore.Mvc;

namespace Wortlaut.Api.Speeches;

internal static class SpeechEndpoints
{
    public static void MapSpeechEndpoints(this RouteGroupBuilder api)
    {
        var speeches = api.MapGroup("/speeches").WithTags("Speeches");

        speeches.MapGet("/search", async ([AsParameters] SearchParameters parameters, SpeechService service, CancellationToken cancellationToken) =>
            parameters.TryCreateRequest(out var request, out var errors)
                ? Results.Ok(await service.SearchAsync(request, cancellationToken))
                : Results.ValidationProblem(errors))
            .WithName("SearchSpeeches")
            .WithSummary("Search speeches by word or phrase, with filters, highlights and facet counts")
            .Produces<SearchResponse>()
            .ProducesValidationProblem();

        speeches.MapGet("/{id}", async (string id, SpeechService service, CancellationToken cancellationToken) =>
            await service.FindAsync(id, cancellationToken) is { } speech
                ? Results.Ok(speech)
                : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Speech not found"))
            .WithName("GetSpeech")
            .WithSummary("Get one speech with its full text, reactions and source")
            .Produces<SpeechDetail>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

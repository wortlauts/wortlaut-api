using Microsoft.AspNetCore.Mvc;

namespace Wortlaut.Api.Trends;

internal static class TrendEndpoints
{
    public static void MapTrendEndpoints(this RouteGroupBuilder api) =>
        api.MapGet("/trends", async ([AsParameters] TrendParameters parameters, TrendService service, CancellationToken cancellationToken) =>
            parameters.TryCreateRequest(out var request, out var errors)
                ? Results.Ok(await service.GetAsync(request, cancellationToken))
                : Results.ValidationProblem(errors))
            .WithTags("Trends")
            .WithName("GetTrends")
            .WithSummary("Count the speeches containing a term per year or legislative period, optionally normalised")
            .Produces<TrendResponse>()
            .ProducesValidationProblem();
}

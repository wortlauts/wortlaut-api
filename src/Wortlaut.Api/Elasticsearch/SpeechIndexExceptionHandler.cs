using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Wortlaut.Api.Elasticsearch;

internal sealed class SpeechIndexExceptionHandler(ILogger<SpeechIndexExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not SpeechIndexException)
        {
            return false;
        }

        logger.LogError(exception, "The search backend failed.");
        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The search is temporarily unavailable.",
            },
            cancellationToken);
        return true;
    }
}

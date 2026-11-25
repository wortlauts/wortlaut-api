using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wortlaut.Api.Elasticsearch;

internal sealed class EsHealthCheck(ISpeechIndex index) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await index.IsHealthyAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Elasticsearch is not reachable or its cluster is red.");
}

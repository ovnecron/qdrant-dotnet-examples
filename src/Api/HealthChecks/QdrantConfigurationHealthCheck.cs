using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using VectorStore.Qdrant.Options;

namespace Api.HealthChecks;

public sealed class QdrantConfigurationHealthCheck : IHealthCheck
{
    private readonly QdrantOptions _qdrantOptions;

    public QdrantConfigurationHealthCheck(IOptions<QdrantOptions> qdrantOptions)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _qdrantOptions = qdrantOptions.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(_qdrantOptions.EndpointRest))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Qdrant REST endpoint is not configured."));
        }

        return Uri.TryCreate(_qdrantOptions.EndpointRest, UriKind.Absolute, out _)
            ? Task.FromResult(HealthCheckResult.Healthy())
            : Task.FromResult(HealthCheckResult.Unhealthy("Qdrant REST endpoint is invalid."));
    }
}

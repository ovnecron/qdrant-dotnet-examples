using Grpc.Core;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Qdrant.Client;

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

        if (!QdrantEndpointResolver.TryResolveGrpcEndpoint(_qdrantOptions, out _, out var failureMessage))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(failureMessage));
        }

        return CheckQdrantAvailabilityAsync(cancellationToken);
    }

    private async Task<HealthCheckResult> CheckQdrantAvailabilityAsync(CancellationToken cancellationToken)
    {
        var grpcEndpoint = QdrantEndpointResolver.ResolveGrpcEndpoint(_qdrantOptions);
        var timeout = _qdrantOptions.Timeout <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(5)
            : _qdrantOptions.Timeout;

        try
        {
            var client = new QdrantClient(grpcEndpoint, _qdrantOptions.ApiKey, timeout);
            _ = await client
                .CollectionExistsAsync("__readiness_probe__", cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return HealthCheckResult.Unhealthy(
                "Qdrant is unreachable.",
                exception);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Qdrant readiness probe failed.",
                exception);
        }
    }
}

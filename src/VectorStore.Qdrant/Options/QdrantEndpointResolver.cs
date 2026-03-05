using System.Diagnostics.CodeAnalysis;

namespace VectorStore.Qdrant.Options;

public static class QdrantEndpointResolver
{
    private static readonly Uri DefaultGrpcEndpoint = new("http://localhost:6334");

    public static Uri ResolveGrpcEndpoint(QdrantOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (TryResolveGrpcEndpoint(options, out var endpoint, out var failureMessage))
        {
            return endpoint;
        }

        throw new InvalidOperationException(failureMessage);
    }

    public static bool TryResolveGrpcEndpoint(
        QdrantOptions options,
        [NotNullWhen(true)] out Uri? endpoint,
        [NotNullWhen(false)] out string? failureMessage)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (TryParseUri(options.EndpointGrpc, out endpoint))
        {
            failureMessage = null;
            return true;
        }

        if (TryParseUri(options.EndpointRest, out var restEndpoint))
        {
            if (TryPromoteRestEndpointToGrpc(restEndpoint, out endpoint))
            {
                failureMessage = null;
                return true;
            }

            endpoint = null;
            failureMessage = "Cannot derive gRPC endpoint from Qdrant:EndpointRest. Set Qdrant:EndpointGrpc explicitly.";
            return false;
        }

        endpoint = DefaultGrpcEndpoint;
        failureMessage = null;
        return true;
    }

    public static bool IsNullOrAbsoluteUri(string? rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue) ||
            Uri.TryCreate(rawValue, UriKind.Absolute, out _);
    }

    private static bool TryParseUri(string? rawValue, [NotNullWhen(true)] out Uri? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(rawValue) &&
            Uri.TryCreate(rawValue, UriKind.Absolute, out var parsedEndpoint) &&
            parsedEndpoint is not null)
        {
            endpoint = parsedEndpoint;
            return true;
        }

        endpoint = null;
        return false;
    }

    private static bool TryPromoteRestEndpointToGrpc(
        Uri restEndpoint,
        [NotNullWhen(true)] out Uri? grpcEndpoint)
    {
        if (restEndpoint.Port != 6333)
        {
            grpcEndpoint = null;
            return false;
        }

        var uriBuilder = new UriBuilder(restEndpoint)
        {
            Port = 6334
        };

        grpcEndpoint = uriBuilder.Uri;
        return true;
    }
}

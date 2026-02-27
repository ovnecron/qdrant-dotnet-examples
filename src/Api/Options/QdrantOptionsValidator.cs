using Microsoft.Extensions.Options;

using VectorStore.Qdrant.Options;

namespace Api.Options;

public sealed class QdrantOptionsValidator : IValidateOptions<QdrantOptions>
{
    public ValidateOptionsResult Validate(string? name, QdrantOptions options)
    {
        _ = name;
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!IsSupportedTransport(options.Transport))
        {
            failures.Add("Qdrant:Transport must be either 'Rest' or 'Grpc'.");
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            failures.Add("Qdrant:Timeout must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Collection))
        {
            failures.Add("Qdrant:Collection must be provided.");
        }

        if (!IsNullOrAbsoluteUri(options.EndpointRest))
        {
            failures.Add("Qdrant:EndpointRest must be an absolute URI when provided.");
        }

        if (!IsNullOrAbsoluteUri(options.EndpointGrpc))
        {
            failures.Add("Qdrant:EndpointGrpc must be an absolute URI when provided.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsSupportedTransport(string? transport)
    {
        if (string.IsNullOrWhiteSpace(transport))
        {
            return false;
        }

        return transport.Equals("Rest", StringComparison.OrdinalIgnoreCase) ||
            transport.Equals("Grpc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNullOrAbsoluteUri(string? rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue) ||
            Uri.TryCreate(rawValue, UriKind.Absolute, out _);
    }
}

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

        if (options.Timeout <= TimeSpan.Zero)
        {
            failures.Add("Qdrant:Timeout must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Collection))
        {
            failures.Add("Qdrant:Collection must be provided.");
        }

        if (!QdrantEndpointResolver.IsNullOrAbsoluteUri(options.EndpointRest))
        {
            failures.Add("Qdrant:EndpointRest must be an absolute URI when provided.");
        }

        if (!QdrantEndpointResolver.IsNullOrAbsoluteUri(options.EndpointGrpc))
        {
            failures.Add("Qdrant:EndpointGrpc must be an absolute URI when provided.");
        }

        if (!QdrantEndpointResolver.TryResolveGrpcEndpoint(options, out _, out var failureMessage))
        {
            failures.Add(failureMessage ?? "Qdrant endpoint is not configured.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

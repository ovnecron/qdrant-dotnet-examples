using Embeddings.Clients;
using Embeddings.Options;

using Microsoft.Extensions.Options;

namespace Api.Options;

public sealed class EmbeddingOptionsValidator : IValidateOptions<EmbeddingOptions>
{
    public ValidateOptionsResult Validate(string? name, EmbeddingOptions options)
    {
        _ = name;
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add("Embedding:Provider must be provided.");
        }
        else if (!DeterministicTextEmbeddingClient.SupportsProvider(options.Provider))
        {
            failures.Add("Embedding:Provider must be one of: Deterministic, Mock.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            failures.Add("Embedding:Model must be provided.");
        }

        if (options.Dimension <= 0)
        {
            failures.Add("Embedding:Dimension must be greater than zero.");
        }

        if (options.BatchSize <= 0)
        {
            failures.Add("Embedding:BatchSize must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.SchemaVersion))
        {
            failures.Add("Embedding:SchemaVersion must be provided.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

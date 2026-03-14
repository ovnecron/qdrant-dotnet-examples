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
        if (!Enum.IsDefined(options.Provider))
        {
            failures.Add("Embedding:Provider must be one of: Deterministic, Ollama.");
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

        if (options.Provider == EmbeddingProvider.Ollama &&
            !IsValidHttpBaseUrl(options.BaseUrl))
        {
            failures.Add("Embedding:BaseUrl must be an absolute http/https URI for Ollama.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidHttpBaseUrl(string? baseUrl)
    {
        try
        {
            _ = OllamaHttpClientConfiguration.ResolveBaseAddress(baseUrl);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

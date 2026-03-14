using Embeddings.Clients;

using Microsoft.Extensions.Options;

namespace Api.Options;

public sealed class RagOptionsValidator : IValidateOptions<RagOptions>
{
    public ValidateOptionsResult Validate(string? name, RagOptions options)
    {
        _ = name;
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (!Enum.IsDefined(options.AnswerProvider))
        {
            failures.Add("Rag:AnswerProvider must be one of: Deterministic, Ollama.");
        }

        if (options.MaxAnswerTokens <= 0)
        {
            failures.Add("Rag:MaxAnswerTokens must be greater than zero.");
        }

        if (options.RequestTimeoutSeconds <= 0)
        {
            failures.Add("Rag:RequestTimeoutSeconds must be greater than zero.");
        }

        if (options.Temperature < 0)
        {
            failures.Add("Rag:Temperature must be greater than or equal to zero.");
        }

        if (options.AnswerProvider == RagAnswerProvider.Ollama)
        {
            if (string.IsNullOrWhiteSpace(options.AnswerModel))
            {
                failures.Add("Rag:AnswerModel must be provided when Rag:AnswerProvider is Ollama.");
            }

            if (!IsValidHttpBaseUrl(options.BaseUrl))
            {
                failures.Add("Rag:BaseUrl must be an absolute http/https URI when Rag:AnswerProvider is Ollama.");
            }
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

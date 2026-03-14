using Api.Services;

using Embeddings.Clients;

using Microsoft.Extensions.Options;

namespace Api.Options;

internal static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredRagAnswerGenerator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<OllamaRagPromptBuilder>();
        services.AddScoped<DeterministicRagAnswerGenerator>();
        services.AddHttpClient<OllamaRagAnswerGenerator>(
            (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RagOptions>>().Value;
                OllamaHttpClientConfiguration.ConfigureJsonApiClient(httpClient, options.BaseUrl, options.ApiKey);
            });

        services.AddScoped<IRagAnswerGenerator>(
            serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RagOptions>>().Value;

                return options.AnswerProvider switch
                {
                    RagAnswerProvider.Deterministic => serviceProvider.GetRequiredService<DeterministicRagAnswerGenerator>(),
                    RagAnswerProvider.Ollama => serviceProvider.GetRequiredService<OllamaRagAnswerGenerator>(),
                    _ => throw new InvalidOperationException($"Unsupported RAG answer provider '{options.AnswerProvider}'.")
                };
            });

        return services;
    }
}

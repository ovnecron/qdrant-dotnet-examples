using Embeddings.Clients;
using Embeddings.Interfaces;
using Embeddings.Options;

using Microsoft.Extensions.Options;

namespace Api.Options;

internal static class EmbeddingServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredTextEmbeddingClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<DeterministicTextEmbeddingClient>();
        services.AddHttpClient<OllamaTextEmbeddingClient>(
            (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
                OllamaHttpClientConfiguration.ConfigureJsonApiClient(httpClient, options.BaseUrl, options.ApiKey);
            });

        services.AddTransient<ITextEmbeddingClient>(
            serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;

                return options.Provider switch
                {
                    EmbeddingProvider.Deterministic => serviceProvider.GetRequiredService<DeterministicTextEmbeddingClient>(),
                    EmbeddingProvider.Ollama => serviceProvider.GetRequiredService<OllamaTextEmbeddingClient>(),
                    _ => throw new InvalidOperationException($"Unsupported embedding provider '{options.Provider}'.")
                };
            });

        return services;
    }
}

using System.Net.Http.Headers;

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

                httpClient.BaseAddress = OllamaTextEmbeddingClient.ResolveBaseAddress(options.BaseUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    httpClient.DefaultRequestHeaders.Authorization = null;
                    return;
                }

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    options.ApiKey.Trim());
            });

        services.AddTransient<ITextEmbeddingClient>(
            serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;

                if (DeterministicTextEmbeddingClient.SupportsProvider(options.Provider))
                {
                    return serviceProvider.GetRequiredService<DeterministicTextEmbeddingClient>();
                }

                if (OllamaTextEmbeddingClient.SupportsProvider(options.Provider))
                {
                    return serviceProvider.GetRequiredService<OllamaTextEmbeddingClient>();
                }

                throw new InvalidOperationException($"Unsupported embedding provider '{options.Provider}'.");
            });

        return services;
    }
}

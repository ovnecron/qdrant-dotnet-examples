using Microsoft.Extensions.DependencyInjection;

using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Clients;
using VectorStore.Qdrant.Options;

namespace VectorStore.Qdrant;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQdrantVectorStore(
        this IServiceCollection services,
        Action<QdrantOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<QdrantOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.AddSingleton<IVectorStoreClient, QdrantVectorStoreClient>();
        return services;
    }
}

using Microsoft.AspNetCore.Mvc.Testing;

using QdrantDotNetExample.Common;

using Testcontainers.Qdrant;

namespace Integration.Fixtures;

public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    private readonly QdrantContainer _qdrantContainer;

    private ApiWebApplicationFactory? _apiFactory;

    public HttpClient Client { get; private set; } = default!;

    public ApiIntegrationFixture()
    {
        var (repository, tag) = QdrantContainerImage.Resolve();
        _qdrantContainer = new QdrantBuilder($"{repository}:{tag}")
            .WithCleanUp(true)
            .Build();
    }

    public string CreateCollectionName()
    {
        return $"knowledge_chunks_it_{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        await _qdrantContainer.StartAsync().ConfigureAwait(false);

        var restEndpoint = _qdrantContainer.GetHttpConnectionString();
        var grpcEndpoint = _qdrantContainer.GetGrpcConnectionString();

        var configuration = new Dictionary<string, string?>
        {
            ["Qdrant:EndpointRest"] = restEndpoint,
            ["Qdrant:EndpointGrpc"] = grpcEndpoint
        };

        _apiFactory = new ApiWebApplicationFactory(configuration);
        Client = _apiFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    public async Task DisposeAsync()
    {
        if (Client is not null)
        {
            Client.Dispose();
        }

        _apiFactory?.Dispose();
        if (_qdrantContainer is not null)
        {
            await _qdrantContainer.DisposeAsync().ConfigureAwait(false);
        }
    }
}

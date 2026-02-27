using Microsoft.AspNetCore.Mvc.Testing;

using Testcontainers.Qdrant;

namespace Integration.Fixtures;

public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    private const string QdrantTransportKey = "QDRANT__TRANSPORT";
    private const string QdrantRestEndpointKey = "QDRANT__ENDPOINT_REST";
    private const string QdrantGrpcEndpointKey = "QDRANT__ENDPOINT_GRPC";

    private readonly QdrantContainer _qdrantContainer = new QdrantBuilder("qdrant/qdrant:latest")
        .WithCleanUp(true)
        .Build();

    private ApiWebApplicationFactory? _apiFactory;
    private string? _previousTransport;
    private string? _previousRestEndpoint;
    private string? _previousGrpcEndpoint;

    public HttpClient Client { get; private set; } = default!;

    public string CreateCollectionName()
    {
        return $"knowledge_chunks_it_{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        await _qdrantContainer.StartAsync().ConfigureAwait(false);

        var restEndpoint = _qdrantContainer.GetHttpConnectionString();
        var grpcEndpoint = _qdrantContainer.GetGrpcConnectionString();

        CaptureAndSetEnvironmentVariables(restEndpoint, grpcEndpoint);

        try
        {
            var configuration = new Dictionary<string, string?>
            {
                ["Qdrant:Transport"] = "Rest",
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
        catch
        {
            RestoreEnvironmentVariables();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (Client is not null)
        {
            Client.Dispose();
        }

        _apiFactory?.Dispose();
        RestoreEnvironmentVariables();
        if (_qdrantContainer is not null)
        {
            await _qdrantContainer.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void CaptureAndSetEnvironmentVariables(string restEndpoint, string grpcEndpoint)
    {
        _previousTransport = Environment.GetEnvironmentVariable(QdrantTransportKey);
        _previousRestEndpoint = Environment.GetEnvironmentVariable(QdrantRestEndpointKey);
        _previousGrpcEndpoint = Environment.GetEnvironmentVariable(QdrantGrpcEndpointKey);

        Environment.SetEnvironmentVariable(QdrantTransportKey, "Rest");
        Environment.SetEnvironmentVariable(QdrantRestEndpointKey, restEndpoint);
        Environment.SetEnvironmentVariable(QdrantGrpcEndpointKey, grpcEndpoint);
    }

    private void RestoreEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable(QdrantTransportKey, _previousTransport);
        Environment.SetEnvironmentVariable(QdrantRestEndpointKey, _previousRestEndpoint);
        Environment.SetEnvironmentVariable(QdrantGrpcEndpointKey, _previousGrpcEndpoint);
    }

}

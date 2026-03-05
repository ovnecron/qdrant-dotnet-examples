using VectorStore.Qdrant.Options;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class QdrantEndpointResolverTests
{
    [Fact]
    public void TryResolveGrpcEndpoint_UsesDefaultLocalEndpoint_WhenEndpointsAreUnset()
    {
        var options = new QdrantOptions
        {
            EndpointGrpc = null,
            EndpointRest = null
        };

        var resolved = QdrantEndpointResolver.TryResolveGrpcEndpoint(options, out var endpoint, out var failure);

        Assert.True(resolved);
        var resolvedEndpoint = Assert.IsType<Uri>(endpoint);
        Assert.Equal("http://localhost:6334/", resolvedEndpoint.ToString());
        Assert.Null(failure);
    }

    [Fact]
    public void TryResolveGrpcEndpoint_PromotesDefaultRestPortToGrpcPort()
    {
        var options = new QdrantOptions
        {
            EndpointGrpc = null,
            EndpointRest = "http://qdrant.local:6333"
        };

        var resolved = QdrantEndpointResolver.TryResolveGrpcEndpoint(options, out var endpoint, out var failure);

        Assert.True(resolved);
        var resolvedEndpoint = Assert.IsType<Uri>(endpoint);
        Assert.Equal("http://qdrant.local:6334/", resolvedEndpoint.ToString());
        Assert.Null(failure);
    }

    [Fact]
    public void TryResolveGrpcEndpoint_FailsWhenRestEndpointUsesCustomPortWithoutGrpcOverride()
    {
        var options = new QdrantOptions
        {
            EndpointGrpc = null,
            EndpointRest = "http://qdrant.local:7000"
        };

        var resolved = QdrantEndpointResolver.TryResolveGrpcEndpoint(options, out var endpoint, out var failure);

        Assert.False(resolved);
        Assert.Null(endpoint);
        Assert.Equal(
            "Cannot derive gRPC endpoint from Qdrant:EndpointRest. Set Qdrant:EndpointGrpc explicitly.",
            failure);
    }
}

using Embeddings.Clients;
using Embeddings.Contracts;
using Embeddings.Options;

using Microsoft.Extensions.Options;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class DeterministicTextEmbeddingClientTests
{
    [Fact]
    public async Task EmbedAsync_ReturnsDeterministicVector_ForSameText()
    {
        var client = CreateClient();
        var request = new TextEmbeddingRequest
        {
            Text = "Run AppHost and wait for Qdrant to become ready.",
            Kind = EmbeddingKind.Document
        };

        var first = await client.EmbedAsync(request);
        var second = await client.EmbedAsync(request);

        Assert.Equal(first.Vector, second.Vector);
        Assert.Equal(first.Descriptor, second.Descriptor);
    }

    [Fact]
    public async Task EmbedAsync_ReturnsSameVectorSpace_ForSameTextAcrossKinds()
    {
        var client = CreateClient();

        var document = await client.EmbedAsync(
            new TextEmbeddingRequest
            {
                Text = "initialize qdrant collection",
                Kind = EmbeddingKind.Document
            });

        var query = await client.EmbedAsync(
            new TextEmbeddingRequest
            {
                Text = "initialize qdrant collection",
                Kind = EmbeddingKind.Query
            });

        Assert.Equal(document.Vector, query.Vector);
    }

    [Fact]
    public async Task EmbedAsync_ReturnsDifferentVectors_ForDifferentText()
    {
        var client = CreateClient();

        var first = await client.EmbedAsync(
            new TextEmbeddingRequest
            {
                Text = "qdrant collection init",
                Kind = EmbeddingKind.Document
            });

        var second = await client.EmbedAsync(
            new TextEmbeddingRequest
            {
                Text = "docker readiness troubleshooting",
                Kind = EmbeddingKind.Document
            });

        Assert.False(first.Vector.SequenceEqual(second.Vector));
    }

    [Fact]
    public async Task EmbedAsync_UsesConfiguredDescriptorAndDimension()
    {
        var client = CreateClient(
            new EmbeddingOptions
            {
                Provider = EmbeddingProvider.Deterministic,
                Model = "hashing-text-v2",
                Dimension = 256,
                BatchSize = 8,
                SchemaVersion = "v2"
            });

        var result = await client.EmbedAsync(
            new TextEmbeddingRequest
            {
                Text = "local vector search example",
                Kind = EmbeddingKind.Query
            });

        Assert.Equal(256, result.Vector.Count);
        Assert.Equal("Deterministic", result.Descriptor.Provider);
        Assert.Equal("hashing-text-v2", result.Descriptor.Model);
        Assert.Equal("v2", result.Descriptor.SchemaVersion);
    }

    [Fact]
    public async Task EmbedBatchAsync_PreservesInputOrder()
    {
        var client = CreateClient();
        var requests = new[]
        {
            new TextEmbeddingRequest { Text = "first item", Kind = EmbeddingKind.Document },
            new TextEmbeddingRequest { Text = "second item", Kind = EmbeddingKind.Query },
            new TextEmbeddingRequest { Text = "third item", Kind = EmbeddingKind.Document }
        };

        var results = await client.EmbedBatchAsync(requests);

        Assert.Equal(requests.Select(static request => request.Text), results.Select(static result => result.Text));
        Assert.Equal(requests.Select(static request => request.Kind), results.Select(static result => result.Kind));
    }

    [Fact]
    public async Task EmbedAsync_Throws_WhenTextIsEmpty()
    {
        var client = CreateClient();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => client.EmbedAsync(
                new TextEmbeddingRequest
                {
                    Text = "   ",
                    Kind = EmbeddingKind.Document
                }));

        Assert.Contains("Embedding text must be provided.", exception.Message);
    }

    private static DeterministicTextEmbeddingClient CreateClient(EmbeddingOptions? options = null)
    {
        return new DeterministicTextEmbeddingClient(
            Options.Create(
                options ?? new EmbeddingOptions
                {
                    Provider = EmbeddingProvider.Deterministic,
                    Model = "hashing-text-v1",
                    Dimension = 384,
                    BatchSize = 16,
                    SchemaVersion = "v1"
                }));
    }
}

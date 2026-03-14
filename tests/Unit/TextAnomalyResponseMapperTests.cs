using Api.Contracts;
using Api.Services;
using Api.Services.Mappers;

using Embeddings.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class TextAnomalyResponseMapperTests
{
    [Fact]
    public void ToScoreResponse_MapsNeighborsAndDebugMetadata()
    {
        var mapper = new TextAnomalyResponseMapper();

        var response = mapper.ToScoreResponse(
            traceId: "trace-123",
            collectionName: "knowledge_chunks_text_anomaly",
            text: "Check /health and /ready after startup.",
            includeNeighbors: true,
            includeDebug: true,
            embeddingDescriptor: new EmbeddingDescriptor
            {
                Provider = "Deterministic",
                Model = "hashing-text-v1",
                Dimension = 384,
                SchemaVersion = "v1"
            },
            neighbors:
            [
                new SearchResult
                {
                    ChunkId = "ops-health:0:ABCD",
                    DocId = "ops-health",
                    Source = "docs/ops-health.md",
                    Title = "Operational Health Checks",
                    Content = "Check the /health and /ready endpoints after startup.",
                    ContentPreview = null,
                    Score = 0.91f,
                    Tags = ["ops", "health"]
                }
            ],
            threshold: 0.35f,
            computation: new AnomalyScoreComputation(
                AnomalyScore: 0.09f,
                MeanNeighborSimilarity: 0.91f,
                MaxNeighborSimilarity: 0.91f,
                IsAnomalous: false));

        Assert.Equal("knowledge_chunks_text_anomaly", response.Collection);
        Assert.Equal("Check /health and /ready after startup.", response.Text);
        Assert.Equal(0.09f, response.AnomalyScore);
        Assert.Equal(1, response.NeighborCount);
        Assert.Single(response.Neighbors);
        Assert.Equal("Operational Health Checks", response.Neighbors[0].Title);
        var contentPreview = Assert.IsType<string>(response.Neighbors[0].ContentPreview);
        Assert.Contains("/health", contentPreview, StringComparison.Ordinal);
        var debug = Assert.IsType<TextAnomalyDebugResponse>(response.Debug);
        Assert.Equal("Deterministic", debug.EmbeddingProvider);
        Assert.Equal("hashing-text-v1", debug.EmbeddingModel);
        Assert.Equal("v1", debug.EmbeddingSchemaVersion);
    }

    [Fact]
    public void ToScoreResponse_OmitsNeighborsAndDebug_WhenDisabled()
    {
        var mapper = new TextAnomalyResponseMapper();

        var response = mapper.ToScoreResponse(
            traceId: "trace-123",
            collectionName: "knowledge_chunks_text_anomaly",
            text: "Check /health and /ready after startup.",
            includeNeighbors: false,
            includeDebug: false,
            embeddingDescriptor: new EmbeddingDescriptor
            {
                Provider = "Deterministic",
                Model = "hashing-text-v1",
                Dimension = 384,
                SchemaVersion = "v1"
            },
            neighbors:
            [
                new SearchResult
                {
                    ChunkId = "ops-health:0:ABCD",
                    DocId = "ops-health",
                    Source = "docs/ops-health.md",
                    Content = "Check the /health and /ready endpoints after startup.",
                    Score = 0.91f
                }
            ],
            threshold: 0.35f,
            computation: new AnomalyScoreComputation(
                AnomalyScore: 0.09f,
                MeanNeighborSimilarity: 0.91f,
                MaxNeighborSimilarity: 0.91f,
                IsAnomalous: false));

        Assert.Empty(response.Neighbors);
        Assert.Null(response.Debug);
    }
}

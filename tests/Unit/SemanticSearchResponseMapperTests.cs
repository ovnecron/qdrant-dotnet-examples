using Api.Services.Mappers;

using Embeddings.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class SemanticSearchResponseMapperTests
{
    [Fact]
    public void ToQueryResponse_MapsEmbeddingMetadataAndHits()
    {
        var mapper = new SemanticSearchResponseMapper();
        var response = mapper.ToQueryResponse(
            traceId: "trace-123",
            collectionName: "knowledge_chunks",
            queryText: "How do I run this locally?",
            embeddingDescriptor: new EmbeddingDescriptor
            {
                Provider = "Deterministic",
                Model = "hashing-text-v1",
                Dimension = 384,
                SchemaVersion = "v1"
            },
            hits:
            [
                new SearchResult
                {
                    ChunkId = "doc-1:0",
                    DocId = "doc-1",
                    Score = 0.98f,
                    Source = "docs/local-run.md",
                    Title = "Local Run",
                    Section = "Verify Health",
                    Content = "Check health and ready endpoints.",
                    ContentPreview = "Check health and ready endpoints.",
                    Tags = ["tutorial", "local"]
                }
            ]);

        Assert.Equal("trace-123", response.TraceId);
        Assert.Equal("knowledge_chunks", response.Collection);
        Assert.Equal("How do I run this locally?", response.QueryText);
        Assert.Equal("Deterministic", response.EmbeddingProvider);
        Assert.Equal("hashing-text-v1", response.EmbeddingModel);
        Assert.Equal("v1", response.EmbeddingSchemaVersion);

        var hit = Assert.Single(response.Hits);
        Assert.Equal("doc-1:0", hit.ChunkId);
        Assert.Equal("doc-1", hit.DocId);
        Assert.Equal("docs/local-run.md", hit.Source);
        Assert.Contains("tutorial", hit.Tags);
    }
}

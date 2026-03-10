using Api.Services;

using Embeddings.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class RagContextAssemblerTests
{
    [Fact]
    public void Assemble_BuildsContextAndCitations_FromRetrievedHits()
    {
        var assembler = new RagContextAssembler();
        var retrieval = new TextRetrievalResult(
            CollectionName: "knowledge_chunks",
            QueryText: "How do I check health locally?",
            Embedding: new EmbeddingDescriptor
            {
                Provider = "Deterministic",
                Model = "hashing-text-v1",
                Dimension = 384,
                SchemaVersion = "v1"
            },
            Hits:
            [
                new SearchResult
                {
                    ChunkId = "guide-local-run:0",
                    DocId = "guide-local-run",
                    Score = 0.91f,
                    Source = "docs/local-run.md",
                    Title = "Local Run",
                    Section = "Verify Health",
                    Content = "Check the /health and /ready endpoints after starting AppHost."
                },
                new SearchResult
                {
                    ChunkId = "guide-vector-delete:0",
                    DocId = "guide-vector-delete",
                    Score = 0.42f,
                    Source = "docs/vector-delete.md",
                    Title = "Delete Vectors",
                    Content = "Delete vectors by chunk ids when you want to remove stale content."
                }
            ]);

        var context = assembler.Assemble(retrieval);

        Assert.Equal(2, context.Citations.Count);
        Assert.Equal(2, context.RetrievedHitCount);
        Assert.Contains("[1] Source: docs/local-run.md", context.Context);
        Assert.Contains("Title: Local Run", context.Context);
        Assert.Contains("Section: Verify Health", context.Context);
        Assert.Contains("Check the /health and /ready endpoints after starting AppHost.", context.Context);
    }

    [Fact]
    public void Assemble_SkipsHitsWithoutUsableContent()
    {
        var assembler = new RagContextAssembler();
        var retrieval = new TextRetrievalResult(
            CollectionName: "knowledge_chunks",
            QueryText: "How do I check health locally?",
            Embedding: new EmbeddingDescriptor
            {
                Provider = "Deterministic",
                Model = "hashing-text-v1",
                Dimension = 384,
                SchemaVersion = "v1"
            },
            Hits:
            [
                new SearchResult
                {
                    ChunkId = "empty:0",
                    DocId = "empty",
                    Score = 0.8f,
                    Source = "docs/empty.md",
                    Content = "   "
                },
                new SearchResult
                {
                    ChunkId = "guide-local-run:0",
                    DocId = "guide-local-run",
                    Score = 0.7f,
                    Source = "docs/local-run.md",
                    Title = "Local Run",
                    Content = "Check the /health endpoint."
                }
            ]);

        var context = assembler.Assemble(retrieval);

        var citation = Assert.Single(context.Citations);
        Assert.Equal("guide-local-run", citation.DocId);
        Assert.Equal(2, context.RetrievedHitCount);
        Assert.DoesNotContain("docs/empty.md", context.Context);
    }
}

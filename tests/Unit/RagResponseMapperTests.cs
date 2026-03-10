using Api.Contracts;
using Api.Services;
using Api.Services.Mappers;

using Embeddings.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class RagResponseMapperTests
{
    [Fact]
    public void ToQueryResponse_MapsCitationsAndDebugMetadata()
    {
        var mapper = new RagResponseMapper();
        var response = mapper.ToQueryResponse(
            traceId: "trace-123",
            retrieval: new TextRetrievalResult(
                CollectionName: "knowledge_chunks",
                QueryText: "How do I check health locally?",
                Embedding: new EmbeddingDescriptor
                {
                    Provider = "Deterministic",
                    Model = "hashing-text-v1",
                    Dimension = 384,
                    SchemaVersion = "v1"
                },
                Hits: []),
            context: new RagContext(
                Context: "Context",
                Citations:
                [
                    new RagCitationDraft(
                        ChunkId: "guide-local-run:0",
                        DocId: "guide-local-run",
                        Source: "docs/local-run.md",
                        Title: "Local Run",
                        Section: "Verify Health",
                        Score: 0.91f,
                        Content: "Check the /health and /ready endpoints after starting AppHost.")
                ],
                RetrievedHitCount: 2),
            generation: new RagAnswerGenerationResult(
                Answer: "Check the /health and /ready endpoints after starting AppHost.",
                Descriptor: new RagAnswerGeneratorDescriptor("Deterministic", "grounded-answer-v1")),
            includeDebug: true);

        Assert.Equal("trace-123", response.TraceId);
        Assert.Equal("Check the /health and /ready endpoints after starting AppHost.", response.Answer);

        var citation = Assert.Single(response.Citations);
        Assert.Equal("guide-local-run:0", citation.ChunkId);
        Assert.Equal("guide-local-run", citation.DocId);
        Assert.Equal("docs/local-run.md", citation.Source);
        Assert.Equal("Verify Health", citation.Section);

        var debug = Assert.IsType<RagDebugResponse>(response.Debug);
        Assert.Equal("knowledge_chunks", debug.Collection);
        Assert.Equal("Deterministic", debug.EmbeddingProvider);
        Assert.Equal("hashing-text-v1", debug.EmbeddingModel);
        Assert.Equal("v1", debug.EmbeddingSchemaVersion);
        Assert.Equal("Deterministic", debug.AnswerProvider);
        Assert.Equal("grounded-answer-v1", debug.AnswerModel);
        Assert.Equal(2, debug.RetrievedHitCount);
    }
}

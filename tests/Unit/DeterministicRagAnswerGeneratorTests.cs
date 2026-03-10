using Api.Services;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class DeterministicRagAnswerGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsGroundedAnswerAndDescriptor()
    {
        var generator = new DeterministicRagAnswerGenerator();

        var result = await generator.GenerateAsync(
            new RagAnswerGenerationRequest
            {
                Question = "How do I check health and ready endpoints locally?",
                Context = """
                    [1] Source: docs/local-run.md
                    Title: Local Run
                    Content:
                    Check the /health and /ready endpoints after starting AppHost.
                    """,
                Citations =
                [
                    new RagCitationDraft(
                        ChunkId: "guide-local-run:0",
                        DocId: "guide-local-run",
                        Source: "docs/local-run.md",
                        Title: "Local Run",
                        Section: null,
                        Score: 0.91f,
                        Content: "Check the /health and /ready endpoints after starting AppHost.")
                ]
            },
            CancellationToken.None);

        Assert.Equal("Deterministic", result.Descriptor.Provider);
        Assert.Equal("grounded-answer-v1", result.Descriptor.Model);
        Assert.Contains("/health", result.Answer, StringComparison.Ordinal);
        Assert.Contains("/ready", result.Answer, StringComparison.Ordinal);
    }
}

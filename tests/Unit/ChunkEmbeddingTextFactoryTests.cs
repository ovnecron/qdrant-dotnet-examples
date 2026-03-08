using Api.Services.Ingestion;

using Embeddings.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class ChunkEmbeddingTextFactoryTests
{
    [Fact]
    public void CreateDocumentRequest_IncludesTitleSectionAndContent()
    {
        var factory = new ChunkEmbeddingTextFactory();
        var chunk = new MarkdownChunk
        {
            ChunkId = "guide-1:0:ABCD",
            ChunkIndex = 0,
            DocId = "guide-1",
            Source = "docs/guide.md",
            Title = "Local Run",
            Section = "Setup",
            Content = "Install .NET and Docker.",
            Checksum = "ABCDEF",
            Tags = ["tutorial"]
        };

        var request = factory.CreateDocumentRequest(chunk);

        Assert.Equal(EmbeddingKind.Document, request.Kind);
        Assert.Equal(
            "Title: Local Run\nSection: Setup\nContent:\nInstall .NET and Docker.",
            request.Text);
    }

    [Fact]
    public void CreateDocumentRequest_OmitsSection_WhenChunkHasNoSection()
    {
        var factory = new ChunkEmbeddingTextFactory();
        var chunk = new MarkdownChunk
        {
            ChunkId = "guide-1:0:ABCD",
            ChunkIndex = 0,
            DocId = "guide-1",
            Source = "docs/guide.md",
            Title = "Local Run",
            Content = "Run AppHost and wait.",
            Checksum = "ABCDEF",
            Tags = ["tutorial"]
        };

        var request = factory.CreateDocumentRequest(chunk);

        Assert.Equal(
            "Title: Local Run\nContent:\nRun AppHost and wait.",
            request.Text);
        Assert.DoesNotContain("Section:", request.Text, StringComparison.Ordinal);
    }
}

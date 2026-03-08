using Api.Contracts;
using Api.Services.Validation;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class MarkdownIngestRequestParserTests
{
    [Fact]
    public void TryParse_UsesDefaultCollectionAndChunking_AndNormalizesFields()
    {
        var parser = new MarkdownIngestRequestParser();
        var request = new MarkdownIngestRequest
        {
            DocId = " guide-1 ",
            SourceId = " docs/guide.md ",
            Title = " Local Run ",
            Markdown = " # Local Run\n\nRun AppHost and wait. ",
            Tags = ["tutorial", " Tutorial ", "setup", "  "],
            TenantId = " tenant-a "
        };

        var success = parser.TryParse(
            request,
            defaultCollectionName: "knowledge_chunks",
            out var command,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);
        Assert.NotNull(command);
        Assert.Equal("knowledge_chunks", command.CollectionName);
        Assert.Equal("guide-1", command.DocId);
        Assert.Equal("docs/guide.md", command.SourceId);
        Assert.Equal("Local Run", command.Title);
        Assert.Equal("# Local Run\n\nRun AppHost and wait.", command.Markdown);
        Assert.Equal(["tutorial", "setup"], command.Tags);
        Assert.Equal("tenant-a", command.TenantId);
        Assert.Equal(800, command.ChunkSize);
        Assert.Equal(120, command.ChunkOverlap);
    }

    [Fact]
    public void TryParse_ReturnsValidationErrors_ForMissingFieldsAndInvalidChunking()
    {
        var parser = new MarkdownIngestRequestParser();
        var request = new MarkdownIngestRequest
        {
            DocId = " ",
            SourceId = null,
            Markdown = " ",
            ChunkSize = 128,
            ChunkOverlap = 128
        };

        var success = parser.TryParse(
            request,
            defaultCollectionName: " ",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("collection"));
        Assert.True(errors.ContainsKey("docId"));
        Assert.True(errors.ContainsKey("sourceId"));
        Assert.True(errors.ContainsKey("markdown"));
        Assert.True(errors.ContainsKey("chunkOverlap"));
    }

    [Fact]
    public void TryParse_TreatsNullTagsAsEmpty()
    {
        var parser = new MarkdownIngestRequestParser();
        var request = new MarkdownIngestRequest
        {
            DocId = "guide-1",
            SourceId = "docs/guide.md",
            Markdown = "# Guide\n\nHello",
            Tags = null!
        };

        var success = parser.TryParse(
            request,
            defaultCollectionName: "knowledge_chunks",
            out var command,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);
        Assert.NotNull(command);
        Assert.Empty(command.Tags);
    }
}

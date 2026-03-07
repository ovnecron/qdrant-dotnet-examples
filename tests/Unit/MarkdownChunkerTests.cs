using Api.Services.Ingestion;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class MarkdownChunkerTests
{
    [Fact]
    public void Chunk_ReturnsStableChunkIdsAndOrder_ForSameInput()
    {
        var chunker = new MarkdownChunker();
        var document = new MarkdownDocument
        {
            DocId = "guide-1",
            Source = "docs/guide.md",
            Markdown = """
                # Guide

                This is the first paragraph.

                This is the second paragraph with some more content to force chunking.

                This is the third paragraph with even more content to keep the output deterministic.
                """
        };
        var options = new MarkdownChunkingOptions
        {
            ChunkSize = 70,
            ChunkOverlap = 10
        };

        var firstRun = chunker.Chunk(document, options);
        var secondRun = chunker.Chunk(document, options);

        Assert.Equal(firstRun.Select(static chunk => chunk.ChunkId), secondRun.Select(static chunk => chunk.ChunkId));
        Assert.Equal(firstRun.Select(static chunk => chunk.Content), secondRun.Select(static chunk => chunk.Content));
    }

    [Fact]
    public void Chunk_UsesHeadingHierarchy_ForTitleAndSection()
    {
        var chunker = new MarkdownChunker();
        var document = new MarkdownDocument
        {
            DocId = "local-run",
            Source = "docs/local-run.md",
            Markdown = """
                # Local Run

                Overview paragraph.

                ## Setup

                Install .NET and Docker.

                ### Troubleshooting

                Check ports and logs.
                """
        };

        var chunks = chunker.Chunk(
            document,
            new MarkdownChunkingOptions
            {
                ChunkSize = 200,
                ChunkOverlap = 20
            });

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, chunk => Assert.Equal("Local Run", chunk.Title));
        Assert.Null(chunks[0].Section);
        Assert.Equal("Setup", chunks[1].Section);
        Assert.Equal("Setup / Troubleshooting", chunks[2].Section);
    }

    [Fact]
    public void Chunk_SplitsLongSections_WithDeterministicOverlap()
    {
        var chunker = new MarkdownChunker();
        var sequence = string.Concat(Enumerable.Range(0, 140).Select(static index => (char)('A' + (index % 26))));
        var document = new MarkdownDocument
        {
            DocId = "overlap-doc",
            Source = "docs/overlap.md",
            Markdown = $"# Overlap\n\n{sequence}"
        };
        var options = new MarkdownChunkingOptions
        {
            ChunkSize = 60,
            ChunkOverlap = 10
        };

        var chunks = chunker.Chunk(document, options);

        Assert.True(chunks.Count >= 3);
        Assert.Equal(chunks[0].Content[^10..], chunks[1].Content[..10]);
        Assert.Equal(chunks[1].Content[^10..], chunks[2].Content[..10]);
    }

    [Fact]
    public void Chunk_Throws_WhenMarkdownIsEmpty()
    {
        var chunker = new MarkdownChunker();
        var document = new MarkdownDocument
        {
            DocId = "empty-doc",
            Source = "docs/empty.md",
            Markdown = "   "
        };

        var exception = Assert.Throws<ArgumentException>(() => chunker.Chunk(document));

        Assert.Contains("Markdown must be provided.", exception.Message);
    }
}

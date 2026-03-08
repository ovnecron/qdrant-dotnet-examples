using Api.Services.Mappers;

using VectorStore.Abstractions.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class VectorResponseMapperTests
{
    [Fact]
    public void ToDeleteResponse_MapsCollectionAndDeletedCount()
    {
        var mapper = new VectorResponseMapper();

        var response = mapper.ToDeleteResponse("knowledge_chunks", 2);

        Assert.Equal("knowledge_chunks", response.Collection);
        Assert.Equal(2, response.DeletedCount);
    }

    [Fact]
    public void ToRecordResponse_MapsAllRelevantFields()
    {
        var mapper = new VectorResponseMapper();
        var record = new VectorRecord
        {
            ChunkId = "doc-1:0",
            Vector = [0.12f, -0.09f, 0.31f],
            DocId = "doc-1",
            Source = "docs/intro.md",
            Title = "Introduction",
            Section = "Getting Started",
            Tags = ["intro", "setup"],
            Content = "Run AppHost and wait.",
            Checksum = "ABC123",
            CreatedAtUtc = DateTimeOffset.Parse("2026-03-06T09:15:00+00:00"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-03-06T09:20:00+00:00"),
            TenantId = "tenant-a",
            DocVersion = "doc-version-1",
            EmbeddingSchemaVersion = "v1"
        };

        var response = mapper.ToRecordResponse("knowledge_chunks", record);

        Assert.Equal("knowledge_chunks", response.Collection);
        Assert.Equal("doc-1:0", response.ChunkId);
        Assert.Equal([0.12f, -0.09f, 0.31f], response.Vector);
        Assert.Equal("doc-1", response.DocId);
        Assert.Equal("docs/intro.md", response.Source);
        Assert.Equal("Introduction", response.Title);
        Assert.Equal("Getting Started", response.Section);
        Assert.Equal(["intro", "setup"], response.Tags);
        Assert.Equal("Run AppHost and wait.", response.Content);
        Assert.Equal("ABC123", response.Checksum);
        Assert.Equal(DateTimeOffset.Parse("2026-03-06T09:15:00+00:00"), response.CreatedAtUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-03-06T09:20:00+00:00"), response.UpdatedAtUtc);
        Assert.Equal("tenant-a", response.TenantId);
        Assert.Equal("doc-version-1", response.DocVersion);
        Assert.Equal("v1", response.EmbeddingSchemaVersion);
    }
}

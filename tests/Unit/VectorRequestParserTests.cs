using System.Security.Cryptography;
using System.Text;

using Api.Contracts;
using Api.Services.Validation;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class VectorRequestParserTests
{
    [Fact]
    public void TryParseDeleteRequest_NormalizesAndDeduplicatesChunkIds()
    {
        var parser = new VectorRequestParser();
        var request = new VectorDeleteRequest
        {
            Collection = "  knowledge_chunks  ",
            ChunkIds = [" doc-1:0 ", "doc-1:0", "doc-1:1", "   "]
        };

        var success = parser.TryParseDeleteRequest(
            request,
            defaultCollectionName: "fallback_collection",
            out var command,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);

        var parsedCommand = Assert.IsType<DeleteVectorsCommand>(command);
        Assert.Equal("knowledge_chunks", parsedCommand.CollectionName);
        Assert.Equal(["doc-1:0", "doc-1:1"], parsedCommand.ChunkIds);
    }

    [Fact]
    public void TryParseDeleteRequest_ReturnsValidationError_WhenChunkIdsMissing()
    {
        var parser = new VectorRequestParser();
        var request = new VectorDeleteRequest
        {
            Collection = "knowledge_chunks",
            ChunkIds = []
        };

        var success = parser.TryParseDeleteRequest(
            request,
            defaultCollectionName: "knowledge_chunks",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("chunkIds"));
    }

    [Fact]
    public void TryParseUpsertRequest_ReturnsValidationError_WhenPointsMissing()
    {
        var parser = new VectorRequestParser();
        var request = new VectorUpsertRequest
        {
            Collection = "knowledge_chunks",
            Points = []
        };

        var success = parser.TryParseUpsertRequest(
            request,
            defaultCollectionName: "knowledge_chunks",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("points"));
    }

    [Fact]
    public void TryParseUpsertRequest_ComputesChecksumAndNormalizesFields()
    {
        var parser = new VectorRequestParser();
        var request = new VectorUpsertRequest
        {
            Collection = "  knowledge_chunks  ",
            Points =
            [
                new VectorPointRequest
                {
                    Id = " chunk-1 ",
                    Vector = [0.1f, 0.2f, 0.3f],
                    Payload = new VectorPayloadRequest
                    {
                        DocId = " doc-1 ",
                        Source = " docs/file.md ",
                        Tags = ["a", " A ", "b"],
                        Content = "  hello world  "
                    }
                }
            ]
        };

        var success = parser.TryParseUpsertRequest(
            request,
            defaultCollectionName: "fallback_collection",
            out var command,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);
        var parsedCommand = Assert.IsType<UpsertVectorsCommand>(command);
        Assert.Equal("knowledge_chunks", parsedCommand.CollectionName);

        var record = Assert.Single(parsedCommand.Records);
        Assert.Equal("chunk-1", record.ChunkId);
        Assert.Equal("doc-1", record.DocId);
        Assert.Equal("docs/file.md", record.Source);
        Assert.Equal("hello world", record.Content);
        Assert.Equal(["a", "b"], record.Tags);
        Assert.Equal(ComputeSha256("hello world"), record.Checksum);
    }

    private static string ComputeSha256(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}

using Api.Contracts;
using Api.Services.Validation;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class RagRequestParserTests
{
    [Fact]
    public void TryParseQueryRequest_UsesDefaultCollectionAndNormalizesFields()
    {
        var parser = new RagRequestParser();
        var request = new RagQueryRequest
        {
            Question = "  How do I check health locally?  ",
            TopK = 3,
            IncludeDebug = true,
            Filter = new VectorSearchFilterRequest
            {
                TagsAny = [" local ", "local", "tutorial"],
                TenantIdEquals = " tenant-a "
            }
        };

        var success = parser.TryParseQueryRequest(
            request,
            defaultCollectionName: "knowledge_chunks",
            out var command,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);

        var parsedCommand = Assert.IsType<RagQueryCommand>(command);
        Assert.Equal("knowledge_chunks", parsedCommand.CollectionName);
        Assert.Equal("How do I check health locally?", parsedCommand.Question);
        Assert.Equal(3, parsedCommand.TopK);
        Assert.True(parsedCommand.IncludeDebug);
        Assert.Equal(["local", "tutorial"], parsedCommand.Filter.TagsAny);
        Assert.Equal("tenant-a", parsedCommand.Filter.TenantIdEquals);
    }

    [Fact]
    public void TryParseQueryRequest_ReturnsValidationErrors_WhenRequestInvalid()
    {
        var parser = new RagRequestParser();
        var request = new RagQueryRequest
        {
            Collection = "knowledge_chunks",
            Question = "   ",
            TopK = 0
        };

        var success = parser.TryParseQueryRequest(
            request,
            defaultCollectionName: "knowledge_chunks",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("question"));
        Assert.True(errors.ContainsKey("topK"));
    }
}

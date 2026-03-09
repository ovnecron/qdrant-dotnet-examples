using Api.Contracts;
using Api.Services.Validation;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class SemanticSearchRequestParserTests
{
    [Fact]
    public void TryParseQueryRequest_UsesDefaultCollectionAndNormalizesFields()
    {
        var parser = new SemanticSearchRequestParser();
        var request = new SemanticSearchQueryRequest
        {
            QueryText = "  How do I run this locally?  ",
            TopK = 3,
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

        var parsedCommand = Assert.IsType<SemanticSearchQueryCommand>(command);
        Assert.Equal("knowledge_chunks", parsedCommand.CollectionName);
        Assert.Equal("How do I run this locally?", parsedCommand.QueryText);
        Assert.Equal(3, parsedCommand.TopK);
        Assert.Equal(["local", "tutorial"], parsedCommand.Filter.TagsAny);
        Assert.Equal("tenant-a", parsedCommand.Filter.TenantIdEquals);
    }

    [Fact]
    public void TryParseQueryRequest_ReturnsValidationErrors_WhenRequestInvalid()
    {
        var parser = new SemanticSearchRequestParser();
        var request = new SemanticSearchQueryRequest
        {
            Collection = "knowledge_chunks",
            QueryText = "   ",
            TopK = 0
        };

        var success = parser.TryParseQueryRequest(
            request,
            defaultCollectionName: "knowledge_chunks",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("queryText"));
        Assert.True(errors.ContainsKey("topK"));
    }
}

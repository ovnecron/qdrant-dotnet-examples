using Api.Contracts;
using Api.Services.Validation;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class AdminRequestParserTests
{
    [Fact]
    public void TryParseInitializeCollectionRequest_UsesFallbackCollectionAndDefaultDistance()
    {
        var parser = new AdminRequestParser();
        var request = new InitializeCollectionRequest
        {
            VectorSize = 3,
            PayloadIndexes = []
        };

        var success = parser.TryParseInitializeCollectionRequest(
            request,
            defaultCollectionName: "knowledge_chunks",
            out var definition,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);
        var parsedDefinition = Assert.IsType<VectorStore.Abstractions.Contracts.CollectionDefinition>(definition);
        Assert.Equal("knowledge_chunks", parsedDefinition.Name);
        Assert.Equal(3, parsedDefinition.VectorSize);
        Assert.Equal(VectorStore.Abstractions.Contracts.VectorDistance.Cosine, parsedDefinition.Distance);
        Assert.Contains("tenantId", parsedDefinition.PayloadIndexes);
    }

    [Fact]
    public void TryParseInitializeCollectionRequest_ReturnsValidationError_ForInvalidDistance()
    {
        var parser = new AdminRequestParser();
        var request = new InitializeCollectionRequest
        {
            CollectionName = "knowledge_chunks",
            VectorSize = 3,
            Distance = "WrongDistance"
        };

        var success = parser.TryParseInitializeCollectionRequest(
            request,
            defaultCollectionName: "fallback",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("distance"));
    }
}

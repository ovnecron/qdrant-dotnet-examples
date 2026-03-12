using Api.Contracts;
using Api.Services.Validation;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class AnomalyRequestParserTests
{
    [Fact]
    public void TryParseScoreRequest_UsesDefaultCollectionAndDefaultThreshold()
    {
        var parser = new AnomalyRequestParser();
        var request = new AnomalyScoreRequest
        {
            Vector = [0.12f, -0.09f, 0.31f],
            TopK = 3,
            IncludeNeighbors = false,
            Filter = new VectorSearchFilterRequest
            {
                TagsAny = [" baseline ", "baseline", "normal"],
                TenantIdEquals = " tenant-a "
            }
        };

        var success = parser.TryParseScoreRequest(
            request,
            defaultCollectionName: "anomaly_vectors",
            out var command,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);

        var parsedCommand = Assert.IsType<AnomalyScoreCommand>(command);
        Assert.Equal("anomaly_vectors", parsedCommand.CollectionName);
        Assert.Equal([0.12f, -0.09f, 0.31f], parsedCommand.Vector);
        Assert.Equal(3, parsedCommand.TopK);
        Assert.Equal(0.35f, parsedCommand.Threshold);
        Assert.Equal(["baseline", "normal"], parsedCommand.Filter.TagsAny);
        Assert.Equal("tenant-a", parsedCommand.Filter.TenantIdEquals);
        Assert.False(parsedCommand.IncludeNeighbors);
    }

    [Fact]
    public void TryParseScoreRequest_ReturnsValidationErrors_WhenRequestInvalid()
    {
        var parser = new AnomalyRequestParser();
        var request = new AnomalyScoreRequest
        {
            Collection = "anomaly_vectors",
            Vector = [],
            TopK = 0,
            Threshold = 1.5f
        };

        var success = parser.TryParseScoreRequest(
            request,
            defaultCollectionName: "anomaly_vectors",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("vector"));
        Assert.True(errors.ContainsKey("topK"));
        Assert.True(errors.ContainsKey("threshold"));
    }
}

using Api.Contracts;
using Api.Services.Validation;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class TextAnomalyRequestParserTests
{
    [Fact]
    public void TryParseScoreRequest_UsesDefaultCollectionAndDefaultThreshold()
    {
        var parser = new TextAnomalyRequestParser();
        var request = new TextAnomalyScoreRequest
        {
            Text = "  Check /health and /ready after startup.  ",
            TopK = 3,
            IncludeNeighbors = false,
            IncludeDebug = true,
            Filter = new VectorSearchFilterRequest
            {
                TagsAny = [" ops ", "ops", "health"],
                TenantIdEquals = " tenant-a "
            }
        };

        var success = parser.TryParseScoreRequest(
            request,
            defaultCollectionName: "knowledge_chunks_text_anomaly",
            out var command,
            out var errors);

        Assert.True(success);
        Assert.Empty(errors);

        var parsedCommand = Assert.IsType<TextAnomalyScoreCommand>(command);
        Assert.Equal("knowledge_chunks_text_anomaly", parsedCommand.CollectionName);
        Assert.Equal("Check /health and /ready after startup.", parsedCommand.Text);
        Assert.Equal(3, parsedCommand.TopK);
        Assert.Equal(0.35f, parsedCommand.Threshold);
        Assert.Equal(["ops", "health"], parsedCommand.Filter.TagsAny);
        Assert.Equal("tenant-a", parsedCommand.Filter.TenantIdEquals);
        Assert.False(parsedCommand.IncludeNeighbors);
        Assert.True(parsedCommand.IncludeDebug);
    }

    [Fact]
    public void TryParseScoreRequest_ReturnsValidationErrors_WhenRequestInvalid()
    {
        var parser = new TextAnomalyRequestParser();
        var request = new TextAnomalyScoreRequest
        {
            Collection = "knowledge_chunks_text_anomaly",
            Text = "   ",
            TopK = 0,
            Threshold = -0.1f
        };

        var success = parser.TryParseScoreRequest(
            request,
            defaultCollectionName: "knowledge_chunks_text_anomaly",
            out _,
            out var errors);

        Assert.False(success);
        Assert.True(errors.ContainsKey("text"));
        Assert.True(errors.ContainsKey("topK"));
        Assert.True(errors.ContainsKey("threshold"));
    }
}

using Api.Services;

using VectorStore.Abstractions.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class CosineAnomalyScoreCalculatorTests
{
    [Fact]
    public void Compute_ReturnsLowAnomalyScore_ForHighSimilarityNeighbors()
    {
        var calculator = new CosineAnomalyScoreCalculator();

        var result = calculator.Compute(
            [
                CreateNeighbor("baseline-1", 0.95f),
                CreateNeighbor("baseline-2", 0.91f),
                CreateNeighbor("baseline-3", 0.89f)
            ],
            threshold: 0.35f);

        Assert.False(result.IsAnomalous);
        Assert.InRange(result.AnomalyScore, 0f, 0.15f);
        Assert.Equal(0.95f, result.MaxNeighborSimilarity);
    }

    [Fact]
    public void Compute_ReturnsHighAnomalyScore_ForLowSimilarityNeighbors()
    {
        var calculator = new CosineAnomalyScoreCalculator();

        var result = calculator.Compute(
            [
                CreateNeighbor("baseline-1", 0.18f),
                CreateNeighbor("baseline-2", 0.12f),
                CreateNeighbor("baseline-3", 0.08f)
            ],
            threshold: 0.35f);

        Assert.True(result.IsAnomalous);
        Assert.InRange(result.AnomalyScore, 0.8f, 1f);
        Assert.Equal(0.18f, result.MaxNeighborSimilarity);
    }

    private static SearchResult CreateNeighbor(string chunkId, float score)
    {
        return new SearchResult
        {
            ChunkId = chunkId,
            DocId = "baseline",
            Score = score,
            Source = "fixtures/anomaly-baseline.json",
            Content = "baseline"
        };
    }
}

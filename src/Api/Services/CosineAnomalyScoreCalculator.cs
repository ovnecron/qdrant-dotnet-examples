using VectorStore.Abstractions.Contracts;

namespace Api.Services;

internal sealed class CosineAnomalyScoreCalculator : IAnomalyScoreCalculator
{
    public AnomalyScoreComputation Compute(
        IReadOnlyList<SearchResult> neighbors,
        float threshold)
    {
        ArgumentNullException.ThrowIfNull(neighbors);

        if (neighbors.Count == 0)
        {
            throw new ArgumentException("At least one neighbor is required.", nameof(neighbors));
        }

        var meanNeighborSimilarity = neighbors.Average(static neighbor => neighbor.Score);
        var maxNeighborSimilarity = neighbors.Max(static neighbor => neighbor.Score);
        var anomalyScore = Math.Clamp(1f - meanNeighborSimilarity, 0f, 1f);

        return new AnomalyScoreComputation(
            anomalyScore,
            meanNeighborSimilarity,
            maxNeighborSimilarity,
            anomalyScore >= threshold);
    }
}

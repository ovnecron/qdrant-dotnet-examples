namespace Api.Services;

internal sealed record AnomalyScoreComputation(
    float AnomalyScore,
    float MeanNeighborSimilarity,
    float MaxNeighborSimilarity,
    bool IsAnomalous);

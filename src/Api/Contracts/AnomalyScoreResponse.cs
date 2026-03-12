namespace Api.Contracts;

public sealed record AnomalyScoreResponse
{
    public required string TraceId { get; init; }

    public required string Collection { get; init; }

    public required float AnomalyScore { get; init; }

    public required float Threshold { get; init; }

    public required bool IsAnomalous { get; init; }

    public required int NeighborCount { get; init; }

    public required float MeanNeighborSimilarity { get; init; }

    public required float MaxNeighborSimilarity { get; init; }

    public IReadOnlyList<AnomalyNeighborResponse> Neighbors { get; init; } = [];
}

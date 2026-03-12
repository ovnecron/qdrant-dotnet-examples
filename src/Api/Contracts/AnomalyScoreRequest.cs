namespace Api.Contracts;

public sealed record AnomalyScoreRequest
{
    public string? Collection { get; init; }

    public IReadOnlyList<float> Vector { get; init; } = [];

    public int TopK { get; init; } = 5;

    public float? Threshold { get; init; }

    public VectorSearchFilterRequest? Filter { get; init; }

    public bool IncludeNeighbors { get; init; } = true;
}

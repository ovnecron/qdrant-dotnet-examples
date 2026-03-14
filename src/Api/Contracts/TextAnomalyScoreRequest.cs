namespace Api.Contracts;

public sealed record TextAnomalyScoreRequest
{
    public string? Collection { get; init; }

    public string? Text { get; init; }

    public int TopK { get; init; } = 5;

    public float? Threshold { get; init; }

    public VectorSearchFilterRequest? Filter { get; init; }

    public bool IncludeNeighbors { get; init; } = true;

    public bool IncludeDebug { get; init; }
}

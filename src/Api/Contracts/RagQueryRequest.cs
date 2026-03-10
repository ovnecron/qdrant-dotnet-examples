namespace Api.Contracts;

public sealed record RagQueryRequest
{
    public string? Collection { get; init; }

    public string? Question { get; init; }

    public int TopK { get; init; } = 5;

    public float? MinScore { get; init; }

    public VectorSearchFilterRequest? Filter { get; init; }

    public bool IncludeDebug { get; init; }
}

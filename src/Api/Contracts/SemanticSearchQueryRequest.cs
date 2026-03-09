namespace Api.Contracts;

public sealed record SemanticSearchQueryRequest
{
    public string? Collection { get; init; }

    public string? QueryText { get; init; }

    public int TopK { get; init; } = 5;

    public float? MinScore { get; init; }

    public VectorSearchFilterRequest? Filter { get; init; }
}

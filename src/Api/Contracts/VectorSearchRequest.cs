namespace Api.Contracts;

public sealed record VectorSearchRequest
{
    public string? Collection { get; init; }

    public IReadOnlyList<float> QueryVector { get; init; } = [];

    public int TopK { get; init; } = 5;

    public float? MinScore { get; init; }

    public VectorSearchFilterRequest? Filter { get; init; }
}

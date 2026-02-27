namespace VectorStore.Abstractions.Contracts;

public sealed record SearchRequest
{
    public required string CollectionName { get; init; }

    public required IReadOnlyList<float> QueryVector { get; init; }

    public int TopK { get; init; } = 5;

    public float? MinScore { get; init; }

    public SearchFilter Filter { get; init; } = new();
}

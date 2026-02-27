namespace VectorStore.Abstractions.Contracts;

public sealed record CollectionInitResult
{
    public required string CollectionName { get; init; }

    public required bool Created { get; init; }
}

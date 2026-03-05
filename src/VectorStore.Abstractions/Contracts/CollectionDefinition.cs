namespace VectorStore.Abstractions.Contracts;

public sealed record CollectionDefinition
{
    public required string Name { get; init; }

    public required int VectorSize { get; init; }

    public VectorDistance Distance { get; init; } = VectorDistance.Cosine;

    public IReadOnlyList<string> PayloadIndexes { get; init; } = [];
}

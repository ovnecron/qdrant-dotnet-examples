namespace Api.Contracts;

public sealed record InitializeCollectionRequest
{
    public string? CollectionName { get; init; }

    public int? VectorSize { get; init; }

    public string? Distance { get; init; }

    public string? VectorName { get; init; }

    public IReadOnlyList<string>? PayloadIndexes { get; init; }
}

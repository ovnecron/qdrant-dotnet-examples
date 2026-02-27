namespace Api.Contracts;

public sealed record InitializeCollectionResponse
{
    public required string CollectionName { get; init; }

    public required bool Created { get; init; }
}

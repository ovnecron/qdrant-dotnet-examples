namespace Api.Contracts;

public sealed record VectorUpsertResponse
{
    public required string Collection { get; init; }

    public required int UpsertedCount { get; init; }
}

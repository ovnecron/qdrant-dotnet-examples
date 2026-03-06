namespace Api.Contracts;

public sealed record VectorDeleteResponse
{
    public required string Collection { get; init; }

    public required int DeletedCount { get; init; }
}

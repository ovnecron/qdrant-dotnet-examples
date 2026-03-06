namespace Api.Contracts;

public sealed record VectorDeleteRequest
{
    public string? Collection { get; init; }

    public IReadOnlyList<string> ChunkIds { get; init; } = [];
}

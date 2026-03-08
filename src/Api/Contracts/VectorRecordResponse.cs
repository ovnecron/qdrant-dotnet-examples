namespace Api.Contracts;

public sealed record VectorRecordResponse
{
    public required string Collection { get; init; }

    public required string ChunkId { get; init; }

    public required IReadOnlyList<float> Vector { get; init; }

    public required string DocId { get; init; }

    public required string Source { get; init; }

    public string? Title { get; init; }

    public string? Section { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public required string Content { get; init; }

    public required string Checksum { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required DateTimeOffset UpdatedAtUtc { get; init; }

    public string? TenantId { get; init; }

    public string? DocVersion { get; init; }

    public string? EmbeddingSchemaVersion { get; init; }
}

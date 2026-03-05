namespace Api.Contracts;

public sealed record VectorPayloadRequest
{
    public string? DocId { get; init; }

    public string? Source { get; init; }

    public string? Title { get; init; }

    public string? Section { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string? Content { get; init; }

    public string? Checksum { get; init; }

    public DateTimeOffset? CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public string? TenantId { get; init; }

    public string? EmbeddingSchemaVersion { get; init; }
}

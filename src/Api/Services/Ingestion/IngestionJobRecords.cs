namespace Api.Services.Ingestion;

internal sealed record QueuedMarkdownIngestionJob
{
    public required string JobId { get; init; }

    public required string CollectionName { get; init; }

    public required string DocId { get; init; }

    public required string DocVersion { get; init; }

    public required string SourceId { get; init; }

    public string? Title { get; init; }

    public required string Markdown { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string? TenantId { get; init; }

    public required int ChunkSize { get; init; }

    public required int ChunkOverlap { get; init; }

    public required DateTimeOffset AcceptedAtUtc { get; init; }

    public required string EmbeddingModel { get; init; }

    public required string EmbeddingSchemaVersion { get; init; }
}

internal sealed record IngestionJobResult(int ChunkCount, int UpsertedCount);

internal sealed record IngestionJobError(string Code, string Message);

internal sealed record IngestionJobRecord
{
    public required string JobId { get; init; }

    public required string CollectionName { get; init; }

    public required string DocId { get; init; }

    public required string DocVersion { get; init; }

    public required DateTimeOffset AcceptedAtUtc { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public required string EmbeddingModel { get; init; }

    public required string EmbeddingSchemaVersion { get; init; }

    public required IngestionJobStatus Status { get; init; }

    public IngestionJobResult? Result { get; init; }

    public IngestionJobError? Error { get; init; }
}

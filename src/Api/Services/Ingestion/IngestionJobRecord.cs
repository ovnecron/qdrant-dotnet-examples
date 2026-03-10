namespace Api.Services.Ingestion;

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

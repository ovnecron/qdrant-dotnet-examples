namespace Api.Contracts;

public sealed record IngestJobStatusResponse
{
    public required string JobId { get; init; }

    public required string Status { get; init; }

    public required string Collection { get; init; }

    public required string DocId { get; init; }

    public required string DocVersion { get; init; }

    public required DateTimeOffset AcceptedAtUtc { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public required string EmbeddingModel { get; init; }

    public required string EmbeddingSchemaVersion { get; init; }

    public IngestJobResultResponse? Result { get; init; }

    public IngestJobErrorResponse? Error { get; init; }
}

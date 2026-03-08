namespace Api.Contracts;

public sealed record MarkdownIngestAcceptedResponse
{
    public required string JobId { get; init; }

    public required string Collection { get; init; }

    public required string DocId { get; init; }

    public required string DocVersion { get; init; }

    public required DateTimeOffset AcceptedAtUtc { get; init; }

    public required string EmbeddingModel { get; init; }

    public required string EmbeddingSchemaVersion { get; init; }
}

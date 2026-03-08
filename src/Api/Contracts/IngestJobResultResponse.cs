namespace Api.Contracts;

public sealed record IngestJobResultResponse
{
    public required int ChunkCount { get; init; }

    public required int UpsertedCount { get; init; }
}

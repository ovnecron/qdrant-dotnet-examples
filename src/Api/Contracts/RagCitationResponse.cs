namespace Api.Contracts;

public sealed record RagCitationResponse
{
    public required string ChunkId { get; init; }

    public required string DocId { get; init; }

    public required string Source { get; init; }

    public string? Title { get; init; }

    public string? Section { get; init; }

    public required float Score { get; init; }
}

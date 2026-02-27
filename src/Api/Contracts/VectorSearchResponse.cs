namespace Api.Contracts;

public sealed record VectorSearchResponse
{
    public required string TraceId { get; init; }

    public required IReadOnlyList<VectorSearchHitResponse> Hits { get; init; }
}

namespace Api.Contracts;

public sealed record RagQueryResponse
{
    public required string TraceId { get; init; }

    public required string Answer { get; init; }

    public required IReadOnlyList<RagCitationResponse> Citations { get; init; }

    public RagDebugResponse? Debug { get; init; }
}

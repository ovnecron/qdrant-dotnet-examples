namespace Api.Contracts;

public sealed record VectorUpsertRequest
{
    public string? Collection { get; init; }

    public IReadOnlyList<VectorPointRequest> Points { get; init; } = [];
}

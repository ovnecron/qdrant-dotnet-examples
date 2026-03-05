namespace Api.Contracts;

public sealed record VectorPointRequest
{
    public string? Id { get; init; }

    public IReadOnlyList<float> Vector { get; init; } = [];

    public VectorPayloadRequest? Payload { get; init; }
}

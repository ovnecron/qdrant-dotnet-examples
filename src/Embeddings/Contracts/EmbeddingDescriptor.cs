namespace Embeddings.Contracts;

public sealed record EmbeddingDescriptor
{
    public required string Provider { get; init; }

    public required string Model { get; init; }

    public required int Dimension { get; init; }

    public required string SchemaVersion { get; init; }
}

namespace Embeddings.Contracts;

public sealed record TextEmbeddingRequest
{
    public required string Text { get; init; }

    public required EmbeddingKind Kind { get; init; }
}

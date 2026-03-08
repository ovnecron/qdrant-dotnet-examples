namespace Embeddings.Contracts;

public sealed record TextEmbeddingResult
{
    public required string Text { get; init; }

    public required EmbeddingKind Kind { get; init; }

    public required IReadOnlyList<float> Vector { get; init; }

    public required EmbeddingDescriptor Descriptor { get; init; }
}

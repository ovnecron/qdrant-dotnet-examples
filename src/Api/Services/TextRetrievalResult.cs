using Embeddings.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services;

internal sealed record TextRetrievalResult(
    string CollectionName,
    string QueryText,
    EmbeddingDescriptor Embedding,
    IReadOnlyList<SearchResult> Hits);

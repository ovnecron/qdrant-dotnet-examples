using VectorStore.Abstractions.Contracts;

namespace Api.Services;

internal sealed record TextRetrievalRequest(
    string CollectionName,
    string QueryText,
    int TopK,
    float? MinScore,
    SearchFilter Filter);

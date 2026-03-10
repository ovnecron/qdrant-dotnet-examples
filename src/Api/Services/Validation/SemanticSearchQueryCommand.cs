using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal sealed record SemanticSearchQueryCommand(
    string CollectionName,
    string QueryText,
    int TopK,
    float? MinScore,
    SearchFilter Filter);

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal sealed record RagQueryCommand(
    string CollectionName,
    string Question,
    int TopK,
    float? MinScore,
    SearchFilter Filter,
    bool IncludeDebug);

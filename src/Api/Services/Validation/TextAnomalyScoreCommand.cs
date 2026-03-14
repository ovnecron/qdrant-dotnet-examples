using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal sealed record TextAnomalyScoreCommand(
    string CollectionName,
    string Text,
    int TopK,
    float Threshold,
    SearchFilter Filter,
    bool IncludeNeighbors,
    bool IncludeDebug);

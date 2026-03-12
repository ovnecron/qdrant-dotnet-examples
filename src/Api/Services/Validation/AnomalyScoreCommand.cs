using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal sealed record AnomalyScoreCommand(
    string CollectionName,
    IReadOnlyList<float> Vector,
    int TopK,
    float Threshold,
    SearchFilter Filter,
    bool IncludeNeighbors);

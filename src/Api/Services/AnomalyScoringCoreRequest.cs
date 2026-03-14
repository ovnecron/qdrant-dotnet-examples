using VectorStore.Abstractions.Contracts;

namespace Api.Services;

internal sealed record AnomalyScoringCoreRequest(
    string CollectionName,
    IReadOnlyList<float> Vector,
    int TopK,
    float Threshold,
    SearchFilter Filter);

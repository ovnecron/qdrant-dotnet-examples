using VectorStore.Abstractions.Contracts;

namespace Api.Services;

internal sealed record AnomalyScoringCoreResult(
    IReadOnlyList<SearchResult> Neighbors,
    AnomalyScoreComputation Computation);

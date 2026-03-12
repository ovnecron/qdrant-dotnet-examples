using VectorStore.Abstractions.Contracts;

namespace Api.Services;

internal interface IAnomalyScoreCalculator
{
    AnomalyScoreComputation Compute(
        IReadOnlyList<SearchResult> neighbors,
        float threshold);
}

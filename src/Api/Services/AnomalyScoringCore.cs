using Api.Services.Results;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;

namespace Api.Services;

internal sealed class AnomalyScoringCore : IAnomalyScoringCore
{
    private readonly IAnomalyScoreCalculator _anomalyScoreCalculator;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly IVectorStoreClient _vectorStoreClient;

    public AnomalyScoringCore(
        IVectorStoreClient vectorStoreClient,
        IAnomalyScoreCalculator anomalyScoreCalculator,
        IServiceResultExecutor resultExecutor)
    {
        _vectorStoreClient = vectorStoreClient ?? throw new ArgumentNullException(nameof(vectorStoreClient));
        _anomalyScoreCalculator = anomalyScoreCalculator ?? throw new ArgumentNullException(nameof(anomalyScoreCalculator));
        _resultExecutor = resultExecutor ?? throw new ArgumentNullException(nameof(resultExecutor));
    }

    public async Task<ServiceResult<AnomalyScoringCoreResult>> ScoreAsync(
        AnomalyScoringCoreRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchResult = await _resultExecutor.ExecuteAsync(
            () => _vectorStoreClient.SearchAsync(
                new SearchRequest
                {
                    CollectionName = request.CollectionName,
                    QueryVector = request.Vector.ToArray(),
                    TopK = request.TopK,
                    Filter = request.Filter
                },
                cancellationToken),
            unexpectedTitle: "Anomaly scoring failed");

        if (searchResult.Failure is not null)
        {
            return ServiceResult<AnomalyScoringCoreResult>.Failed(searchResult.Failure);
        }

        var neighbors = searchResult.Value;
        if (neighbors is null)
        {
            return ServiceResult<AnomalyScoringCoreResult>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Anomaly scoring failed",
                    "No neighbor payload was returned."));
        }

        if (neighbors.Count == 0)
        {
            return ServiceResult<AnomalyScoringCoreResult>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.InsufficientAnomalyBaseline,
                    "Insufficient anomaly baseline",
                    "No sufficiently similar baseline neighbors were found for the requested vector."));
        }

        var computation = _anomalyScoreCalculator.Compute(neighbors, request.Threshold);

        return ServiceResult<AnomalyScoringCoreResult>.Success(
            new AnomalyScoringCoreResult(neighbors, computation));
    }
}

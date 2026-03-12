using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class AnomalyEndpointService : IAnomalyEndpointService
{
    private readonly IAnomalyScoreCalculator _anomalyScoreCalculator;
    private readonly string _defaultCollectionName;
    private readonly IAnomalyRequestParser _requestParser;
    private readonly IAnomalyResponseMapper _responseMapper;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly IVectorStoreClient _vectorStoreClient;

    public AnomalyEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        IVectorStoreClient vectorStoreClient,
        IAnomalyRequestParser requestParser,
        IAnomalyScoreCalculator anomalyScoreCalculator,
        IAnomalyResponseMapper responseMapper,
        IServiceResultExecutor resultExecutor)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _vectorStoreClient = vectorStoreClient ?? throw new ArgumentNullException(nameof(vectorStoreClient));
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _anomalyScoreCalculator = anomalyScoreCalculator ?? throw new ArgumentNullException(nameof(anomalyScoreCalculator));
        _responseMapper = responseMapper ?? throw new ArgumentNullException(nameof(responseMapper));
        _resultExecutor = resultExecutor ?? throw new ArgumentNullException(nameof(resultExecutor));
    }

    public async Task<ServiceResult<AnomalyScoreResponse>> ScoreAsync(
        AnomalyScoreRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseScoreRequest(
                request,
                _defaultCollectionName,
                out var command,
                out var errors))
        {
            return ServiceResult<AnomalyScoreResponse>.Validation(errors);
        }

        var searchResult = await _resultExecutor.ExecuteAsync(
            () => _vectorStoreClient.SearchAsync(
                new SearchRequest
                {
                    CollectionName = command.CollectionName,
                    QueryVector = command.Vector.ToArray(),
                    TopK = command.TopK,
                    Filter = command.Filter
                },
                cancellationToken),
            unexpectedTitle: "Anomaly scoring failed");

        if (searchResult.Failure is not null)
        {
            return ServiceResult<AnomalyScoreResponse>.Failed(searchResult.Failure);
        }

        var neighbors = searchResult.Value;
        if (neighbors is null)
        {
            return ServiceResult<AnomalyScoreResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Anomaly scoring failed",
                    "No neighbor payload was returned."));
        }

        if (neighbors.Count == 0)
        {
            return ServiceResult<AnomalyScoreResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.InsufficientAnomalyBaseline,
                    "Insufficient anomaly baseline",
                    "No sufficiently similar baseline neighbors were found for the requested vector."));
        }

        var computation = _anomalyScoreCalculator.Compute(neighbors, command.Threshold);

        return ServiceResult<AnomalyScoreResponse>.Success(
            _responseMapper.ToScoreResponse(
                traceId,
                command.CollectionName,
                command.IncludeNeighbors,
                neighbors,
                command.Threshold,
                computation));
    }
}

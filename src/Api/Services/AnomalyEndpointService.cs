using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Microsoft.Extensions.Options;

using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class AnomalyEndpointService : IAnomalyEndpointService
{
    private readonly IAnomalyScoringCore _anomalyScoringCore;
    private readonly string _defaultCollectionName;
    private readonly IAnomalyRequestParser _requestParser;
    private readonly IAnomalyResponseMapper _responseMapper;

    public AnomalyEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        IAnomalyScoringCore anomalyScoringCore,
        IAnomalyRequestParser requestParser,
        IAnomalyResponseMapper responseMapper)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _anomalyScoringCore = anomalyScoringCore ?? throw new ArgumentNullException(nameof(anomalyScoringCore));
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _responseMapper = responseMapper ?? throw new ArgumentNullException(nameof(responseMapper));
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

        var scoringResult = await _anomalyScoringCore.ScoreAsync(
            new AnomalyScoringCoreRequest(
                command.CollectionName,
                command.Vector,
                command.TopK,
                command.Threshold,
                command.Filter),
            cancellationToken);

        if (scoringResult.Failure is not null)
        {
            return ServiceResult<AnomalyScoreResponse>.Failed(scoringResult.Failure);
        }

        var score = scoringResult.Value;
        if (score is null)
        {
            return ServiceResult<AnomalyScoreResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Anomaly scoring failed",
                    "No anomaly score result was returned."));
        }

        return ServiceResult<AnomalyScoreResponse>.Success(
            _responseMapper.ToScoreResponse(
                traceId,
                command.CollectionName,
                command.IncludeNeighbors,
                score.Neighbors,
                command.Threshold,
                score.Computation));
    }
}

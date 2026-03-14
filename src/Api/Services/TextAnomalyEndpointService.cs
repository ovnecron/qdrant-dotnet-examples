using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Embeddings.Contracts;
using Embeddings.Interfaces;

using Microsoft.Extensions.Options;

using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class TextAnomalyEndpointService : ITextAnomalyEndpointService
{
    private readonly string _defaultCollectionName;
    private readonly ITextEmbeddingClient _embeddingClient;
    private readonly IAnomalyScoringCore _anomalyScoringCore;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly ITextAnomalyRequestParser _requestParser;
    private readonly ITextAnomalyResponseMapper _responseMapper;

    public TextAnomalyEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        ITextEmbeddingClient embeddingClient,
        IAnomalyScoringCore anomalyScoringCore,
        ITextAnomalyRequestParser requestParser,
        ITextAnomalyResponseMapper responseMapper,
        IServiceResultExecutor resultExecutor)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _anomalyScoringCore = anomalyScoringCore ?? throw new ArgumentNullException(nameof(anomalyScoringCore));
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _responseMapper = responseMapper ?? throw new ArgumentNullException(nameof(responseMapper));
        _resultExecutor = resultExecutor ?? throw new ArgumentNullException(nameof(resultExecutor));
    }

    public async Task<ServiceResult<TextAnomalyScoreResponse>> ScoreAsync(
        TextAnomalyScoreRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseScoreRequest(
                request,
                _defaultCollectionName,
                out var command,
                out var errors))
        {
            return ServiceResult<TextAnomalyScoreResponse>.Validation(errors);
        }

        var embeddingResult = await _resultExecutor.ExecuteAsync(
            () => _embeddingClient.EmbedAsync(
                new TextEmbeddingRequest
                {
                    Text = command.Text,
                    Kind = EmbeddingKind.Query
                },
                cancellationToken),
            unexpectedTitle: "Text anomaly scoring failed");

        if (embeddingResult.Failure is not null)
        {
            return ServiceResult<TextAnomalyScoreResponse>.Failed(embeddingResult.Failure);
        }

        var embedding = embeddingResult.Value;
        if (embedding is null)
        {
            return ServiceResult<TextAnomalyScoreResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Text anomaly scoring failed",
                    "No embedding result was returned."));
        }

        var scoringResult = await _anomalyScoringCore.ScoreAsync(
            new AnomalyScoringCoreRequest(
                command.CollectionName,
                embedding.Vector.ToArray(),
                command.TopK,
                command.Threshold,
                command.Filter),
            cancellationToken);

        if (scoringResult.Failure is not null)
        {
            return ServiceResult<TextAnomalyScoreResponse>.Failed(scoringResult.Failure);
        }

        var score = scoringResult.Value;
        if (score is null)
        {
            return ServiceResult<TextAnomalyScoreResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Text anomaly scoring failed",
                    "No anomaly score result was returned."));
        }

        return ServiceResult<TextAnomalyScoreResponse>.Success(
            _responseMapper.ToScoreResponse(
                traceId,
                command.CollectionName,
                command.Text,
                command.IncludeNeighbors,
                command.IncludeDebug,
                embedding.Descriptor,
                score.Neighbors,
                command.Threshold,
                score.Computation));
    }
}

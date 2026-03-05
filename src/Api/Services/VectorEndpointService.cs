using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class VectorEndpointService : IVectorEndpointService
{
    private readonly string _defaultCollectionName;
    private readonly IVectorRequestParser _requestParser;
    private readonly IVectorResponseMapper _responseMapper;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly IVectorStoreClient _vectorStoreClient;

    public VectorEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        IVectorStoreClient vectorStoreClient,
        IVectorRequestParser requestParser,
        IVectorResponseMapper responseMapper,
        IServiceResultExecutor resultExecutor)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _vectorStoreClient = vectorStoreClient ?? throw new ArgumentNullException(nameof(vectorStoreClient));
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _responseMapper = responseMapper ?? throw new ArgumentNullException(nameof(responseMapper));
        _resultExecutor = resultExecutor ?? throw new ArgumentNullException(nameof(resultExecutor));
    }

    public async Task<ServiceResult<VectorUpsertResponse>> UpsertAsync(
        VectorUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseUpsertRequest(
                request,
                _defaultCollectionName,
                out var command,
                out var errors))
        {
            return ServiceResult<VectorUpsertResponse>.Validation(errors);
        }

        return await _resultExecutor.ExecuteAsync(
                async () =>
                {
                    await _vectorStoreClient
                        .UpsertAsync(command.CollectionName, command.Records, cancellationToken)
                        .ConfigureAwait(false);

                    return (
                        Value: _responseMapper.ToUpsertResponse(
                            command.CollectionName,
                            command.Records.Count),
                        IsCreated: true);
                },
                unexpectedTitle: "Vector upsert failed")
            .ConfigureAwait(false);
    }

    public async Task<ServiceResult<VectorSearchResponse>> SearchAsync(
        VectorSearchRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseSearchRequest(
                request,
                _defaultCollectionName,
                out var searchRequest,
                out var errors))
        {
            return ServiceResult<VectorSearchResponse>.Validation(errors);
        }

        return await _resultExecutor.ExecuteAsync(
                async () =>
                {
                    var hits = await _vectorStoreClient
                        .SearchAsync(searchRequest, cancellationToken)
                        .ConfigureAwait(false);

                    return _responseMapper.ToSearchResponse(traceId, hits);
                },
                unexpectedTitle: "Vector search failed")
            .ConfigureAwait(false);
    }
}

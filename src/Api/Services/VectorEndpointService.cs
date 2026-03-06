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

    public async Task<ServiceResult<VectorDeleteResponse>> DeleteAsync(
        VectorDeleteRequest request,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseDeleteRequest(
                request,
                _defaultCollectionName,
                out var command,
                out var errors))
        {
            return ServiceResult<VectorDeleteResponse>.Validation(errors);
        }

        return await _resultExecutor.ExecuteAsync(
                async () =>
                {
                    var deletedCount = await _vectorStoreClient
                        .DeleteAsync(command.CollectionName, command.ChunkIds, cancellationToken);

                    return _responseMapper.ToDeleteResponse(command.CollectionName, deletedCount);
                },
                unexpectedTitle: "Vector delete failed");
    }

    public Task<ServiceResult<VectorRecordResponse>> GetByIdAsync(
        string collectionName,
        string chunkId,
        CancellationToken cancellationToken)
    {
        var resolvedCollectionName = collectionName?.Trim() ?? string.Empty;
        var resolvedChunkId = chunkId?.Trim() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(resolvedCollectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        if (string.IsNullOrWhiteSpace(resolvedChunkId))
        {
            errors["chunkId"] = ["Chunk id is required."];
        }

        if (errors.Count > 0)
        {
            return Task.FromResult(ServiceResult<VectorRecordResponse>.Validation(errors));
        }

        return _resultExecutor.ExecuteOptionalAsync(
            async () =>
            {
                var record = await _vectorStoreClient
                    .GetByIdAsync(resolvedCollectionName, resolvedChunkId, cancellationToken);

                return record is null
                    ? null
                    : _responseMapper.ToRecordResponse(resolvedCollectionName, record);
            },
            unexpectedTitle: "Vector retrieval failed",
            missingFailure: new ServiceFailure(
                ServiceFailureKind.NotFound,
                "Vector not found",
                $"No vector with chunk id '{resolvedChunkId}' exists in collection '{resolvedCollectionName}'."));
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
                        .UpsertAsync(command.CollectionName, command.Records, cancellationToken);

                    return (
                        Value: _responseMapper.ToUpsertResponse(
                            command.CollectionName,
                            command.Records.Count),
                        IsCreated: true);
                },
                unexpectedTitle: "Vector upsert failed");
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
                        .SearchAsync(searchRequest, cancellationToken);

                    return _responseMapper.ToSearchResponse(traceId, hits);
                },
                unexpectedTitle: "Vector search failed");
    }
}

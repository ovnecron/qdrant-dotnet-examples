using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class AdminEndpointService : IAdminEndpointService
{
    private readonly string _defaultCollectionName;
    private readonly IAdminRequestParser _requestParser;
    private readonly IAdminResponseMapper _responseMapper;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly IVectorStoreClient _vectorStoreClient;

    public AdminEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        IVectorStoreClient vectorStoreClient,
        IAdminRequestParser requestParser,
        IAdminResponseMapper responseMapper,
        IServiceResultExecutor resultExecutor)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _vectorStoreClient = vectorStoreClient ?? throw new ArgumentNullException(nameof(vectorStoreClient));
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _responseMapper = responseMapper ?? throw new ArgumentNullException(nameof(responseMapper));
        _resultExecutor = resultExecutor ?? throw new ArgumentNullException(nameof(resultExecutor));
    }

    public async Task<ServiceResult<InitializeCollectionResponse>> InitializeCollectionAsync(
        InitializeCollectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseInitializeCollectionRequest(
                request,
                _defaultCollectionName,
                out var definition,
                out var errors))
        {
            return ServiceResult<InitializeCollectionResponse>.Validation(errors);
        }

        return await _resultExecutor.ExecuteAsync(
                async () =>
                {
                    var result = await _vectorStoreClient
                        .InitializeCollectionAsync(definition, cancellationToken);

                    return (
                        Value: _responseMapper.ToResponse(result),
                        IsCreated: result.Created);
                },
                unexpectedTitle: "Collection initialization failed");
    }
}

using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Embeddings.Contracts;
using Embeddings.Interfaces;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class SemanticSearchEndpointService : ISemanticSearchEndpointService
{
    private readonly string _defaultCollectionName;
    private readonly ITextEmbeddingClient _embeddingClient;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly ISemanticSearchRequestParser _requestParser;
    private readonly ISemanticSearchResponseMapper _responseMapper;
    private readonly IVectorStoreClient _vectorStoreClient;

    public SemanticSearchEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        ITextEmbeddingClient embeddingClient,
        IVectorStoreClient vectorStoreClient,
        ISemanticSearchRequestParser requestParser,
        ISemanticSearchResponseMapper responseMapper,
        IServiceResultExecutor resultExecutor)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _vectorStoreClient = vectorStoreClient ?? throw new ArgumentNullException(nameof(vectorStoreClient));
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _responseMapper = responseMapper ?? throw new ArgumentNullException(nameof(responseMapper));
        _resultExecutor = resultExecutor ?? throw new ArgumentNullException(nameof(resultExecutor));
    }

    public async Task<ServiceResult<SemanticSearchQueryResponse>> QueryAsync(
        SemanticSearchQueryRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseQueryRequest(
                request,
                _defaultCollectionName,
                out var command,
                out var errors))
        {
            return ServiceResult<SemanticSearchQueryResponse>.Validation(errors);
        }

        return await _resultExecutor.ExecuteAsync(
            async () =>
            {
                var embedding = await _embeddingClient.EmbedAsync(
                    new TextEmbeddingRequest
                    {
                        Text = command.QueryText,
                        Kind = EmbeddingKind.Query
                    },
                    cancellationToken);

                var searchRequest = new SearchRequest
                {
                    CollectionName = command.CollectionName,
                    QueryVector = embedding.Vector.ToArray(),
                    TopK = command.TopK,
                    MinScore = command.MinScore,
                    Filter = command.Filter
                };

                var hits = await _vectorStoreClient.SearchAsync(searchRequest, cancellationToken);

                return _responseMapper.ToQueryResponse(
                    traceId,
                    command.CollectionName,
                    command.QueryText,
                    embedding.Descriptor,
                    hits);
            },
            unexpectedTitle: "Semantic search failed");
    }
}

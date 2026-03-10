using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Microsoft.Extensions.Options;

using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class SemanticSearchEndpointService : ISemanticSearchEndpointService
{
    private readonly string _defaultCollectionName;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly ISemanticSearchRequestParser _requestParser;
    private readonly ISemanticSearchResponseMapper _responseMapper;
    private readonly ITextRetrievalService _textRetrievalService;

    public SemanticSearchEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        ITextRetrievalService textRetrievalService,
        ISemanticSearchRequestParser requestParser,
        ISemanticSearchResponseMapper responseMapper,
        IServiceResultExecutor resultExecutor)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _textRetrievalService = textRetrievalService ?? throw new ArgumentNullException(nameof(textRetrievalService));
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
                var retrieval = await _textRetrievalService.RetrieveAsync(
                    new TextRetrievalRequest(
                        command.CollectionName,
                        command.QueryText,
                        command.TopK,
                        command.MinScore,
                        command.Filter),
                    cancellationToken);

                return _responseMapper.ToQueryResponse(
                    traceId,
                    retrieval.CollectionName,
                    retrieval.QueryText,
                    retrieval.Embedding,
                    retrieval.Hits);
            },
            unexpectedTitle: "Semantic search failed");
    }
}

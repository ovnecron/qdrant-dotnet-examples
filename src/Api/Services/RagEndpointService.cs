using Api.Contracts;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Microsoft.Extensions.Options;

using VectorStore.Qdrant.Options;

namespace Api.Services;

internal sealed class RagEndpointService : IRagEndpointService
{
    private readonly IRagAnswerGenerator _answerGenerator;
    private readonly IRagContextAssembler _contextAssembler;
    private readonly string _defaultCollectionName;
    private readonly IRagRequestParser _requestParser;
    private readonly IRagResponseMapper _responseMapper;
    private readonly IServiceResultExecutor _resultExecutor;
    private readonly ITextRetrievalService _textRetrievalService;

    public RagEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        ITextRetrievalService textRetrievalService,
        IRagRequestParser requestParser,
        IRagContextAssembler contextAssembler,
        IRagAnswerGenerator answerGenerator,
        IRagResponseMapper responseMapper,
        IServiceResultExecutor resultExecutor)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _textRetrievalService = textRetrievalService ?? throw new ArgumentNullException(nameof(textRetrievalService));
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _contextAssembler = contextAssembler ?? throw new ArgumentNullException(nameof(contextAssembler));
        _answerGenerator = answerGenerator ?? throw new ArgumentNullException(nameof(answerGenerator));
        _responseMapper = responseMapper ?? throw new ArgumentNullException(nameof(responseMapper));
        _resultExecutor = resultExecutor ?? throw new ArgumentNullException(nameof(resultExecutor));
    }

    public async Task<ServiceResult<RagQueryResponse>> QueryAsync(
        RagQueryRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParseQueryRequest(
                request,
                _defaultCollectionName,
                out var command,
                out var errors))
        {
            return ServiceResult<RagQueryResponse>.Validation(errors);
        }

        var retrievalResult = await _resultExecutor.ExecuteAsync(
            () => _textRetrievalService.RetrieveAsync(
                new TextRetrievalRequest(
                    command.CollectionName,
                    command.Question,
                    command.TopK,
                    command.MinScore,
                    command.Filter),
                cancellationToken),
            unexpectedTitle: "RAG retrieval failed");

        if (retrievalResult.Failure is not null)
        {
            return ServiceResult<RagQueryResponse>.Failed(retrievalResult.Failure);
        }

        var retrieval = retrievalResult.Value;
        if (retrieval is null)
        {
            return ServiceResult<RagQueryResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "RAG retrieval failed",
                    "No retrieval payload was returned."));
        }

        if (retrieval.Hits.Count == 0)
        {
            return ServiceResult<RagQueryResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.InsufficientGrounding,
                    "Insufficient grounded context",
                    "No sufficiently relevant evidence was found for the requested question."));
        }

        var context = _contextAssembler.Assemble(retrieval);
        if (context.Citations.Count == 0)
        {
            return ServiceResult<RagQueryResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.InsufficientGrounding,
                    "Insufficient grounded context",
                    "No usable grounded context could be assembled from the retrieved hits."));
        }

        return await _resultExecutor.ExecuteAsync(
            async () =>
            {
                var generation = await _answerGenerator.GenerateAsync(
                    new RagAnswerGenerationRequest
                    {
                        Question = command.Question,
                        Context = context.Context,
                        Citations = context.Citations
                    },
                    cancellationToken);

                return _responseMapper.ToQueryResponse(
                    traceId,
                    retrieval,
                    context,
                    generation,
                    command.IncludeDebug);
            },
            unexpectedTitle: "RAG query failed");
    }
}

using Api.Contracts;

namespace Api.Services.Mappers;

internal interface IRagResponseMapper
{
    RagQueryResponse ToQueryResponse(
        string traceId,
        TextRetrievalResult retrieval,
        RagContext context,
        RagAnswerGenerationResult generation,
        bool includeDebug);
}

using Api.Contracts;

namespace Api.Services.Mappers;

internal sealed class RagResponseMapper : IRagResponseMapper
{
    public RagQueryResponse ToQueryResponse(
        string traceId,
        TextRetrievalResult retrieval,
        RagContext context,
        RagAnswerGenerationResult generation,
        bool includeDebug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentNullException.ThrowIfNull(retrieval);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(generation);

        return new RagQueryResponse
        {
            TraceId = traceId,
            Answer = generation.Answer,
            Citations = context.Citations
                .Select(
                    citation => new RagCitationResponse
                    {
                        ChunkId = citation.ChunkId,
                        DocId = citation.DocId,
                        Source = citation.Source,
                        Title = citation.Title,
                        Section = citation.Section,
                        Score = citation.Score
                    })
                .ToArray(),
            Debug = includeDebug
                ? new RagDebugResponse
                {
                    Collection = retrieval.CollectionName,
                    EmbeddingProvider = retrieval.Embedding.Provider,
                    EmbeddingModel = retrieval.Embedding.Model,
                    EmbeddingSchemaVersion = retrieval.Embedding.SchemaVersion,
                    AnswerProvider = generation.Descriptor.Provider,
                    AnswerModel = generation.Descriptor.Model,
                    RetrievedHitCount = context.RetrievedHitCount
                }
                : null
        };
    }
}

using Embeddings.Interfaces;
using Embeddings.Options;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;

namespace Api.Services.Ingestion;

internal sealed class MarkdownIngestionProcessor : IMarkdownIngestionProcessor
{
    private readonly IMarkdownChunker _chunker;
    private readonly IChunkEmbeddingTextFactory _embeddingTextFactory;
    private readonly ITextEmbeddingClient _embeddingClient;
    private readonly EmbeddingOptions _embeddingOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IVectorStoreClient _vectorStoreClient;

    public MarkdownIngestionProcessor(
        IMarkdownChunker chunker,
        IChunkEmbeddingTextFactory embeddingTextFactory,
        ITextEmbeddingClient embeddingClient,
        IOptions<EmbeddingOptions> embeddingOptions,
        TimeProvider timeProvider,
        IVectorStoreClient vectorStoreClient)
    {
        _chunker = chunker ?? throw new ArgumentNullException(nameof(chunker));
        _embeddingTextFactory = embeddingTextFactory ?? throw new ArgumentNullException(nameof(embeddingTextFactory));
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        ArgumentNullException.ThrowIfNull(embeddingOptions);
        _embeddingOptions = embeddingOptions.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _vectorStoreClient = vectorStoreClient ?? throw new ArgumentNullException(nameof(vectorStoreClient));
    }

    public async Task<IngestionJobResult> ProcessAsync(
        QueuedMarkdownIngestionJob job,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var chunks = _chunker.Chunk(
            new MarkdownDocument
            {
                DocId = job.DocId,
                Source = job.SourceId,
                Markdown = job.Markdown,
                Title = job.Title,
                Tags = job.Tags
            },
            new MarkdownChunkingOptions
            {
                ChunkSize = job.ChunkSize,
                ChunkOverlap = job.ChunkOverlap
            });

        var embeddingRequests = chunks
            .Select(_embeddingTextFactory.CreateDocumentRequest)
            .ToArray();

        var embeddingResults = new List<Embeddings.Contracts.TextEmbeddingResult>(embeddingRequests.Length);
        var batchSize = Math.Max(1, _embeddingOptions.BatchSize);

        foreach (var batch in embeddingRequests.Chunk(batchSize))
        {
            var batchResults = await _embeddingClient.EmbedBatchAsync(batch, cancellationToken);
            embeddingResults.AddRange(batchResults);
        }

        if (embeddingResults.Count != chunks.Count)
        {
            throw new InvalidOperationException("Embedding result count does not match chunk count.");
        }

        var now = _timeProvider.GetUtcNow();

        await _vectorStoreClient.DeleteByFilterAsync(
            job.CollectionName,
            new SearchFilter
            {
                DocIdEquals = job.DocId,
                TenantIdEquals = job.TenantId,
                TenantIdIsNull = string.IsNullOrWhiteSpace(job.TenantId)
            },
            cancellationToken);

        var records = chunks.Zip(
                embeddingResults,
                (chunk, embedding) => new VectorRecord
                {
                    ChunkId = chunk.ChunkId,
                    Vector = embedding.Vector,
                    DocId = chunk.DocId,
                    Source = chunk.Source,
                    Title = chunk.Title,
                    Section = chunk.Section,
                    Tags = chunk.Tags,
                    Content = chunk.Content,
                    Checksum = chunk.Checksum,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    TenantId = job.TenantId,
                    DocVersion = job.DocVersion,
                    EmbeddingSchemaVersion = embedding.Descriptor.SchemaVersion
                })
            .ToArray();

        await _vectorStoreClient.UpsertAsync(job.CollectionName, records, cancellationToken);

        return new IngestionJobResult(records.Length, records.Length);
    }
}

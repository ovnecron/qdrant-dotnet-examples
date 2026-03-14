using Api.Services.Ingestion;

using Embeddings.Clients;
using Embeddings.Options;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class MarkdownIngestionProcessorTests
{
    [Fact]
    public async Task ProcessAsync_DeletesTenantScopedRecords_AndUpsertsDocVersion()
    {
        var vectorStore = new RecordingVectorStoreClient();
        var processor = CreateProcessor(vectorStore, "schema-v2");
        var job = new QueuedMarkdownIngestionJob
        {
            JobId = "job-1",
            CollectionName = "knowledge_chunks",
            DocId = "guide-1",
            DocVersion = "doc-version-1",
            SourceId = "docs/guide.md",
            Title = "Local Run",
            Markdown = "# Local Run\n\nRun AppHost and wait.",
            Tags = ["tutorial"],
            TenantId = "tenant-a",
            ChunkSize = 800,
            ChunkOverlap = 120,
            AcceptedAtUtc = DateTimeOffset.Parse("2026-03-08T10:00:00+00:00"),
            EmbeddingModel = "hashing-text-v1",
            EmbeddingSchemaVersion = "schema-v2"
        };

        var result = await processor.ProcessAsync(job, CancellationToken.None);

        Assert.Equal(1, result.ChunkCount);
        Assert.Equal(1, result.UpsertedCount);
        Assert.Equal("knowledge_chunks", vectorStore.DeleteCollectionName);
        Assert.NotNull(vectorStore.DeleteFilter);
        Assert.Equal("guide-1", vectorStore.DeleteFilter.DocIdEquals);
        Assert.Equal("tenant-a", vectorStore.DeleteFilter.TenantIdEquals);
        Assert.False(vectorStore.DeleteFilter.TenantIdIsNull);

        var record = Assert.Single(vectorStore.UpsertedRecords);
        Assert.Equal("guide-1", record.DocId);
        Assert.Equal("tenant-a", record.TenantId);
        Assert.Equal("doc-version-1", record.DocVersion);
        Assert.Equal("schema-v2", record.EmbeddingSchemaVersion);
    }

    [Fact]
    public async Task ProcessAsync_DeletesOnlyTenantlessRecords_WhenTenantIdIsMissing()
    {
        var vectorStore = new RecordingVectorStoreClient();
        var processor = CreateProcessor(vectorStore, "schema-v1");
        var job = new QueuedMarkdownIngestionJob
        {
            JobId = "job-2",
            CollectionName = "knowledge_chunks",
            DocId = "guide-1",
            DocVersion = "doc-version-2",
            SourceId = "docs/guide.md",
            Title = "Local Run",
            Markdown = "# Local Run\n\nRun AppHost and wait.",
            Tags = ["tutorial"],
            TenantId = null,
            ChunkSize = 800,
            ChunkOverlap = 120,
            AcceptedAtUtc = DateTimeOffset.Parse("2026-03-08T10:05:00+00:00"),
            EmbeddingModel = "hashing-text-v1",
            EmbeddingSchemaVersion = "schema-v1"
        };

        await processor.ProcessAsync(job, CancellationToken.None);

        Assert.NotNull(vectorStore.DeleteFilter);
        Assert.Equal("guide-1", vectorStore.DeleteFilter.DocIdEquals);
        Assert.Null(vectorStore.DeleteFilter.TenantIdEquals);
        Assert.True(vectorStore.DeleteFilter.TenantIdIsNull);

        var record = Assert.Single(vectorStore.UpsertedRecords);
        Assert.Null(record.TenantId);
        Assert.Equal("doc-version-2", record.DocVersion);
        Assert.Equal("schema-v1", record.EmbeddingSchemaVersion);
    }

    private static MarkdownIngestionProcessor CreateProcessor(
        RecordingVectorStoreClient vectorStore,
        string schemaVersion)
    {
        var embeddingOptions = Options.Create(
            new EmbeddingOptions
            {
                Provider = EmbeddingProvider.Deterministic,
                Model = "hashing-text-v1",
                Dimension = 16,
                BatchSize = 8,
                SchemaVersion = schemaVersion
            });

        return new MarkdownIngestionProcessor(
            new MarkdownChunker(),
            new ChunkEmbeddingTextFactory(),
            new DeterministicTextEmbeddingClient(embeddingOptions),
            embeddingOptions,
            TimeProvider.System,
            vectorStore);
    }

    private sealed class RecordingVectorStoreClient : IVectorStoreClient
    {
        public string? DeleteCollectionName { get; private set; }

        public SearchFilter? DeleteFilter { get; private set; }

        public IReadOnlyList<VectorRecord> UpsertedRecords { get; private set; } = [];

        public Task<CollectionInitResult> InitializeCollectionAsync(
            CollectionDefinition definition,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpsertAsync(
            string collectionName,
            IReadOnlyCollection<VectorRecord> records,
            CancellationToken cancellationToken = default)
        {
            UpsertedRecords = records.ToArray();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<VectorRecord?> GetByIdAsync(
            string collectionName,
            string chunkId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> DeleteAsync(
            string collectionName,
            IReadOnlyCollection<string> chunkIds,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteByFilterAsync(
            string collectionName,
            SearchFilter filter,
            CancellationToken cancellationToken = default)
        {
            DeleteCollectionName = collectionName;
            DeleteFilter = filter;
            return Task.CompletedTask;
        }
    }
}

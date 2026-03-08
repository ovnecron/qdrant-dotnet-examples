using Api.Contracts;
using Api.Services.Results;
using Api.Services.Validation;

using Embeddings.Options;

using Microsoft.Extensions.Options;

using VectorStore.Qdrant.Options;

namespace Api.Services.Ingestion;

internal sealed class IngestionEndpointService : IIngestionEndpointService
{
    private readonly string _defaultCollectionName;
    private readonly EmbeddingOptions _embeddingOptions;
    private readonly IIngestionJobStore _jobStore;
    private readonly IMarkdownIngestionQueue _queue;
    private readonly IMarkdownIngestRequestParser _requestParser;
    private readonly TimeProvider _timeProvider;

    public IngestionEndpointService(
        IOptions<QdrantOptions> qdrantOptions,
        IOptions<EmbeddingOptions> embeddingOptions,
        IMarkdownIngestRequestParser requestParser,
        IMarkdownIngestionQueue queue,
        IIngestionJobStore jobStore,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        ArgumentNullException.ThrowIfNull(embeddingOptions);
        _defaultCollectionName = qdrantOptions.Value.Collection;
        _embeddingOptions = embeddingOptions.Value;
        _requestParser = requestParser ?? throw new ArgumentNullException(nameof(requestParser));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ServiceResult<MarkdownIngestAcceptedResponse>> IngestMarkdownAsync(
        MarkdownIngestRequest request,
        CancellationToken cancellationToken)
    {
        if (!_requestParser.TryParse(
                request,
                _defaultCollectionName,
                out var command,
                out var errors))
        {
            return ServiceResult<MarkdownIngestAcceptedResponse>.Validation(errors);
        }

        var acceptedAtUtc = _timeProvider.GetUtcNow();
        var jobId = Guid.CreateVersion7().ToString("N");
        var docVersion = Guid.CreateVersion7().ToString("N");

        var job = new QueuedMarkdownIngestionJob
        {
            JobId = jobId,
            CollectionName = command.CollectionName,
            DocId = command.DocId,
            DocVersion = docVersion,
            SourceId = command.SourceId,
            Title = command.Title,
            Markdown = command.Markdown,
            Tags = command.Tags,
            TenantId = command.TenantId,
            ChunkSize = command.ChunkSize,
            ChunkOverlap = command.ChunkOverlap,
            AcceptedAtUtc = acceptedAtUtc,
            EmbeddingModel = _embeddingOptions.Model,
            EmbeddingSchemaVersion = _embeddingOptions.SchemaVersion
        };

        _jobStore.AddAccepted(job);

        try
        {
            await _queue.EnqueueAsync(job, cancellationToken);
        }
        catch
        {
            _jobStore.Remove(job.JobId);
            throw;
        }

        return ServiceResult<MarkdownIngestAcceptedResponse>.Success(
            new MarkdownIngestAcceptedResponse
            {
                JobId = job.JobId,
                Collection = job.CollectionName,
                DocId = job.DocId,
                DocVersion = job.DocVersion,
                AcceptedAtUtc = job.AcceptedAtUtc,
                EmbeddingModel = job.EmbeddingModel,
                EmbeddingSchemaVersion = job.EmbeddingSchemaVersion
            });
    }

    public Task<ServiceResult<IngestJobStatusResponse>> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var resolvedJobId = jobId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resolvedJobId))
        {
            return Task.FromResult(
                ServiceResult<IngestJobStatusResponse>.Validation(
                    new Dictionary<string, string[]>
                    {
                        ["jobId"] = ["Job id is required."]
                    }));
        }

        if (!_jobStore.TryGet(resolvedJobId, out var record) || record is null)
        {
            return Task.FromResult(
                ServiceResult<IngestJobStatusResponse>.Failed(
                    new ServiceFailure(
                        ServiceFailureKind.NotFound,
                        "Ingestion job not found",
                        $"No ingestion job with id '{resolvedJobId}' exists.")));
        }

        return Task.FromResult(
            ServiceResult<IngestJobStatusResponse>.Success(
                new IngestJobStatusResponse
                {
                    JobId = record.JobId,
                    Status = record.Status.ToString(),
                    Collection = record.CollectionName,
                    DocId = record.DocId,
                    DocVersion = record.DocVersion,
                    AcceptedAtUtc = record.AcceptedAtUtc,
                    StartedAtUtc = record.StartedAtUtc,
                    CompletedAtUtc = record.CompletedAtUtc,
                    EmbeddingModel = record.EmbeddingModel,
                    EmbeddingSchemaVersion = record.EmbeddingSchemaVersion,
                    Result = record.Result is null
                        ? null
                        : new IngestJobResultResponse
                        {
                            ChunkCount = record.Result.ChunkCount,
                            UpsertedCount = record.Result.UpsertedCount
                        },
                    Error = record.Error is null
                        ? null
                        : new IngestJobErrorResponse
                        {
                            Code = record.Error.Code,
                            Message = record.Error.Message
                        }
                }));
    }
}

using System.Collections.Concurrent;

namespace Api.Services.Ingestion;

internal sealed class InMemoryIngestionJobStore : IIngestionJobStore
{
    private readonly ConcurrentDictionary<string, IngestionJobRecord> _jobs = new(StringComparer.Ordinal);

    public void AddAccepted(QueuedMarkdownIngestionJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        _jobs[job.JobId] = new IngestionJobRecord
        {
            JobId = job.JobId,
            CollectionName = job.CollectionName,
            DocId = job.DocId,
            DocVersion = job.DocVersion,
            AcceptedAtUtc = job.AcceptedAtUtc,
            EmbeddingModel = job.EmbeddingModel,
            EmbeddingSchemaVersion = job.EmbeddingSchemaVersion,
            Status = IngestionJobStatus.Accepted
        };
    }

    public bool TryGet(string jobId, out IngestionJobRecord? record)
    {
        return _jobs.TryGetValue(jobId, out record);
    }

    public void Remove(string jobId)
    {
        _jobs.TryRemove(jobId, out _);
    }

    public void MarkRunning(string jobId, DateTimeOffset startedAtUtc)
    {
        Update(jobId, existing => existing with
        {
            Status = IngestionJobStatus.Running,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = null,
            Result = null,
            Error = null
        });
    }

    public void MarkSucceeded(string jobId, IngestionJobResult result, DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        Update(jobId, existing => existing with
        {
            Status = IngestionJobStatus.Succeeded,
            CompletedAtUtc = completedAtUtc,
            Result = result,
            Error = null
        });
    }

    public void MarkFailed(string jobId, IngestionJobError error, DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(error);

        Update(jobId, existing => existing with
        {
            Status = IngestionJobStatus.Failed,
            CompletedAtUtc = completedAtUtc,
            Result = null,
            Error = error
        });
    }

    private void Update(string jobId, Func<IngestionJobRecord, IngestionJobRecord> update)
    {
        _jobs.AddOrUpdate(
            jobId,
            static _ => throw new InvalidOperationException("Ingestion job does not exist."),
            (_, existing) => update(existing));
    }
}

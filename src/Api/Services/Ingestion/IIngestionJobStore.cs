namespace Api.Services.Ingestion;

internal interface IIngestionJobStore
{
    void AddAccepted(QueuedMarkdownIngestionJob job);

    bool TryGet(string jobId, out IngestionJobRecord? record);

    void Remove(string jobId);

    void MarkRunning(string jobId, DateTimeOffset startedAtUtc);

    void MarkSucceeded(string jobId, IngestionJobResult result, DateTimeOffset completedAtUtc);

    void MarkFailed(string jobId, IngestionJobError error, DateTimeOffset completedAtUtc);
}

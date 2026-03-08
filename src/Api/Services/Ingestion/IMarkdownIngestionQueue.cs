namespace Api.Services.Ingestion;

internal interface IMarkdownIngestionQueue
{
    Task EnqueueAsync(QueuedMarkdownIngestionJob job, CancellationToken cancellationToken);

    ValueTask<QueuedMarkdownIngestionJob> DequeueAsync(CancellationToken cancellationToken);
}

namespace Api.Services.Ingestion;

internal interface IMarkdownIngestionProcessor
{
    Task<IngestionJobResult> ProcessAsync(
        QueuedMarkdownIngestionJob job,
        CancellationToken cancellationToken);
}

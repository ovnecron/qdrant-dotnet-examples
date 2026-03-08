namespace Api.Services.Ingestion;

internal enum IngestionJobStatus
{
    Accepted = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

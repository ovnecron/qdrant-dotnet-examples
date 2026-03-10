namespace Api.Services.Ingestion;

internal sealed record IngestionJobResult(int ChunkCount, int UpsertedCount);

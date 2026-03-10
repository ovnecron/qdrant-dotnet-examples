namespace Api.Services;

internal sealed record RagCitationDraft(
    string ChunkId,
    string DocId,
    string Source,
    string? Title,
    string? Section,
    float Score,
    string Content);

namespace Api.Services.Validation;

internal sealed record DeleteVectorsCommand(
    string CollectionName,
    IReadOnlyList<string> ChunkIds);

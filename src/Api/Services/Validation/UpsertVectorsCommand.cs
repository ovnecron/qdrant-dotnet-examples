using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal sealed record UpsertVectorsCommand(
    string CollectionName,
    IReadOnlyList<VectorRecord> Records);

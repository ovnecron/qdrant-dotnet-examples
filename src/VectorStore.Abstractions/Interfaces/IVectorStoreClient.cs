using VectorStore.Abstractions.Contracts;

namespace VectorStore.Abstractions.Interfaces;

public interface IVectorStoreClient
{
    Task<CollectionInitResult> InitializeCollectionAsync(
        CollectionDefinition definition,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        string collectionName,
        IReadOnlyCollection<VectorRecord> records,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);

    Task<VectorRecord?> GetByIdAsync(
        string collectionName,
        string chunkId,
        CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(
        string collectionName,
        IReadOnlyCollection<string> chunkIds,
        CancellationToken cancellationToken = default);

    Task DeleteByFilterAsync(
        string collectionName,
        SearchFilter filter,
        CancellationToken cancellationToken = default);
}

using Grpc.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Qdrant.Client;
using Qdrant.Client.Grpc;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Clients.Internal;
using VectorStore.Qdrant.Options;

namespace VectorStore.Qdrant.Clients;

public sealed class QdrantVectorStoreClient : IVectorStoreClient
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStoreClient> _logger;

    public QdrantVectorStoreClient(
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStoreClient> logger,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var resolvedOptions = options.Value
            ?? throw new InvalidOperationException("Qdrant options are not configured.");

        var grpcEndpoint = QdrantEndpointResolver.ResolveGrpcEndpoint(resolvedOptions);
        var timeout = resolvedOptions.Timeout <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(30)
            : resolvedOptions.Timeout;

        _client = new QdrantClient(grpcEndpoint, resolvedOptions.ApiKey, timeout, loggerFactory);
    }

    public async Task<CollectionInitResult> InitializeCollectionAsync(
        CollectionDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ValidateCollectionName(definition.Name, nameof(definition.Name));

        if (definition.VectorSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(definition.VectorSize),
                "Vector size must be greater than zero.");
        }

        var collectionExists = await _client
            .CollectionExistsAsync(definition.Name, cancellationToken);

        if (!collectionExists)
        {
            await _client.CreateCollectionAsync(
                    definition.Name,
                    new VectorParams
                    {
                        Size = checked((ulong)definition.VectorSize),
                        Distance = MapDistance(definition.Distance)
                    },
                    cancellationToken: cancellationToken);
        }

        await EnsurePayloadIndexesAsync(
                definition.Name,
                definition.PayloadIndexes,
                cancellationToken);

        return new CollectionInitResult
        {
            CollectionName = definition.Name,
            Created = !collectionExists
        };
    }

    public async Task UpsertAsync(
        string collectionName,
        IReadOnlyCollection<VectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        ValidateCollectionName(collectionName, nameof(collectionName));
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            return;
        }

        var points = records
            .Select(QdrantPayloadMapper.MapPoint)
            .ToArray();

        await _client.UpsertAsync(
                collectionName,
                points,
                wait: true,
                cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateCollectionName(request.CollectionName, nameof(request.CollectionName));

        if (request.QueryVector.Count == 0)
        {
            throw new ArgumentException("Query vector cannot be empty.", nameof(request.QueryVector));
        }

        if (request.TopK <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TopK),
                "TopK must be greater than zero.");
        }

        var filter = QdrantSearchFilterMapper.BuildFilter(request.Filter);

        var hits = await _client.SearchAsync(
                request.CollectionName,
                request.QueryVector.ToArray(),
                filter: filter,
                limit: checked((ulong)request.TopK),
                payloadSelector: true,
                vectorsSelector: false,
                scoreThreshold: request.MinScore,
                cancellationToken: cancellationToken);

        return hits
            .Select(QdrantPayloadMapper.MapSearchResult)
            .ToArray();
    }

    public async Task<VectorRecord?> GetByIdAsync(
        string collectionName,
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        ValidateCollectionName(collectionName, nameof(collectionName));

        if (string.IsNullOrWhiteSpace(chunkId))
        {
            throw new ArgumentException("Chunk id must be provided.", nameof(chunkId));
        }

        var pointId = QdrantPointIdConverter.ToPointId(chunkId);

        var points = await _client.RetrieveAsync(
                collectionName,
                pointId,
                withPayload: true,
                withVectors: true,
                cancellationToken: cancellationToken);

        var point = points.FirstOrDefault();
        return point is null ? null : QdrantPayloadMapper.MapVectorRecord(point, chunkId);
    }

    public async Task<int> DeleteAsync(
        string collectionName,
        IReadOnlyCollection<string> chunkIds,
        CancellationToken cancellationToken = default)
    {
        ValidateCollectionName(collectionName, nameof(collectionName));
        ArgumentNullException.ThrowIfNull(chunkIds);

        var pointIds = chunkIds
            .Where(static chunkId => !string.IsNullOrWhiteSpace(chunkId))
            .Select(chunkId => chunkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .Select(QdrantPointIdConverter.ToPointId)
            .ToArray();

        if (pointIds.Length == 0)
        {
            return 0;
        }

        var existingPoints = await _client
            .RetrieveAsync(
                collectionName,
                pointIds,
                withPayload: false,
                withVectors: false,
                cancellationToken: cancellationToken);

        if (existingPoints.Count == 0)
        {
            return 0;
        }

        await _client.DeleteAsync(
                collectionName,
                pointIds,
                wait: true,
                cancellationToken: cancellationToken);

        return existingPoints.Count;
    }

    public async Task DeleteByFilterAsync(
        string collectionName,
        SearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        ValidateCollectionName(collectionName, nameof(collectionName));
        ArgumentNullException.ThrowIfNull(filter);

        var qdrantFilter = QdrantSearchFilterMapper.BuildFilter(filter);
        if (qdrantFilter is null)
        {
            return;
        }

        await _client.DeleteAsync(
            collectionName,
            qdrantFilter,
            wait: true,
            cancellationToken: cancellationToken);
    }

    private async Task EnsurePayloadIndexesAsync(
        string collectionName,
        IReadOnlyList<string> payloadIndexes,
        CancellationToken cancellationToken)
    {
        if (payloadIndexes is null || payloadIndexes.Count == 0)
        {
            return;
        }

        var distinctIndexes = payloadIndexes
            .Where(static index => !string.IsNullOrWhiteSpace(index))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var indexName in distinctIndexes)
        {
            try
            {
                await _client.CreatePayloadIndexAsync(
                        collectionName,
                        indexName,
                        ResolvePayloadSchemaType(indexName),
                        wait: true,
                        cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (IsAlreadyExistsError(exception))
            {
                _logger.LogDebug(
                    exception,
                    "Skipping payload index creation because index '{IndexName}' already exists in '{CollectionName}'.",
                    indexName,
                    collectionName);
            }
        }
    }

    private static Distance MapDistance(VectorDistance distance)
    {
        return distance switch
        {
            VectorDistance.Cosine => Distance.Cosine,
            VectorDistance.Dot => Distance.Dot,
            VectorDistance.Euclid => Distance.Euclid,
            VectorDistance.Manhattan => Distance.Manhattan,
            _ => throw new ArgumentOutOfRangeException(nameof(distance), distance, "Unsupported vector distance.")
        };
    }

    private static PayloadSchemaType ResolvePayloadSchemaType(string indexName)
    {
        return indexName.Equals("updatedAtUtc", StringComparison.OrdinalIgnoreCase)
            ? PayloadSchemaType.Datetime
            : PayloadSchemaType.Keyword;
    }

    private static bool IsAlreadyExistsError(Exception exception)
    {
        if (exception is RpcException rpcException && rpcException.StatusCode == StatusCode.AlreadyExists)
        {
            return true;
        }

        return exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCollectionName(string collectionName, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("Collection name must be provided.", parameterName);
        }
    }
}

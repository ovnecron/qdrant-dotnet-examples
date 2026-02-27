using Api.Contracts;
using Api.Services.Results;

using Grpc.Core;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Options;

namespace Api.Services;

public sealed class AdminEndpointService : IAdminEndpointService
{
    private static readonly IReadOnlyList<string> DefaultPayloadIndexes =
    [
        "docId",
        "source",
        "tags",
        "tenantId",
        "updatedAtUtc"
    ];

    private readonly QdrantOptions _qdrantOptions;
    private readonly IVectorStoreClient _vectorStoreClient;

    public AdminEndpointService(IOptions<QdrantOptions> qdrantOptions, IVectorStoreClient vectorStoreClient)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _qdrantOptions = qdrantOptions.Value;
        _vectorStoreClient = vectorStoreClient;
    }

    public async Task<ServiceResult<InitializeCollectionResponse>> InitializeCollectionAsync(
        InitializeCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var collectionName = ResolveCollectionName(request.CollectionName);
        var errors = ValidateRequest(request, collectionName);

        if (errors.Count > 0)
        {
            return ServiceResult<InitializeCollectionResponse>.Validation(errors);
        }

        TryParseDistance(request.Distance, out var distance);

        var definition = new CollectionDefinition
        {
            Name = collectionName!,
            VectorSize = request.VectorSize!.Value,
            Distance = distance,
            VectorName = string.IsNullOrWhiteSpace(request.VectorName) ? null : request.VectorName.Trim(),
            PayloadIndexes = ResolvePayloadIndexes(request.PayloadIndexes)
        };

        try
        {
            var result = await _vectorStoreClient.InitializeCollectionAsync(definition, cancellationToken);
            var response = new InitializeCollectionResponse
            {
                CollectionName = result.CollectionName,
                Created = result.Created
            };

            return ServiceResult<InitializeCollectionResponse>.Success(
                response,
                isCreated: result.Created);
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return ServiceResult<InitializeCollectionResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.VectorStoreUnavailable,
                    "Vector store unavailable",
                    exception.Status.Detail));
        }
        catch (InvalidOperationException exception)
        {
            return ServiceResult<InitializeCollectionResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.ConfigurationInvalid,
                    "Vector store configuration invalid",
                    exception.Message));
        }
        catch (Exception exception)
        {
            return ServiceResult<InitializeCollectionResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Collection initialization failed",
                    exception.Message));
        }
    }

    private Dictionary<string, string[]> ValidateRequest(
        InitializeCollectionRequest request,
        string? collectionName)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collectionName"] = ["Collection name is required."];
        }

        if (request.VectorSize is null || request.VectorSize <= 0)
        {
            errors["vectorSize"] = ["Vector size must be greater than zero."];
        }

        if (!TryParseDistance(request.Distance, out _))
        {
            errors["distance"] = ["Distance must be one of: Cosine, Dot, Euclid, Manhattan."];
        }

        return errors;
    }

    private static IReadOnlyList<string> ResolvePayloadIndexes(IReadOnlyList<string>? requestedIndexes)
    {
        if (requestedIndexes is null || requestedIndexes.Count == 0)
        {
            return DefaultPayloadIndexes;
        }

        return requestedIndexes
            .Where(static index => !string.IsNullOrWhiteSpace(index))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryParseDistance(string? rawDistance, out VectorDistance distance)
    {
        if (string.IsNullOrWhiteSpace(rawDistance))
        {
            distance = VectorDistance.Cosine;
            return true;
        }

        return Enum.TryParse(rawDistance, ignoreCase: true, out distance);
    }

    private string ResolveCollectionName(string? requestedCollection)
    {
        if (!string.IsNullOrWhiteSpace(requestedCollection))
        {
            return requestedCollection.Trim();
        }

        return _qdrantOptions.Collection;
    }
}

using System.Security.Cryptography;
using System.Text;

using Api.Contracts;
using Api.Services.Results;

using Grpc.Core;

using Microsoft.Extensions.Options;

using VectorStore.Abstractions.Contracts;
using VectorStore.Abstractions.Interfaces;
using VectorStore.Qdrant.Options;

namespace Api.Services;

public sealed class VectorEndpointService : IVectorEndpointService
{
    private readonly QdrantOptions _qdrantOptions;
    private readonly IVectorStoreClient _vectorStoreClient;

    public VectorEndpointService(IOptions<QdrantOptions> qdrantOptions, IVectorStoreClient vectorStoreClient)
    {
        ArgumentNullException.ThrowIfNull(qdrantOptions);
        _qdrantOptions = qdrantOptions.Value;
        _vectorStoreClient = vectorStoreClient;
    }

    public async Task<ServiceResult<VectorUpsertResponse>> UpsertAsync(
        VectorUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var collectionName = ResolveCollectionName(request.Collection);
        var errors = ValidateUpsertRequest(request, collectionName);

        if (errors.Count > 0)
        {
            return ServiceResult<VectorUpsertResponse>.Validation(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var records = request.Points!
            .Select(point => MapRecord(point, now))
            .ToArray();

        try
        {
            await _vectorStoreClient.UpsertAsync(collectionName!, records, cancellationToken);

            return ServiceResult<VectorUpsertResponse>.Success(
                new VectorUpsertResponse
                {
                    Collection = collectionName!,
                    UpsertedCount = records.Length
                },
                isCreated: true);
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return ServiceResult<VectorUpsertResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.VectorStoreUnavailable,
                    "Vector store unavailable",
                    exception.Status.Detail));
        }
        catch (InvalidOperationException exception)
        {
            return ServiceResult<VectorUpsertResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.ConfigurationInvalid,
                    "Vector store configuration invalid",
                    exception.Message));
        }
        catch (Exception exception)
        {
            return ServiceResult<VectorUpsertResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Vector upsert failed",
                    exception.Message));
        }
    }

    public async Task<ServiceResult<VectorSearchResponse>> SearchAsync(
        VectorSearchRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        var collectionName = ResolveCollectionName(request.Collection);
        var errors = ValidateSearchRequest(request, collectionName);

        if (errors.Count > 0)
        {
            return ServiceResult<VectorSearchResponse>.Validation(errors);
        }

        try
        {
            var searchRequest = new SearchRequest
            {
                CollectionName = collectionName!,
                QueryVector = request.QueryVector!,
                TopK = request.TopK,
                MinScore = request.MinScore,
                Filter = MapFilter(request.Filter)
            };

            var hits = await _vectorStoreClient.SearchAsync(searchRequest, cancellationToken);

            var response = new VectorSearchResponse
            {
                TraceId = traceId,
                Hits = hits
                    .Select(
                        hit => new VectorSearchHitResponse
                        {
                            ChunkId = hit.ChunkId,
                            Score = hit.Score,
                            Source = hit.Source,
                            Title = hit.Title,
                            Section = hit.Section,
                            ContentPreview = hit.ContentPreview,
                            Tags = hit.Tags
                        })
                    .ToArray()
            };

            return ServiceResult<VectorSearchResponse>.Success(response);
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return ServiceResult<VectorSearchResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.VectorStoreUnavailable,
                    "Vector store unavailable",
                    exception.Status.Detail));
        }
        catch (InvalidOperationException exception)
        {
            return ServiceResult<VectorSearchResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.ConfigurationInvalid,
                    "Vector store configuration invalid",
                    exception.Message));
        }
        catch (Exception exception)
        {
            return ServiceResult<VectorSearchResponse>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    "Vector search failed",
                    exception.Message));
        }
    }

    private static VectorRecord MapRecord(VectorPointRequest point, DateTimeOffset now)
    {
        var payload = point.Payload!;
        var createdAt = payload.CreatedAtUtc ?? now;
        var updatedAt = payload.UpdatedAtUtc ?? createdAt;
        var content = payload.Content!.Trim();
        var checksum = string.IsNullOrWhiteSpace(payload.Checksum)
            ? ComputeSha256(content)
            : payload.Checksum.Trim();

        return new VectorRecord
        {
            ChunkId = point.Id!.Trim(),
            Vector = point.Vector!.ToArray(),
            DocId = payload.DocId!.Trim(),
            Source = payload.Source!.Trim(),
            Title = string.IsNullOrWhiteSpace(payload.Title) ? null : payload.Title.Trim(),
            Section = string.IsNullOrWhiteSpace(payload.Section) ? null : payload.Section.Trim(),
            Tags = payload.Tags?
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [],
            Content = content,
            Checksum = checksum,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            TenantId = string.IsNullOrWhiteSpace(payload.TenantId) ? null : payload.TenantId.Trim(),
            EmbeddingSchemaVersion = string.IsNullOrWhiteSpace(payload.EmbeddingSchemaVersion)
                ? null
                : payload.EmbeddingSchemaVersion.Trim()
        };
    }

    private static SearchFilter MapFilter(VectorSearchFilterRequest? filter)
    {
        if (filter is null)
        {
            return new SearchFilter();
        }

        return new SearchFilter
        {
            DocIdEquals = string.IsNullOrWhiteSpace(filter.DocIdEquals) ? null : filter.DocIdEquals.Trim(),
            SourceEquals = string.IsNullOrWhiteSpace(filter.SourceEquals) ? null : filter.SourceEquals.Trim(),
            TagsAny = filter.TagsAny?
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [],
            TenantIdEquals = string.IsNullOrWhiteSpace(filter.TenantIdEquals) ? null : filter.TenantIdEquals.Trim(),
            UpdatedAtUtcFrom = filter.UpdatedAtUtcFrom,
            UpdatedAtUtcTo = filter.UpdatedAtUtcTo
        };
    }

    private static Dictionary<string, string[]> ValidateUpsertRequest(
        VectorUpsertRequest request,
        string? collectionName)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        if (request.Points is null || request.Points.Count == 0)
        {
            errors["points"] = ["At least one point is required."];
            return errors;
        }

        for (var index = 0; index < request.Points.Count; index++)
        {
            var point = request.Points[index];
            var payload = point.Payload;

            if (string.IsNullOrWhiteSpace(point.Id))
            {
                errors[$"points[{index}].id"] = ["Point id is required."];
            }

            if (point.Vector is null || point.Vector.Count == 0)
            {
                errors[$"points[{index}].vector"] = ["Point vector must contain at least one value."];
            }

            if (payload is null)
            {
                errors[$"points[{index}].payload"] = ["Point payload is required."];
                continue;
            }

            if (string.IsNullOrWhiteSpace(payload.DocId))
            {
                errors[$"points[{index}].payload.docId"] = ["Payload docId is required."];
            }

            if (string.IsNullOrWhiteSpace(payload.Source))
            {
                errors[$"points[{index}].payload.source"] = ["Payload source is required."];
            }

            if (string.IsNullOrWhiteSpace(payload.Content))
            {
                errors[$"points[{index}].payload.content"] = ["Payload content is required."];
            }
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateSearchRequest(
        VectorSearchRequest request,
        string? collectionName)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        if (request.QueryVector is null || request.QueryVector.Count == 0)
        {
            errors["queryVector"] = ["Query vector must contain at least one value."];
        }

        if (request.TopK <= 0)
        {
            errors["topK"] = ["TopK must be greater than zero."];
        }

        return errors;
    }

    private string ResolveCollectionName(string? requestedCollection)
    {
        if (!string.IsNullOrWhiteSpace(requestedCollection))
        {
            return requestedCollection.Trim();
        }

        return _qdrantOptions.Collection;
    }

    private static string ComputeSha256(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash);
    }
}

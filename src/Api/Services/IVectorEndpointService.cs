using Api.Contracts;
using Api.Services.Results;

namespace Api.Services;

public interface IVectorEndpointService
{
    Task<ServiceResult<VectorDeleteResponse>> DeleteAsync(
        VectorDeleteRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<VectorRecordResponse>> GetByIdAsync(
        string collectionName,
        string chunkId,
        CancellationToken cancellationToken);

    Task<ServiceResult<VectorUpsertResponse>> UpsertAsync(
        VectorUpsertRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<VectorSearchResponse>> SearchAsync(
        VectorSearchRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

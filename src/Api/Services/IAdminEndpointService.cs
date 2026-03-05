using Api.Contracts;
using Api.Services.Results;

namespace Api.Services;

public interface IAdminEndpointService
{
    Task<ServiceResult<InitializeCollectionResponse>> InitializeCollectionAsync(
        InitializeCollectionRequest request,
        CancellationToken cancellationToken);
}

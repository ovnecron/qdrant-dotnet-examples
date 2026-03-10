using Api.Contracts;
using Api.Services.Results;

namespace Api.Services;

public interface IRagEndpointService
{
    Task<ServiceResult<RagQueryResponse>> QueryAsync(
        RagQueryRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

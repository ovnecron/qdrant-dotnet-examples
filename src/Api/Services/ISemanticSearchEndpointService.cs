using Api.Contracts;
using Api.Services.Results;

namespace Api.Services;

public interface ISemanticSearchEndpointService
{
    Task<ServiceResult<SemanticSearchQueryResponse>> QueryAsync(
        SemanticSearchQueryRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

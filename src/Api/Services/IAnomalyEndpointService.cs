using Api.Contracts;
using Api.Services.Results;

namespace Api.Services;

public interface IAnomalyEndpointService
{
    Task<ServiceResult<AnomalyScoreResponse>> ScoreAsync(
        AnomalyScoreRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

using Api.Contracts;
using Api.Services.Results;

namespace Api.Services;

public interface ITextAnomalyEndpointService
{
    Task<ServiceResult<TextAnomalyScoreResponse>> ScoreAsync(
        TextAnomalyScoreRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

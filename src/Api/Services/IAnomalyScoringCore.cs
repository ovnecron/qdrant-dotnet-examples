using Api.Services.Results;

namespace Api.Services;

internal interface IAnomalyScoringCore
{
    Task<ServiceResult<AnomalyScoringCoreResult>> ScoreAsync(
        AnomalyScoringCoreRequest request,
        CancellationToken cancellationToken);
}

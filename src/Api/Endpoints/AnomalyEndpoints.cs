using System.Diagnostics;

using Api.Contracts;
using Api.Services;

namespace Api.Endpoints;

public static class AnomalyEndpoints
{
    public static IEndpointRouteBuilder MapAnomalyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/anomaly")
            .WithTags("Anomaly Detection");

        group.MapPost("/score", ScoreAsync)
            .WithName("ScoreAnomaly")
            .Produces<AnomalyScoreResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> ScoreAsync(
        AnomalyScoreRequest request,
        HttpContext httpContext,
        IAnomalyEndpointService anomalyEndpointService,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var result = await anomalyEndpointService.ScoreAsync(request, traceId, cancellationToken);

        return ServiceResultMapper.ToHttpResult(result, success => Results.Ok(success));
    }
}

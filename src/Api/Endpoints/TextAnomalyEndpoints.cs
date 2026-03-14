using System.Diagnostics;

using Api.Contracts;
using Api.Services;

namespace Api.Endpoints;

public static class TextAnomalyEndpoints
{
    public static IEndpointRouteBuilder MapTextAnomalyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/anomaly")
            .WithTags("Anomaly Detection");

        group.MapPost("/text/score", ScoreAsync)
            .WithName("ScoreTextAnomaly")
            .Produces<TextAnomalyScoreResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> ScoreAsync(
        TextAnomalyScoreRequest request,
        HttpContext httpContext,
        ITextAnomalyEndpointService textAnomalyEndpointService,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var result = await textAnomalyEndpointService.ScoreAsync(request, traceId, cancellationToken);

        return ServiceResultMapper.ToHttpResult(result, success => Results.Ok(success));
    }
}

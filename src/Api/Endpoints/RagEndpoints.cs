using System.Diagnostics;

using Api.Contracts;
using Api.Services;

namespace Api.Endpoints;

public static class RagEndpoints
{
    public static IEndpointRouteBuilder MapRagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/rag")
            .WithTags("RAG");

        group.MapPost("/query", QueryAsync)
            .WithName("RagQuery")
            .Produces<RagQueryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> QueryAsync(
        RagQueryRequest request,
        HttpContext httpContext,
        IRagEndpointService ragEndpointService,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var result = await ragEndpointService.QueryAsync(request, traceId, cancellationToken);

        return ServiceResultMapper.ToHttpResult(result, success => Results.Ok(success));
    }
}

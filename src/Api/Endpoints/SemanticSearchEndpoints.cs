using System.Diagnostics;

using Api.Contracts;
using Api.Services;

namespace Api.Endpoints;

public static class SemanticSearchEndpoints
{
    public static IEndpointRouteBuilder MapSemanticSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/search")
            .WithTags("Semantic Search");

        group.MapPost("/query", QueryAsync)
            .WithName("SemanticSearchQuery")
            .Produces<SemanticSearchQueryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> QueryAsync(
        SemanticSearchQueryRequest request,
        HttpContext httpContext,
        ISemanticSearchEndpointService semanticSearchEndpointService,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var result = await semanticSearchEndpointService
            .QueryAsync(request, traceId, cancellationToken);

        return ServiceResultMapper.ToHttpResult(result, success => Results.Ok(success));
    }
}

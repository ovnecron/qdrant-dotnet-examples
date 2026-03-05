using System.Diagnostics;

using Api.Contracts;
using Api.Services;

namespace Api.Endpoints;

public static class VectorEndpoints
{
    public static IEndpointRouteBuilder MapVectorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/vectors")
            .WithTags("Vectors");

        group.MapPost("/upsert", UpsertAsync)
            .WithName("UpsertVectors")
            .Produces<VectorUpsertResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/search", SearchAsync)
            .WithName("SearchVectors")
            .Produces<VectorSearchResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> UpsertAsync(
        VectorUpsertRequest request,
        IVectorEndpointService vectorEndpointService,
        CancellationToken cancellationToken)
    {
        var result = await vectorEndpointService
            .UpsertAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResultMapper.ToHttpResult(
            result,
            success => Results.Created(
                $"/api/v1/vectors/{Uri.EscapeDataString(success.Collection)}",
                success));
    }

    private static async Task<IResult> SearchAsync(
        VectorSearchRequest request,
        HttpContext httpContext,
        IVectorEndpointService vectorEndpointService,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var result = await vectorEndpointService
            .SearchAsync(request, traceId, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResultMapper.ToHttpResult(result, success => Results.Ok(success));
    }
}

using Api.Contracts;
using Api.Services;

namespace Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/admin")
            .WithTags("Admin");

        group.MapPost("/collections/init", InitializeCollectionAsync)
            .WithName("InitializeCollection")
            .Produces<InitializeCollectionResponse>(StatusCodes.Status201Created)
            .Produces<InitializeCollectionResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> InitializeCollectionAsync(
        InitializeCollectionRequest request,
        IAdminEndpointService adminEndpointService,
        CancellationToken cancellationToken)
    {
        var result = await adminEndpointService
            .InitializeCollectionAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return ServiceResultMapper.ToHttpResult(
            result,
            success => result.IsCreated
                ? Results.Created(
                    $"/api/v1/admin/collections/{Uri.EscapeDataString(success.CollectionName)}",
                    success)
                : Results.Ok(success));
    }
}

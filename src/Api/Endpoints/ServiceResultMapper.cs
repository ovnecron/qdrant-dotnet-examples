using Api.Services.Results;

namespace Api.Endpoints;

internal static class ServiceResultMapper
{
    public static IResult ToHttpResult<TValue>(
        ServiceResult<TValue> result,
        Func<TValue, IResult> successFactory)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(successFactory);

        if (result.ValidationErrors is { Count: > 0 })
        {
            return Results.ValidationProblem(result.ValidationErrors);
        }

        if (result.Failure is not null)
        {
            return MapFailure(result.Failure);
        }

        if (result.Value is null)
        {
            return Results.Problem(
                detail: "No response payload was returned by the service.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Service response missing");
        }

        return successFactory(result.Value);
    }

    private static IResult MapFailure(ServiceFailure failure)
    {
        var statusCode = failure.Kind switch
        {
            ServiceFailureKind.VectorStoreUnavailable => StatusCodes.Status503ServiceUnavailable,
            ServiceFailureKind.ConfigurationInvalid => StatusCodes.Status503ServiceUnavailable,
            ServiceFailureKind.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            detail: failure.Detail,
            statusCode: statusCode,
            title: failure.Title);
    }
}

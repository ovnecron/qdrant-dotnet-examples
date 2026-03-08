using Api.Contracts;
using Api.Services;

namespace Api.Endpoints;

public static class IngestEndpoints
{
    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/ingest")
            .WithTags("Ingest");

        group.MapPost("/markdown", IngestMarkdownAsync)
            .WithName("IngestMarkdown")
            .Produces<MarkdownIngestAcceptedResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/jobs/{jobId}", GetJobStatusAsync)
            .WithName("GetIngestionJobStatus")
            .Produces<IngestJobStatusResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> IngestMarkdownAsync(
        MarkdownIngestRequest request,
        IIngestionEndpointService ingestionEndpointService,
        CancellationToken cancellationToken)
    {
        var result = await ingestionEndpointService
            .IngestMarkdownAsync(request, cancellationToken);

        return ServiceResultMapper.ToHttpResult(
            result,
            success => Results.Accepted(
                $"/api/v1/ingest/jobs/{Uri.EscapeDataString(success.JobId)}",
                success));
    }

    private static async Task<IResult> GetJobStatusAsync(
        string jobId,
        IIngestionEndpointService ingestionEndpointService,
        CancellationToken cancellationToken)
    {
        var result = await ingestionEndpointService
            .GetJobStatusAsync(jobId, cancellationToken);

        return ServiceResultMapper.ToHttpResult(result, success => Results.Ok(success));
    }
}

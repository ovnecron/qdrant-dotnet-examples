using Api.Contracts;
using Api.Services.Results;

namespace Api.Services;

public interface IIngestionEndpointService
{
    Task<ServiceResult<MarkdownIngestAcceptedResponse>> IngestMarkdownAsync(
        MarkdownIngestRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<IngestJobStatusResponse>> GetJobStatusAsync(
        string jobId,
        CancellationToken cancellationToken);
}

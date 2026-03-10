namespace Api.Services;

internal interface ITextRetrievalService
{
    Task<TextRetrievalResult> RetrieveAsync(
        TextRetrievalRequest request,
        CancellationToken cancellationToken);
}

namespace Api.Services;

internal interface IRagAnswerGenerator
{
    Task<RagAnswerGenerationResult> GenerateAsync(
        RagAnswerGenerationRequest request,
        CancellationToken cancellationToken);
}

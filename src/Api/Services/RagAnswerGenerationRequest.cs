namespace Api.Services;

internal sealed record RagAnswerGenerationRequest
{
    public required string Question { get; init; }

    public required string Context { get; init; }

    public required IReadOnlyList<RagCitationDraft> Citations { get; init; }
}

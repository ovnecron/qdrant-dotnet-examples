namespace Api.Services;

internal sealed record RagAnswerGenerationResult(
    string Answer,
    RagAnswerGeneratorDescriptor Descriptor);

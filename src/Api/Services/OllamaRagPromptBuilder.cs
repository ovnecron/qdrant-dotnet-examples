namespace Api.Services;

internal sealed class OllamaRagPromptBuilder
{
    public string BuildSystemPrompt()
    {
        return """
            You answer questions using only the grounded context that is provided.
            Do not use outside knowledge.
            If the provided context is insufficient, say that you cannot answer from the provided evidence.
            Be concise and factual.
            Do not invent citations or sources.
            """;
    }

    public string BuildPrompt(RagAnswerGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return $$"""
            Question:
            {{request.Question}}

            Grounded context:
            {{request.Context}}

            Write a short answer using only the grounded context above.
            """;
    }
}

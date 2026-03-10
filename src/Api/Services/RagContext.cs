namespace Api.Services;

internal sealed record RagContext(
    string Context,
    IReadOnlyList<RagCitationDraft> Citations,
    int RetrievedHitCount);

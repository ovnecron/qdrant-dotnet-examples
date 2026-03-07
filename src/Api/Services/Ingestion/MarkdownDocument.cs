namespace Api.Services.Ingestion;

internal sealed record MarkdownDocument
{
    public required string DocId { get; init; }

    public required string Source { get; init; }

    public required string Markdown { get; init; }

    public string? Title { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}

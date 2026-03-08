using System.Diagnostics.CodeAnalysis;

using Api.Contracts;
using Api.Services.Ingestion;

namespace Api.Services.Validation;

internal interface IMarkdownIngestRequestParser
{
    bool TryParse(
        MarkdownIngestRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out MarkdownIngestCommand? command,
        out Dictionary<string, string[]> errors);
}

internal sealed class MarkdownIngestRequestParser : IMarkdownIngestRequestParser
{
    public bool TryParse(
        MarkdownIngestRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out MarkdownIngestCommand? command,
        out Dictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(request);

        errors = new Dictionary<string, string[]>();
        command = null;

        var defaults = new MarkdownChunkingOptions();
        var collectionName = ResolveCollectionName(request.Collection, defaultCollectionName);
        var docId = request.DocId?.Trim();
        var sourceId = request.SourceId?.Trim();
        var markdown = request.Markdown?.Trim();
        var title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        var tenantId = string.IsNullOrWhiteSpace(request.TenantId) ? null : request.TenantId.Trim();
        var chunkSize = request.ChunkSize ?? defaults.ChunkSize;
        var chunkOverlap = request.ChunkOverlap ?? defaults.ChunkOverlap;

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        if (string.IsNullOrWhiteSpace(docId))
        {
            errors["docId"] = ["Doc id is required."];
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            errors["sourceId"] = ["Source id is required."];
        }

        if (string.IsNullOrWhiteSpace(markdown))
        {
            errors["markdown"] = ["Markdown is required."];
        }

        if (chunkSize <= 0)
        {
            errors["chunkSize"] = ["Chunk size must be greater than zero."];
        }

        if (chunkOverlap < 0)
        {
            errors["chunkOverlap"] = ["Chunk overlap cannot be negative."];
        }
        else if (chunkOverlap >= chunkSize && chunkSize > 0)
        {
            errors["chunkOverlap"] = ["Chunk overlap must be smaller than chunk size."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        command = new MarkdownIngestCommand(
            collectionName,
            docId!,
            sourceId!,
            title,
            markdown!,
            (request.Tags ?? [])
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            tenantId,
            chunkSize,
            chunkOverlap);

        return true;
    }

    private static string ResolveCollectionName(string? requestedCollection, string defaultCollectionName)
    {
        return string.IsNullOrWhiteSpace(requestedCollection)
            ? defaultCollectionName.Trim()
            : requestedCollection.Trim();
    }
}

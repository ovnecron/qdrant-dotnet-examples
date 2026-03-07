using System.Security.Cryptography;
using System.Text;

namespace Api.Services.Ingestion;

internal sealed class MarkdownChunker : IMarkdownChunker
{
    public IReadOnlyList<MarkdownChunk> Chunk(
        MarkdownDocument document,
        MarkdownChunkingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var docId = document.DocId?.Trim();
        if (string.IsNullOrWhiteSpace(docId))
        {
            throw new ArgumentException("Document id must be provided.", nameof(document));
        }

        var source = document.Source?.Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source must be provided.", nameof(document));
        }

        var normalizedMarkdown = NormalizeMarkdown(document.Markdown);
        if (string.IsNullOrWhiteSpace(normalizedMarkdown))
        {
            throw new ArgumentException("Markdown must be provided.", nameof(document));
        }

        var resolvedOptions = ValidateOptions(options);
        var resolvedTitle = ResolveDocumentTitle(normalizedMarkdown, document.Title, docId);
        var normalizedTags = (document.Tags ?? [])
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var blocks = ParseBlocks(normalizedMarkdown, resolvedTitle);
        if (blocks.Count == 0)
        {
            throw new ArgumentException("Markdown does not contain chunkable content.", nameof(document));
        }

        var chunks = new List<MarkdownChunk>();

        foreach (var block in blocks)
        {
            foreach (var segment in SplitBlock(block.Content, resolvedOptions))
            {
                var checksum = ComputeSha256(segment);
                chunks.Add(
                    new MarkdownChunk
                    {
                        ChunkId = $"{docId}:{chunks.Count}:{checksum[..4]}",
                        ChunkIndex = chunks.Count,
                        DocId = docId,
                        Source = source,
                        Title = resolvedTitle,
                        Section = block.Section,
                        Content = segment,
                        Checksum = checksum,
                        Tags = normalizedTags
                    });
            }
        }

        return chunks;
    }

    private static MarkdownChunkingOptions ValidateOptions(MarkdownChunkingOptions? options)
    {
        var resolvedOptions = options ?? new MarkdownChunkingOptions();

        if (resolvedOptions.ChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Chunk size must be greater than zero.");
        }

        if (resolvedOptions.ChunkOverlap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Chunk overlap cannot be negative.");
        }

        if (resolvedOptions.ChunkOverlap >= resolvedOptions.ChunkSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Chunk overlap must be smaller than chunk size.");
        }

        return resolvedOptions;
    }

    private static string ResolveDocumentTitle(string markdown, string? explicitTitle, string fallbackDocId)
    {
        if (!string.IsNullOrWhiteSpace(explicitTitle))
        {
            return explicitTitle.Trim();
        }

        foreach (var line in markdown.Split('\n'))
        {
            if (TryParseHeading(line, out var level, out var text) && level == 1)
            {
                return text;
            }
        }

        return fallbackDocId;
    }

    private static List<SectionBlock> ParseBlocks(string markdown, string documentTitle)
    {
        var headingPath = new List<string>();
        var currentLines = new List<string>();
        var blocks = new List<SectionBlock>();

        void FlushCurrentBlock()
        {
            var content = NormalizeBlockContent(currentLines);
            currentLines.Clear();

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            blocks.Add(
                new SectionBlock(
                    ResolveSection(headingPath, documentTitle),
                    content));
        }

        foreach (var line in markdown.Split('\n'))
        {
            if (TryParseHeading(line, out var level, out var text))
            {
                FlushCurrentBlock();
                UpdateHeadingPath(headingPath, level, text);
                continue;
            }

            currentLines.Add(line);
        }

        FlushCurrentBlock();
        return blocks;
    }

    private static IEnumerable<string> SplitBlock(string content, MarkdownChunkingOptions options)
    {
        var start = 0;

        while (start < content.Length)
        {
            var remaining = content.Length - start;
            if (remaining <= options.ChunkSize)
            {
                var finalSegment = content[start..].Trim();
                if (finalSegment.Length > 0)
                {
                    yield return finalSegment;
                }

                yield break;
            }

            var end = FindChunkEnd(content, start, options.ChunkSize, options.ChunkOverlap + 1);
            var chunkStart = start;
            var chunkEnd = end;

            while (chunkStart < chunkEnd && char.IsWhiteSpace(content[chunkStart]))
            {
                chunkStart++;
            }

            while (chunkEnd > chunkStart && char.IsWhiteSpace(content[chunkEnd - 1]))
            {
                chunkEnd--;
            }

            if (chunkEnd <= chunkStart)
            {
                yield break;
            }

            yield return content[chunkStart..chunkEnd];

            start = Math.Max(chunkStart + 1, chunkEnd - options.ChunkOverlap);
        }
    }

    // Prefer paragraph and sentence boundaries before falling back to a hard cut.
    private static int FindChunkEnd(string content, int start, int chunkSize, int minimumChunkLength)
    {
        var hardEnd = Math.Min(content.Length, start + chunkSize);
        if (hardEnd >= content.Length)
        {
            return content.Length;
        }

        var searchStart = Math.Min(hardEnd, start + Math.Max(minimumChunkLength, chunkSize / 2));

        for (var index = hardEnd; index > searchStart; index--)
        {
            if (index > start + 1 &&
                content[index - 1] == '\n' &&
                content[index - 2] == '\n')
            {
                return index;
            }
        }

        for (var index = hardEnd; index > searchStart; index--)
        {
            if (content[index - 1] == '\n')
            {
                return index;
            }
        }

        for (var index = hardEnd; index > searchStart; index--)
        {
            if (IsSentenceBoundary(content, index))
            {
                return index;
            }
        }

        for (var index = hardEnd; index > searchStart; index--)
        {
            if (char.IsWhiteSpace(content[index - 1]))
            {
                return index;
            }
        }

        return hardEnd;
    }

    private static bool IsSentenceBoundary(string content, int index)
    {
        if (index <= 1 || index > content.Length)
        {
            return false;
        }

        var previous = content[index - 1];
        if (previous is not ('.' or '!' or '?'))
        {
            return false;
        }

        return index == content.Length || char.IsWhiteSpace(content[index]);
    }

    private static string? ResolveSection(IReadOnlyList<string> headingPath, string documentTitle)
    {
        if (headingPath.Count == 0)
        {
            return null;
        }

        var sectionParts = headingPath[0].Equals(documentTitle, StringComparison.Ordinal)
            ? headingPath.Skip(1).ToArray()
            : headingPath.ToArray();

        return sectionParts.Length == 0 ? null : string.Join(" / ", sectionParts);
    }

    private static void UpdateHeadingPath(List<string> headingPath, int level, string text)
    {
        while (headingPath.Count >= level)
        {
            headingPath.RemoveAt(headingPath.Count - 1);
        }

        headingPath.Add(text);
    }

    private static bool TryParseHeading(string line, out int level, out string text)
    {
        level = 0;
        text = string.Empty;

        var trimmedLine = line.Trim();
        if (trimmedLine.Length < 2 || trimmedLine[0] != '#')
        {
            return false;
        }

        var markerLength = 0;
        while (markerLength < trimmedLine.Length &&
               markerLength < 6 &&
               trimmedLine[markerLength] == '#')
        {
            markerLength++;
        }

        if (markerLength == 0 ||
            markerLength == trimmedLine.Length ||
            trimmedLine[markerLength] != ' ')
        {
            return false;
        }

        var headingText = trimmedLine[(markerLength + 1)..].Trim();
        if (headingText.Length == 0)
        {
            return false;
        }

        level = markerLength;
        text = headingText;
        return true;
    }

    private static string NormalizeMarkdown(string markdown)
    {
        return (markdown ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    private static string NormalizeBlockContent(IEnumerable<string> lines)
    {
        var content = string.Join('\n', lines).Trim();

        while (content.Contains("\n\n\n", StringComparison.Ordinal))
        {
            content = content.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
        }

        return content;
    }

    private static string ComputeSha256(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }

    private sealed record SectionBlock(string? Section, string Content);
}

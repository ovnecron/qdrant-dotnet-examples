using System.Text;

namespace Api.Services;

internal sealed class RagContextAssembler : IRagContextAssembler
{
    private const int MaxContextChunks = 4;
    private const int MaxContextCharacters = 4000;

    public RagContext Assemble(TextRetrievalResult retrieval)
    {
        ArgumentNullException.ThrowIfNull(retrieval);

        var citations = new List<RagCitationDraft>(Math.Min(retrieval.Hits.Count, MaxContextChunks));
        var builder = new StringBuilder();

        foreach (var hit in retrieval.Hits.Take(MaxContextChunks))
        {
            var content = NormalizeContent(hit.Content);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var nextIndex = citations.Count + 1;
            var block = BuildContextBlock(nextIndex, hit.Source, hit.Title, hit.Section, content);
            var separatorLength = builder.Length == 0 ? 0 : Environment.NewLine.Length * 2;

            if (builder.Length + separatorLength + block.Length > MaxContextCharacters)
            {
                var remainingCharacters = MaxContextCharacters - builder.Length - separatorLength;
                if (remainingCharacters <= 0)
                {
                    break;
                }

                var truncatedContent = TruncateContent(content, remainingCharacters);
                if (string.IsNullOrWhiteSpace(truncatedContent))
                {
                    break;
                }

                block = BuildContextBlock(nextIndex, hit.Source, hit.Title, hit.Section, truncatedContent);
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(block);
            citations.Add(
                new RagCitationDraft(
                    hit.ChunkId,
                    hit.DocId,
                    hit.Source,
                    hit.Title,
                    hit.Section,
                    hit.Score,
                    content));
        }

        return new RagContext(
            builder.ToString(),
            citations,
            retrieval.Hits.Count);
    }

    private static string BuildContextBlock(int index, string source, string? title, string? section, string content)
    {
        var builder = new StringBuilder();

        builder.Append('[').Append(index).Append("] Source: ").Append(source);

        if (!string.IsNullOrWhiteSpace(title))
        {
            builder.AppendLine();
            builder.Append("Title: ").Append(title.Trim());
        }

        if (!string.IsNullOrWhiteSpace(section))
        {
            builder.AppendLine();
            builder.Append("Section: ").Append(section.Trim());
        }

        builder.AppendLine();
        builder.Append("Content:").AppendLine();
        builder.Append(content);

        return builder.ToString();
    }

    private static string NormalizeContent(string content)
    {
        return string.Join(
            Environment.NewLine,
            content
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string TruncateContent(string content, int maxCharacters)
    {
        const string ellipsis = "...";

        if (maxCharacters <= ellipsis.Length)
        {
            return string.Empty;
        }

        if (content.Length <= maxCharacters)
        {
            return content;
        }

        var truncatedLength = maxCharacters - ellipsis.Length;
        return content[..truncatedLength].TrimEnd() + ellipsis;
    }
}

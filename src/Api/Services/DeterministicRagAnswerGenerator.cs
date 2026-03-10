using System.Text;
using System.Text.RegularExpressions;

namespace Api.Services;

internal sealed class DeterministicRagAnswerGenerator : IRagAnswerGenerator
{
    public const string ProviderName = "Deterministic";
    public const string ModelName = "grounded-answer-v1";

    private static readonly HashSet<string> StopWords =
    [
        "a",
        "an",
        "and",
        "are",
        "as",
        "at",
        "by",
        "do",
        "for",
        "from",
        "how",
        "i",
        "in",
        "is",
        "it",
        "locally",
        "of",
        "on",
        "or",
        "the",
        "this",
        "to",
        "when",
        "with"
    ];

    public Task<RagAnswerGenerationResult> GenerateAsync(
        RagAnswerGenerationRequest request,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        ArgumentNullException.ThrowIfNull(request);

        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question must be provided.", nameof(request));
        }

        if (request.Citations.Count == 0)
        {
            throw new ArgumentException("At least one citation is required.", nameof(request));
        }

        var questionTokens = Tokenize(question)
            .Where(static token => token.Length > 2)
            .Where(static token => !StopWords.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var candidates = request.Citations
            .SelectMany(
                (citation, citationIndex) => SplitSentences(citation.Content)
                    .Select(
                        (sentence, sentenceIndex) => new SentenceCandidate(
                            Text: sentence,
                            CitationIndex: citationIndex,
                            SentenceIndex: sentenceIndex,
                            Score: ScoreSentence(sentence, questionTokens, citation.Score))))
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.Text))
            .OrderByDescending(static candidate => candidate.Score)
            .ThenBy(static candidate => candidate.CitationIndex)
            .ThenBy(static candidate => candidate.SentenceIndex)
            .ToArray();

        var selectedSentences = SelectSentences(candidates);
        var answer = selectedSentences.Count > 0
            ? string.Join(" ", selectedSentences)
            : BuildFallbackAnswer(request.Citations[0].Content);

        return Task.FromResult(
            new RagAnswerGenerationResult(
                answer,
                new RagAnswerGeneratorDescriptor(ProviderName, ModelName)));
    }

    private static string BuildFallbackAnswer(string content)
    {
        var normalized = NormalizeWhitespace(content);
        if (normalized.Length <= 280)
        {
            return normalized;
        }

        return normalized[..277].TrimEnd() + "...";
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text, "\\s+", " ").Trim();
    }

    private static float ScoreSentence(string sentence, IReadOnlyList<string> questionTokens, float citationScore)
    {
        if (questionTokens.Count == 0)
        {
            return citationScore;
        }

        var sentenceTokens = Tokenize(sentence)
            .ToHashSet(StringComparer.Ordinal);

        var overlap = questionTokens.Count(sentenceTokens.Contains);
        return overlap + citationScore;
    }

    private static IReadOnlyList<string> SelectSentences(IReadOnlyList<SentenceCandidate> candidates)
    {
        var selected = new List<string>(capacity: 2);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var currentLength = 0;

        foreach (var candidate in candidates)
        {
            var normalized = NormalizeWhitespace(candidate.Text);
            if (normalized.Length == 0 || !seen.Add(normalized))
            {
                continue;
            }

            var nextLength = currentLength == 0
                ? normalized.Length
                : currentLength + 1 + normalized.Length;

            if (nextLength > 320)
            {
                continue;
            }

            selected.Add(normalized);
            currentLength = nextLength;

            if (selected.Count == 2)
            {
                break;
            }
        }

        return selected;
    }

    private static IEnumerable<string> SplitSentences(string content)
    {
        return Regex
            .Split(content, @"(?<=[\.!\?])\s+|\r?\n+")
            .Select(static sentence => sentence.Trim())
            .Where(static sentence => sentence.Length > 0);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]+")
            .Select(static match => match.Value);
    }

    private sealed record SentenceCandidate(
        string Text,
        int CitationIndex,
        int SentenceIndex,
        float Score);
}

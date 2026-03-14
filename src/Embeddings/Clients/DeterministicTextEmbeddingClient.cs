using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

using Embeddings.Contracts;
using Embeddings.Interfaces;
using Embeddings.Options;

using Microsoft.Extensions.Options;

namespace Embeddings.Clients;

public sealed class DeterministicTextEmbeddingClient : ITextEmbeddingClient
{
    public const string ProviderName = "Deterministic";

    private readonly EmbeddingDescriptor _descriptor;

    public DeterministicTextEmbeddingClient(IOptions<EmbeddingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var resolvedOptions = options.Value ?? throw new InvalidOperationException("Embedding options are not configured.");
        _descriptor = new EmbeddingDescriptor
        {
            Provider = ProviderName,
            Model = resolvedOptions.Model,
            Dimension = resolvedOptions.Dimension,
            SchemaVersion = resolvedOptions.SchemaVersion
        };
    }

    public async Task<TextEmbeddingResult> EmbedAsync(
        TextEmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = await EmbedBatchAsync([request], cancellationToken);
        return results[0];
    }

    public Task<IReadOnlyList<TextEmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<TextEmbeddingRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var results = new List<TextEmbeddingResult>(requests.Count);

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(EmbedCore(request));
        }

        return Task.FromResult<IReadOnlyList<TextEmbeddingResult>>(results);
    }

    private TextEmbeddingResult EmbedCore(TextEmbeddingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedText = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedText))
        {
            throw new ArgumentException("Embedding text must be provided.", nameof(request));
        }

        var vector = CreateVector(trimmedText);

        return new TextEmbeddingResult
        {
            Text = trimmedText,
            Kind = request.Kind,
            Vector = vector,
            Descriptor = _descriptor
        };
    }

    private IReadOnlyList<float> CreateVector(string text)
    {
        var tokens = Tokenize(text);
        if (tokens.Count == 0)
        {
            throw new ArgumentException("Embedding text must contain at least one token.", nameof(text));
        }

        var values = new float[_descriptor.Dimension];
        string? previousToken = null;

        foreach (var token in tokens)
        {
            ApplyFeature(values, token, 1.0f);

            if (previousToken is not null)
            {
                ApplyFeature(values, $"{previousToken}|{token}", 0.5f);
            }

            previousToken = token;
        }

        Normalize(values);
        return values;
    }

    private static IReadOnlyList<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var builder = new StringBuilder();

        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            FlushToken(builder, tokens);
        }

        FlushToken(builder, tokens);
        return tokens;
    }

    private static void FlushToken(StringBuilder builder, List<string> tokens)
    {
        if (builder.Length == 0)
        {
            return;
        }

        tokens.Add(builder.ToString());
        builder.Clear();
    }

    private static void ApplyFeature(float[] values, string feature, float weight)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(feature));
        var rawIndex = BinaryPrimitives.ReadUInt32LittleEndian(hash.AsSpan(0, sizeof(uint)));
        var index = (int)(rawIndex % values.Length);
        var sign = (hash[4] & 1) == 0 ? 1.0f : -1.0f;

        values[index] += sign * weight;
    }

    private static void Normalize(float[] values)
    {
        var sumOfSquares = 0.0f;

        foreach (var value in values)
        {
            sumOfSquares += value * value;
        }

        if (sumOfSquares <= 0.0f)
        {
            return;
        }

        var magnitude = MathF.Sqrt(sumOfSquares);
        for (var index = 0; index < values.Length; index++)
        {
            values[index] /= magnitude;
        }
    }
}

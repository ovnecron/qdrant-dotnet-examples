using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Embeddings.Contracts;
using Embeddings.Interfaces;
using Embeddings.Options;

using Microsoft.Extensions.Options;

namespace Embeddings.Clients;

public sealed class OllamaTextEmbeddingClient : ITextEmbeddingClient
{
    public const string ProviderName = "Ollama";

    private readonly EmbeddingDescriptor _descriptor;
    private readonly HttpClient _httpClient;

    public OllamaTextEmbeddingClient(HttpClient httpClient, IOptions<EmbeddingOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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

    public async Task<IReadOnlyList<TextEmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<TextEmbeddingRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            return [];
        }

        var normalizedRequests = requests
            .Select(NormalizeRequest)
            .ToArray();

        var response = await _httpClient.PostAsJsonAsync(
            "embed",
            new OllamaEmbedRequest
            {
                Model = _descriptor.Model,
                Input = normalizedRequests.Select(static request => request.Text).ToArray(),
                Dimensions = _descriptor.Dimension
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Ollama embedding request failed with status code {(int)response.StatusCode}: {errorContent}",
                inner: null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: cancellationToken);
        if (payload?.Embeddings is null || payload.Embeddings.Count == 0)
        {
            throw new InvalidOperationException("Ollama embedding response did not contain any vectors.");
        }

        if (payload.Embeddings.Count != normalizedRequests.Length)
        {
            throw new InvalidOperationException("Ollama embedding response count does not match request count.");
        }

        var results = new TextEmbeddingResult[normalizedRequests.Length];

        for (var index = 0; index < normalizedRequests.Length; index++)
        {
            var vector = payload.Embeddings[index];
            if (vector is null || vector.Count == 0)
            {
                throw new InvalidOperationException($"Ollama embedding response at index {index} did not contain a vector.");
            }

            if (vector.Count != _descriptor.Dimension)
            {
                throw new InvalidOperationException(
                    $"Ollama returned vector dimension {vector.Count}, but {nameof(EmbeddingOptions.Dimension)} is configured as {_descriptor.Dimension}.");
            }

            results[index] = new TextEmbeddingResult
            {
                Text = normalizedRequests[index].Text,
                Kind = normalizedRequests[index].Kind,
                Vector = vector.ToArray(),
                Descriptor = _descriptor
            };
        }

        return results;
    }

    private static TextEmbeddingRequest NormalizeRequest(TextEmbeddingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedText = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedText))
        {
            throw new ArgumentException("Embedding text must be provided.", nameof(request));
        }

        return new TextEmbeddingRequest
        {
            Text = trimmedText,
            Kind = request.Kind
        };
    }

    private sealed record OllamaEmbedRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("input")]
        public required IReadOnlyList<string> Input { get; init; }

        [JsonPropertyName("dimensions")]
        public int Dimensions { get; init; }
    }

    private sealed record OllamaEmbedResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("embeddings")]
        public IReadOnlyList<IReadOnlyList<float>>? Embeddings { get; init; }
    }
}

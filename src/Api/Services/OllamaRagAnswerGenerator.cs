using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Api.Options;
using Api.Services.Results;

using Embeddings.Clients;

using Microsoft.Extensions.Options;

namespace Api.Services;

internal sealed class OllamaRagAnswerGenerator : IRagAnswerGenerator
{
    public const string ProviderName = "Ollama";

    private readonly HttpClient _httpClient;
    private readonly RagOptions _options;
    private readonly OllamaRagPromptBuilder _promptBuilder;

    public OllamaRagAnswerGenerator(
        HttpClient httpClient,
        IOptions<RagOptions> options,
        OllamaRagPromptBuilder promptBuilder)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new InvalidOperationException("RAG options are not configured.");
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
    }

    public async Task<RagAnswerGenerationResult> GenerateAsync(
        RagAnswerGenerationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var answerModel = _options.AnswerModel?.Trim();
        if (string.IsNullOrWhiteSpace(answerModel))
        {
            throw new InvalidOperationException("Rag:AnswerModel must be configured when Rag:AnswerProvider is Ollama.");
        }

        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question must be provided.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Context))
        {
            throw new ArgumentException("Grounded context must be provided.", nameof(request));
        }

        if (request.Citations.Count == 0)
        {
            throw new ArgumentException("At least one citation is required.", nameof(request));
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "generate",
                new OllamaGenerateRequest
                {
                    Model = answerModel,
                    System = _promptBuilder.BuildSystemPrompt(),
                    Prompt = _promptBuilder.BuildPrompt(request),
                    Stream = false,
                    Options = new OllamaGenerateOptions
                    {
                        Temperature = _options.Temperature,
                        NumPredict = _options.MaxAnswerTokens
                    }
                },
                linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw Unavailable("The Ollama answer request timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw Unavailable($"The Ollama answer request failed: {exception.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw Unavailable(
                $"Ollama returned status code {(int)response.StatusCode}: {errorContent}");
        }

        OllamaGenerateResponse? payload;

        try
        {
            payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
        }
        catch (NotSupportedException exception)
        {
            throw Unavailable($"Ollama returned an unsupported response payload: {exception.Message}");
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw Unavailable($"Ollama returned invalid JSON: {exception.Message}");
        }

        var answer = payload?.Response?.Trim();
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw Unavailable("Ollama returned an empty answer.");
        }

        return new RagAnswerGenerationResult(
            answer,
            new RagAnswerGeneratorDescriptor(ProviderName, answerModel));
    }

    private static ServiceFailureException Unavailable(string detail)
    {
        return new ServiceFailureException(
            new ServiceFailure(
                ServiceFailureKind.AnswerProviderUnavailable,
                "Answer provider unavailable",
                detail));
    }

    private sealed record OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("system")]
        public required string System { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("options")]
        public required OllamaGenerateOptions Options { get; init; }
    }

    private sealed record OllamaGenerateOptions
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; init; }

        [JsonPropertyName("num_predict")]
        public int NumPredict { get; init; }
    }

    private sealed record OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; init; }
    }
}

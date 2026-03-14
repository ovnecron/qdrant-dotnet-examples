using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Api.Options;
using Api.Services;
using Api.Services.Results;

using Embeddings.Clients;

using Microsoft.Extensions.Options;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class OllamaRagAnswerGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_SendsExpectedRequest_AndMapsResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var generator = CreateGenerator(
            new RecordingHttpMessageHandler(
                async request =>
                {
                    capturedRequest = request;
                    capturedBody = await request.Content!.ReadAsStringAsync();

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(
                            new
                            {
                                response = " Check the /health and /ready endpoints after startup. "
                            })
                    };
                }),
            new RagOptions
            {
                AnswerProvider = RagAnswerProvider.Ollama,
                AnswerModel = "llama3.2:3b",
                BaseUrl = "http://localhost:11434/api",
                Temperature = 0.15f,
                MaxAnswerTokens = 192,
                RequestTimeoutSeconds = 15
            });

        var result = await generator.GenerateAsync(
            new RagAnswerGenerationRequest
            {
                Question = "How do I check health and ready endpoints locally?",
                Context = """
                    [1] Source: docs/local-run.md
                    Title: Local Run
                    Content:
                    Check the /health and /ready endpoints after startup.
                    """,
                Citations =
                [
                    new RagCitationDraft(
                        ChunkId: "guide-local-run:0",
                        DocId: "guide-local-run",
                        Source: "docs/local-run.md",
                        Title: "Local Run",
                        Section: null,
                        Score: 0.91f,
                        Content: "Check the /health and /ready endpoints after startup.")
                ]
            },
            CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("http://localhost:11434/api/generate", capturedRequest.RequestUri!.ToString());

        Assert.NotNull(capturedBody);
        using var document = JsonDocument.Parse(capturedBody!);
        Assert.Equal("llama3.2:3b", document.RootElement.GetProperty("model").GetString());
        Assert.False(document.RootElement.GetProperty("stream").GetBoolean());
        Assert.Contains(
            "Do not use outside knowledge",
            document.RootElement.GetProperty("system").GetString(),
            StringComparison.Ordinal);
        Assert.Contains(
            "How do I check health and ready endpoints locally?",
            document.RootElement.GetProperty("prompt").GetString(),
            StringComparison.Ordinal);
        Assert.Contains(
            "Check the /health and /ready endpoints after startup.",
            document.RootElement.GetProperty("prompt").GetString(),
            StringComparison.Ordinal);
        Assert.Equal(0.15f, document.RootElement.GetProperty("options").GetProperty("temperature").GetSingle());
        Assert.Equal(192, document.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32());

        Assert.Equal("Ollama", result.Descriptor.Provider);
        Assert.Equal("llama3.2:3b", result.Descriptor.Model);
        Assert.Equal("Check the /health and /ready endpoints after startup.", result.Answer);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsServiceFailureException_WhenOllamaReturnsAnError()
    {
        var generator = CreateGenerator(
            new RecordingHttpMessageHandler(
                _ => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = JsonContent.Create(new { error = "model not available" })
                    })),
            new RagOptions
            {
                AnswerProvider = RagAnswerProvider.Ollama,
                AnswerModel = "llama3.2:3b",
                BaseUrl = "http://localhost:11434/api",
                Temperature = 0.0f,
                MaxAnswerTokens = 128,
                RequestTimeoutSeconds = 15
            });

        var exception = await Assert.ThrowsAsync<ServiceFailureException>(
            () => generator.GenerateAsync(
                new RagAnswerGenerationRequest
                {
                    Question = "How do I check health and ready endpoints locally?",
                    Context = "Grounded context",
                    Citations =
                    [
                        new RagCitationDraft(
                            ChunkId: "guide-local-run:0",
                            DocId: "guide-local-run",
                            Source: "docs/local-run.md",
                            Title: "Local Run",
                            Section: null,
                            Score: 0.91f,
                            Content: "Check the /health and /ready endpoints after startup.")
                    ]
                },
                CancellationToken.None));

        Assert.Equal(ServiceFailureKind.AnswerProviderUnavailable, exception.Failure.Kind);
        Assert.Equal("Answer provider unavailable", exception.Failure.Title);
    }

    private static OllamaRagAnswerGenerator CreateGenerator(
        HttpMessageHandler handler,
        RagOptions options)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = OllamaHttpClientConfiguration.ResolveBaseAddress(options.BaseUrl)
        };

        return new OllamaRagAnswerGenerator(
            httpClient,
            Options.Create(options),
            new OllamaRagPromptBuilder());
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return _handler(request);
        }
    }
}

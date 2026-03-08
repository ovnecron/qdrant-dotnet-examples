using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Embeddings.Clients;
using Embeddings.Contracts;
using Embeddings.Options;

using Microsoft.Extensions.Options;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class OllamaTextEmbeddingClientTests
{
    [Fact]
    public async Task EmbedBatchAsync_SendsExpectedRequest_AndMapsResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var client = CreateClient(
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
                                model = "embeddinggemma",
                                embeddings = new[]
                                {
                                    new[] { 0.1f, 0.2f, 0.3f },
                                    new[] { 0.4f, 0.5f, 0.6f }
                                }
                            })
                    };
                }),
            new EmbeddingOptions
            {
                Provider = "Ollama",
                Model = "embeddinggemma",
                Dimension = 3,
                BatchSize = 8,
                SchemaVersion = "v1",
                BaseUrl = "http://localhost:11434/api"
            });

        var results = await client.EmbedBatchAsync(
            [
                new TextEmbeddingRequest { Text = " first item ", Kind = EmbeddingKind.Document },
                new TextEmbeddingRequest { Text = "second item", Kind = EmbeddingKind.Query }
            ]);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("http://localhost:11434/api/embed", capturedRequest.RequestUri!.ToString());

        Assert.NotNull(capturedBody);
        using var document = JsonDocument.Parse(capturedBody!);
        Assert.Equal("embeddinggemma", document.RootElement.GetProperty("model").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("dimensions").GetInt32());
        Assert.Equal("first item", document.RootElement.GetProperty("input")[0].GetString());
        Assert.Equal("second item", document.RootElement.GetProperty("input")[1].GetString());

        Assert.Equal(2, results.Count);
        Assert.Equal("first item", results[0].Text);
        Assert.Equal(EmbeddingKind.Document, results[0].Kind);
        Assert.Equal("Ollama", results[0].Descriptor.Provider);
        Assert.Equal("embeddinggemma", results[0].Descriptor.Model);
        Assert.Equal("v1", results[0].Descriptor.SchemaVersion);
        Assert.Equal([0.1f, 0.2f, 0.3f], results[0].Vector);
        Assert.Equal("second item", results[1].Text);
        Assert.Equal(EmbeddingKind.Query, results[1].Kind);
    }

    [Fact]
    public async Task EmbedAsync_Throws_WhenResponseVectorDimensionDoesNotMatchConfiguration()
    {
        var client = CreateClient(
            new RecordingHttpMessageHandler(
                _ => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(
                            new
                            {
                                model = "embeddinggemma",
                                embeddings = new[]
                                {
                                    new[] { 0.1f, 0.2f }
                                }
                            })
                    })),
            new EmbeddingOptions
            {
                Provider = "Ollama",
                Model = "embeddinggemma",
                Dimension = 3,
                BatchSize = 8,
                SchemaVersion = "v1",
                BaseUrl = "http://localhost:11434/api"
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.EmbedAsync(
                new TextEmbeddingRequest
                {
                    Text = "dimension mismatch",
                    Kind = EmbeddingKind.Query
                }));

        Assert.Contains("dimension", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmbedBatchAsync_Throws_WhenResponseCountDoesNotMatchRequestCount()
    {
        var client = CreateClient(
            new RecordingHttpMessageHandler(
                _ => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(
                            new
                            {
                                model = "embeddinggemma",
                                embeddings = new[]
                                {
                                    new[] { 0.1f, 0.2f, 0.3f }
                                }
                            })
                    })),
            new EmbeddingOptions
            {
                Provider = "Ollama",
                Model = "embeddinggemma",
                Dimension = 3,
                BatchSize = 8,
                SchemaVersion = "v1",
                BaseUrl = "http://localhost:11434/api"
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.EmbedBatchAsync(
                [
                    new TextEmbeddingRequest { Text = "first", Kind = EmbeddingKind.Document },
                    new TextEmbeddingRequest { Text = "second", Kind = EmbeddingKind.Query }
                ]));

        Assert.Contains("count", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OllamaTextEmbeddingClient CreateClient(
        HttpMessageHandler handler,
        EmbeddingOptions options)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = OllamaTextEmbeddingClient.ResolveBaseAddress(options.BaseUrl)
        };

        return new OllamaTextEmbeddingClient(httpClient, Options.Create(options));
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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Api.Contracts;

using Integration.Fixtures;

namespace Integration.Qdrant;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RagEndpointsIntegrationTests
{
    private readonly ApiIntegrationFixture _fixture;

    public RagEndpointsIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Query_ReturnsGroundedAnswerCitationsAndDebugMetadata()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);

        await IngestAsync(
            client,
            collectionName,
            "guide-local-run",
            "docs/local-run.md",
            "Local Run",
            "# Local Run\n\nCheck the /health and /ready endpoints after starting AppHost.",
            ["tutorial", "local"],
            tenantId: "tenant-a");

        await IngestAsync(
            client,
            collectionName,
            "guide-vector-delete",
            "docs/vector-delete.md",
            "Delete Vectors",
            "# Delete Vectors\n\nDelete vectors by chunk ids when you want to remove stale content.",
            ["vector", "admin"],
            tenantId: "tenant-a");

        var response = await client.PostAsJsonAsync(
            "/api/v1/rag/query",
            new RagQueryRequest
            {
                Collection = collectionName,
                Question = "How do I check health and ready endpoints locally?",
                TopK = 3,
                IncludeDebug = true,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-a",
                    TagsAny = ["local"]
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RagQueryResponse>();

        Assert.NotNull(payload);
        Assert.Contains("/health", payload.Answer, StringComparison.Ordinal);
        Assert.Contains("/ready", payload.Answer, StringComparison.Ordinal);
        Assert.Contains(payload.Citations, citation => citation.DocId == "guide-local-run");

        var debug = Assert.IsType<RagDebugResponse>(payload.Debug);
        Assert.Equal(collectionName, debug.Collection);
        Assert.Equal("Deterministic", debug.EmbeddingProvider);
        Assert.Equal("hashing-text-v1", debug.EmbeddingModel);
        Assert.Equal("v1", debug.EmbeddingSchemaVersion);
        Assert.Equal("Deterministic", debug.AnswerProvider);
        Assert.Equal("grounded-answer-v1", debug.AnswerModel);
        Assert.True(debug.RetrievedHitCount >= 1);
    }

    [Fact]
    public async Task Query_ReturnsUnprocessableEntity_WhenNoGroundedEvidenceFound()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/rag/query",
            new RagQueryRequest
            {
                Collection = collectionName,
                Question = "How do I check health and ready endpoints locally?",
                TopK = 3,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        Assert.Equal("Insufficient grounded context", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(422, document.RootElement.GetProperty("status").GetInt32());
    }

    private static async Task InitializeEmbeddingCollectionAsync(HttpClient client, string collectionName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/collections/init",
            new InitializeCollectionRequest
            {
                CollectionName = collectionName,
                VectorSize = 384,
                Distance = "Cosine"
            });

        Assert.True(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
            $"Expected collection init to return 201 or 200, got {(int)response.StatusCode}.");
    }

    private static async Task IngestAsync(
        HttpClient client,
        string collectionName,
        string docId,
        string sourceId,
        string title,
        string markdown,
        IReadOnlyList<string> tags,
        string tenantId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/ingest/markdown",
            new MarkdownIngestRequest
            {
                Collection = collectionName,
                DocId = docId,
                SourceId = sourceId,
                Title = title,
                Markdown = markdown,
                Tags = tags,
                TenantId = tenantId
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var accepted = await response.Content.ReadFromJsonAsync<MarkdownIngestAcceptedResponse>();
        Assert.NotNull(accepted);

        var status = await WaitForTerminalJobStatusAsync(client, accepted.JobId);
        Assert.Equal("Succeeded", status.Status);
    }

    private static async Task<IngestJobStatusResponse> WaitForTerminalJobStatusAsync(
        HttpClient client,
        string jobId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var response = await client.GetAsync($"/api/v1/ingest/jobs/{jobId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var payload = await response.Content.ReadFromJsonAsync<IngestJobStatusResponse>();
            Assert.NotNull(payload);

            if (payload.Status is "Succeeded" or "Failed")
            {
                return payload;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        throw new TimeoutException($"Ingestion job '{jobId}' did not reach a terminal state in time.");
    }
}

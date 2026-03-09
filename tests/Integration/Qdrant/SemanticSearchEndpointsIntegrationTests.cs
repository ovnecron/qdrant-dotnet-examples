using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Api.Contracts;

using Integration.Fixtures;

namespace Integration.Qdrant;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class SemanticSearchEndpointsIntegrationTests
{
    private readonly ApiIntegrationFixture _fixture;

    public SemanticSearchEndpointsIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Query_ReturnsRelevantHit_ForIngestedMarkdown()
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
            "/api/v1/search/query",
            new SemanticSearchQueryRequest
            {
                Collection = collectionName,
                QueryText = "How do I check health and ready endpoints locally?",
                TopK = 3,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-a",
                    TagsAny = ["local"]
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SemanticSearchQueryResponse>();

        Assert.NotNull(payload);
        Assert.Equal(collectionName, payload.Collection);
        Assert.Equal("How do I check health and ready endpoints locally?", payload.QueryText);
        Assert.NotEmpty(payload.Hits);
        Assert.Contains(payload.Hits, hit => hit.DocId == "guide-local-run");
    }

    [Fact]
    public async Task Query_WithInvalidRequest_ReturnsValidationErrors()
    {
        var client = _fixture.Client;
        var response = await client.PostAsJsonAsync(
            "/api/v1/search/query",
            new SemanticSearchQueryRequest
            {
                Collection = _fixture.CreateCollectionName(),
                QueryText = "   ",
                TopK = 0
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        Assert.True(document.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("queryText", out _));
        Assert.True(errors.TryGetProperty("topK", out _));
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

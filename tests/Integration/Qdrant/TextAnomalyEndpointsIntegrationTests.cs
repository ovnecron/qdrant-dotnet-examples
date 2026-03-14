using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Api.Contracts;

using Integration.Fixtures;

namespace Integration.Qdrant;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TextAnomalyEndpointsIntegrationTests
{
    private readonly ApiIntegrationFixture _fixture;

    public TextAnomalyEndpointsIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Score_ReturnsLowAnomalyScore_ForOperationallySimilarText()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);
        await SeedBaselineAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/anomaly/text/score",
            new TextAnomalyScoreRequest
            {
                Collection = collectionName,
                Text = "Check the /health and /ready endpoints after startup.",
                TopK = 1,
                Threshold = 0.35f,
                IncludeDebug = true,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TextAnomalyScoreResponse>();

        Assert.NotNull(payload);
        Assert.False(payload.IsAnomalous);
        Assert.InRange(payload.AnomalyScore, 0f, 0.35f);
        Assert.Equal(1, payload.NeighborCount);
        Assert.NotEmpty(payload.Neighbors);
        var debug = Assert.IsType<TextAnomalyDebugResponse>(payload.Debug);
        Assert.Equal("Deterministic", debug.EmbeddingProvider);
        Assert.Equal("hashing-text-v1", debug.EmbeddingModel);
        Assert.Equal("v1", debug.EmbeddingSchemaVersion);
    }

    [Fact]
    public async Task Score_ReturnsHighAnomalyScore_ForClearlyUnrelatedText()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);
        await SeedBaselineAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/anomaly/text/score",
            new TextAnomalyScoreRequest
            {
                Collection = collectionName,
                Text = "Galaxies and nebulae drift through deep space far from health endpoint checks.",
                TopK = 1,
                Threshold = 0.35f,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TextAnomalyScoreResponse>();

        Assert.NotNull(payload);
        Assert.True(payload.IsAnomalous);
        Assert.InRange(payload.AnomalyScore, 0.35f, 1f);
        Assert.Equal(1, payload.NeighborCount);
    }

    [Fact]
    public async Task Score_ReturnsUnprocessableEntity_WhenNoBaselineNeighborsFound()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);
        await SeedBaselineAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/anomaly/text/score",
            new TextAnomalyScoreRequest
            {
                Collection = collectionName,
                Text = "The API should return 200 on /health and /ready after startup.",
                TopK = 3,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-missing"
                }
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        Assert.Equal("Insufficient anomaly baseline", document.RootElement.GetProperty("title").GetString());
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

    private static async Task SeedBaselineAsync(HttpClient client, string collectionName)
    {
        await IngestAsync(
            client,
            collectionName,
            "ops-health-checks",
            "docs/ops-health.md",
            "Operational Health Checks",
            "# Operational Health Checks\n\nCheck the /health and /ready endpoints after startup.",
            ["ops", "health"],
            tenantId: "tenant-a");

        await IngestAsync(
            client,
            collectionName,
            "ops-runtime-status",
            "docs/runtime-status.md",
            "Runtime Status",
            "# Runtime Status\n\nVerify that AppHost starts cleanly and the API responds on /health and /ready.",
            ["ops", "runtime"],
            tenantId: "tenant-a");

        await IngestAsync(
            client,
            collectionName,
            "ops-readiness",
            "docs/readiness.md",
            "Service Readiness",
            "# Service Readiness\n\nConfirm readiness checks and local startup status before sending traffic.",
            ["ops", "readiness"],
            tenantId: "tenant-a");
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

        throw new TimeoutException($"Ingestion job did not reach a terminal state in time.");
    }
}

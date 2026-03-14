using System.Net;
using System.Net.Http.Json;

using Api.Contracts;

using Embeddings.Clients;
using Embeddings.Contracts;
using Embeddings.Options;

using Integration.Fixtures;

using Microsoft.Extensions.Options;

namespace Integration.Qdrant;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class IngestEndpointsIntegrationTests
{
    private readonly ApiIntegrationFixture _fixture;

    public IngestEndpointsIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IngestMarkdown_ReturnsAccepted_AndJobEventuallySucceeds()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/ingest/markdown",
            new MarkdownIngestRequest
            {
                Collection = collectionName,
                DocId = "guide-1",
                SourceId = "docs/local-run.md",
                Title = "Local Run",
                Markdown = "# Local Run\n\nRun AppHost and wait until resources are healthy.",
                Tags = ["tutorial", "local"],
                TenantId = "tenant-a"
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var accepted = await response.Content.ReadFromJsonAsync<MarkdownIngestAcceptedResponse>();

        Assert.NotNull(accepted);
        Assert.Equal(collectionName, accepted.Collection);
        Assert.Equal("guide-1", accepted.DocId);
        Assert.Equal("hashing-text-v1", accepted.EmbeddingModel);
        Assert.Equal("v1", accepted.EmbeddingSchemaVersion);
        Assert.EndsWith($"/api/v1/ingest/jobs/{accepted.JobId}", response.Headers.Location!.OriginalString, StringComparison.Ordinal);

        var status = await WaitForTerminalJobStatusAsync(client, accepted.JobId);

        Assert.Equal("Succeeded", status.Status);
        Assert.Equal(accepted.DocVersion, status.DocVersion);
        Assert.NotNull(status.StartedAtUtc);
        Assert.NotNull(status.CompletedAtUtc);
        Assert.NotNull(status.Result);
        Assert.Equal(1, status.Result.ChunkCount);
        Assert.Equal(1, status.Result.UpsertedCount);
        Assert.Null(status.Error);

        var queryVector = await CreateQueryVectorAsync(
            BuildDocumentEmbeddingText(
                title: "Local Run",
                content: "Run AppHost and wait until resources are healthy."));

        var searchResponse = await client.PostAsJsonAsync(
            "/api/v1/vectors/search",
            new VectorSearchRequest
            {
                Collection = collectionName,
                QueryVector = queryVector,
                TopK = 5,
                MinScore = 0.6f,
                Filter = new VectorSearchFilterRequest
                {
                    DocIdEquals = "guide-1",
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var searchPayload = await searchResponse.Content.ReadFromJsonAsync<VectorSearchResponse>();
        Assert.NotNull(searchPayload);
        var hit = Assert.Single(searchPayload.Hits);

        var getResponse = await client.GetAsync($"/api/v1/vectors/{collectionName}/{hit.ChunkId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var record = await getResponse.Content.ReadFromJsonAsync<VectorRecordResponse>();

        Assert.NotNull(record);
        Assert.Equal("tenant-a", record.TenantId);
        Assert.Equal(accepted.DocVersion, record.DocVersion);
        Assert.Equal(accepted.EmbeddingSchemaVersion, record.EmbeddingSchemaVersion);
    }

    [Fact]
    public async Task IngestMarkdown_ReingestReplacesExistingDocChunks()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);

        var firstAccepted = await SubmitIngestAsync(
            client,
            collectionName,
            docId: "guide-1",
            sourceId: "docs/guide.md",
            title: "Aurora Brief",
            markdown: "# Aurora Brief\n\nAlpha orbit signal.",
            tenantId: "tenant-a");

        var firstStatus = await WaitForTerminalJobStatusAsync(client, firstAccepted.JobId);
        Assert.Equal("Succeeded", firstStatus.Status);

        var secondAccepted = await SubmitIngestAsync(
            client,
            collectionName,
            docId: "guide-1",
            sourceId: "docs/guide.md",
            title: "Zenith Ledger",
            markdown: "# Zenith Ledger\n\nGamma lattice beacon.",
            tenantId: "tenant-a");

        var secondStatus = await WaitForTerminalJobStatusAsync(client, secondAccepted.JobId);
        Assert.Equal("Succeeded", secondStatus.Status);
        Assert.NotEqual(firstAccepted.DocVersion, secondAccepted.DocVersion);

        var oldVector = await CreateQueryVectorAsync(
            BuildDocumentEmbeddingText(
                title: "Aurora Brief",
                content: "Alpha orbit signal."));

        var oldSearchResponse = await client.PostAsJsonAsync(
            "/api/v1/vectors/search",
            new VectorSearchRequest
            {
                Collection = collectionName,
                QueryVector = oldVector,
                TopK = 5,
                MinScore = 0.6f,
                Filter = new VectorSearchFilterRequest
                {
                    DocIdEquals = "guide-1",
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.OK, oldSearchResponse.StatusCode);

        var oldSearchPayload = await oldSearchResponse.Content.ReadFromJsonAsync<VectorSearchResponse>();
        Assert.NotNull(oldSearchPayload);
        Assert.Empty(oldSearchPayload.Hits);

        var newVector = await CreateQueryVectorAsync(
            BuildDocumentEmbeddingText(
                title: "Zenith Ledger",
                content: "Gamma lattice beacon."));

        var newSearchResponse = await client.PostAsJsonAsync(
            "/api/v1/vectors/search",
            new VectorSearchRequest
            {
                Collection = collectionName,
                QueryVector = newVector,
                TopK = 5,
                MinScore = 0.6f,
                Filter = new VectorSearchFilterRequest
                {
                    DocIdEquals = "guide-1",
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.OK, newSearchResponse.StatusCode);

        var newSearchPayload = await newSearchResponse.Content.ReadFromJsonAsync<VectorSearchResponse>();
        Assert.NotNull(newSearchPayload);
        var hit = Assert.Single(newSearchPayload.Hits);

        var recordResponse = await client.GetAsync($"/api/v1/vectors/{collectionName}/{hit.ChunkId}");
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);
        var record = await recordResponse.Content.ReadFromJsonAsync<VectorRecordResponse>();

        Assert.NotNull(record);
        Assert.Equal(secondAccepted.DocVersion, record.DocVersion);
        Assert.NotEqual(firstAccepted.DocVersion, record.DocVersion);
    }

    [Fact]
    public async Task IngestMarkdown_JobEventuallyFails_WhenCollectionDoesNotExist()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        var accepted = await SubmitIngestAsync(
            client,
            collectionName,
            docId: "missing-collection-doc",
            sourceId: "docs/failure.md",
            title: "Missing Collection",
            markdown: "# Missing Collection\n\nThis collection does not exist.",
            tenantId: null);

        var status = await WaitForTerminalJobStatusAsync(client, accepted.JobId);

        Assert.Equal("Failed", status.Status);
        Assert.NotNull(status.StartedAtUtc);
        Assert.NotNull(status.CompletedAtUtc);
        Assert.Null(status.Result);
        Assert.NotNull(status.Error);
        Assert.Equal("ingestion_failed", status.Error.Code);
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

    private static async Task<MarkdownIngestAcceptedResponse> SubmitIngestAsync(
        HttpClient client,
        string collectionName,
        string docId,
        string sourceId,
        string title,
        string markdown,
        string? tenantId)
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
                TenantId = tenantId
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MarkdownIngestAcceptedResponse>();
        Assert.NotNull(payload);
        return payload;
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

    private static async Task<IReadOnlyList<float>> CreateQueryVectorAsync(string text)
    {
        var client = new DeterministicTextEmbeddingClient(
            Options.Create(
                new EmbeddingOptions
                {
                    Provider = EmbeddingProvider.Deterministic,
                    Model = "hashing-text-v1",
                    Dimension = 384,
                    BatchSize = 16,
                    SchemaVersion = "v1"
                }));

        var result = await client.EmbedAsync(
            new TextEmbeddingRequest
            {
                Text = text,
                Kind = EmbeddingKind.Query
            });

        return result.Vector;
    }

    private static string BuildDocumentEmbeddingText(string title, string content, string? section = null)
    {
        var lines = new List<string>
        {
            $"Title: {title}"
        };

        if (!string.IsNullOrWhiteSpace(section))
        {
            lines.Add($"Section: {section}");
        }

        lines.Add("Content:");
        lines.Add(content);

        return string.Join('\n', lines);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Api.Contracts;

using Embeddings.Clients;
using Embeddings.Contracts;
using Embeddings.Options;

using Integration.Fixtures;

using Microsoft.Extensions.Options;

namespace Integration.Qdrant;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class EvalLiteIntegrationTests
{
    private const string EvalTenantId = "eval-lite";

    private readonly ApiIntegrationFixture _fixture;

    public EvalLiteIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RecallAt3_RemainsAtOrAboveConfiguredThreshold()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeEmbeddingCollectionAsync(client, collectionName);

        foreach (var document in LoadDocuments())
        {
            var accepted = await SubmitIngestAsync(client, collectionName, document);
            var status = await WaitForTerminalJobStatusAsync(client, accepted.JobId);

            Assert.Equal("Succeeded", status.Status);
        }

        var queries = LoadQueries();
        var evaluatedQueries = new List<EvalQueryResult>(queries.Count);

        foreach (var query in queries)
        {
            var queryVector = await CreateQueryVectorAsync(query.QueryText);
            var searchResponse = await client.PostAsJsonAsync(
                "/api/v1/vectors/search",
                new VectorSearchRequest
                {
                    Collection = collectionName,
                    QueryVector = queryVector,
                    TopK = 3,
                    Filter = new VectorSearchFilterRequest
                    {
                        TenantIdEquals = EvalTenantId
                    }
                });

            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

            var searchPayload = await searchResponse.Content.ReadFromJsonAsync<VectorSearchResponse>();
            Assert.NotNull(searchPayload);

            var actualDocIds = await ResolveDocIdsAsync(client, collectionName, searchPayload.Hits);
            var matchedExpectedDocIds = actualDocIds
                .Intersect(query.ExpectedDocIds, StringComparer.Ordinal)
                .ToArray();

            evaluatedQueries.Add(
                new EvalQueryResult(
                    query.Id,
                    query.QueryText,
                    query.ExpectedDocIds,
                    actualDocIds,
                    matchedExpectedDocIds.Length > 0));
        }

        var successfulQueries = evaluatedQueries.Count(static result => result.IsMatch);
        var recallAt3 = successfulQueries / (double)evaluatedQueries.Count;

        Assert.True(
            recallAt3 >= 0.80d,
            BuildFailureMessage(recallAt3, evaluatedQueries));
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

    private static IReadOnlyList<EvalDocumentFixture> LoadDocuments()
    {
        var datasetDirectory = GetDatasetDirectory();
        var markdownFiles = Directory
            .EnumerateFiles(datasetDirectory, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();

        return markdownFiles
            .Select(
                filePath =>
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileStem = Path.GetFileNameWithoutExtension(filePath);

                    return new EvalDocumentFixture(
                        DocId: $"eval-{fileStem}",
                        SourceId: $"tests/Integration/Fixtures/EvalDataset/{fileName}",
                        Markdown: File.ReadAllText(filePath));
                })
            .ToArray();
    }

    private static IReadOnlyList<EvalQueryFixture> LoadQueries()
    {
        var datasetDirectory = GetDatasetDirectory();
        var queryFilePath = Path.Combine(datasetDirectory, "eval-queries.json");
        var queryJson = File.ReadAllText(queryFilePath);

        var queries = JsonSerializer.Deserialize<List<EvalQueryFixture>>(
            queryJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return queries ?? throw new InvalidOperationException("Eval query fixtures could not be loaded.");
    }

    private static string GetDatasetDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", "EvalDataset");
    }

    private static async Task<MarkdownIngestAcceptedResponse> SubmitIngestAsync(
        HttpClient client,
        string collectionName,
        EvalDocumentFixture document)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/ingest/markdown",
            new MarkdownIngestRequest
            {
                Collection = collectionName,
                DocId = document.DocId,
                SourceId = document.SourceId,
                Markdown = document.Markdown,
                TenantId = EvalTenantId
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

    private static async Task<IReadOnlyList<string>> ResolveDocIdsAsync(
        HttpClient client,
        string collectionName,
        IReadOnlyList<VectorSearchHitResponse> hits)
    {
        var docIds = new List<string>(hits.Count);

        foreach (var hit in hits)
        {
            var response = await client.GetAsync($"/api/v1/vectors/{collectionName}/{hit.ChunkId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var payload = await response.Content.ReadFromJsonAsync<VectorRecordResponse>();
            Assert.NotNull(payload);

            docIds.Add(payload.DocId);
        }

        return docIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<IReadOnlyList<float>> CreateQueryVectorAsync(string text)
    {
        var client = new DeterministicTextEmbeddingClient(
            Options.Create(
                new EmbeddingOptions
                {
                    Provider = DeterministicTextEmbeddingClient.ProviderName,
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

    private static string BuildFailureMessage(double recallAt3, IReadOnlyList<EvalQueryResult> results)
    {
        var lines = new List<string>
        {
            $"Expected Recall@3 >= 0.80, but got {recallAt3:F2}."
        };

        lines.AddRange(
            results.Select(
                result =>
                    $"{result.QueryId}: expected [{string.Join(", ", result.ExpectedDocIds)}], actual [{string.Join(", ", result.ActualDocIds)}]"));

        return string.Join(Environment.NewLine, lines);
    }

    private sealed record EvalDocumentFixture(
        string DocId,
        string SourceId,
        string Markdown);

    private sealed record EvalQueryFixture
    {
        public required string Id { get; init; }

        public required string QueryText { get; init; }

        public required IReadOnlyList<string> ExpectedDocIds { get; init; }
    }

    private sealed record EvalQueryResult(
        string QueryId,
        string QueryText,
        IReadOnlyList<string> ExpectedDocIds,
        IReadOnlyList<string> ActualDocIds,
        bool IsMatch);
}

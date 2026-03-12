using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Api.Contracts;

using Integration.Fixtures;

namespace Integration.Qdrant;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AnomalyEndpointsIntegrationTests
{
    private readonly ApiIntegrationFixture _fixture;

    public AnomalyEndpointsIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Score_ReturnsLowAnomalyScore_ForPointNearBaselineCluster()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeCollectionAsync(client, collectionName);
        await SeedBaselineAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/anomaly/score",
            new AnomalyScoreRequest
            {
                Collection = collectionName,
                Vector = [0.98f, 0.02f, 0f],
                TopK = 3,
                Threshold = 0.35f,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AnomalyScoreResponse>();

        Assert.NotNull(payload);
        Assert.False(payload.IsAnomalous);
        Assert.InRange(payload.AnomalyScore, 0f, 0.15f);
        Assert.Equal(3, payload.NeighborCount);
        Assert.NotEmpty(payload.Neighbors);
    }

    [Fact]
    public async Task Score_ReturnsHighAnomalyScore_ForOutlierPoint()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeCollectionAsync(client, collectionName);
        await SeedBaselineAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/anomaly/score",
            new AnomalyScoreRequest
            {
                Collection = collectionName,
                Vector = [-1f, 0f, 0f],
                TopK = 3,
                Threshold = 0.35f,
                Filter = new VectorSearchFilterRequest
                {
                    TenantIdEquals = "tenant-a"
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AnomalyScoreResponse>();

        Assert.NotNull(payload);
        Assert.True(payload.IsAnomalous);
        Assert.InRange(payload.AnomalyScore, 0.9f, 1f);
        Assert.Equal(3, payload.NeighborCount);
    }

    [Fact]
    public async Task Score_ReturnsUnprocessableEntity_WhenNoBaselineNeighborsFound()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeCollectionAsync(client, collectionName);
        await SeedBaselineAsync(client, collectionName);

        var response = await client.PostAsJsonAsync(
            "/api/v1/anomaly/score",
            new AnomalyScoreRequest
            {
                Collection = collectionName,
                Vector = [0.98f, 0.02f, 0f],
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

    private static async Task InitializeCollectionAsync(HttpClient client, string collectionName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/collections/init",
            new InitializeCollectionRequest
            {
                CollectionName = collectionName,
                VectorSize = 3,
                Distance = "Cosine"
            });

        Assert.True(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
            $"Expected collection init to return 201 or 200, got {(int)response.StatusCode}.");
    }

    private static async Task SeedBaselineAsync(HttpClient client, string collectionName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/vectors/upsert",
            new VectorUpsertRequest
            {
                Collection = collectionName,
                Points =
                [
                    CreatePoint("baseline-1", [1f, 0f, 0f], "acct-42"),
                    CreatePoint("baseline-2", [0.99f, 0.01f, 0f], "acct-42"),
                    CreatePoint("baseline-3", [0.97f, 0.03f, 0f], "acct-42")
                ]
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static VectorPointRequest CreatePoint(string id, IReadOnlyList<float> vector, string docId)
    {
        return new VectorPointRequest
        {
            Id = id,
            Vector = vector,
            Payload = new VectorPayloadRequest
            {
                DocId = docId,
                Source = "fixtures/anomaly-baseline.json",
                Tags = ["baseline", "normal"],
                Content = "Baseline anomaly reference point.",
                TenantId = "tenant-a"
            }
        };
    }
}

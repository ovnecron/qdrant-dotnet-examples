using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Api.Contracts;

using Integration.Fixtures;

namespace Integration.Qdrant;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class VectorEndpointsIntegrationTests
{
    private readonly ApiIntegrationFixture _fixture;

    public VectorEndpointsIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HealthAndReadyEndpoints_ReturnOk()
    {
        var client = _fixture.Client;
        var healthResponse = await client.GetAsync("/health");
        var readyResponse = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
    }

    [Fact]
    public async Task InitializeUpsertAndSearch_ReturnExpectedFilteredHit()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        var initializeResponse = await client
            .PostAsJsonAsync(
                "/api/v1/admin/collections/init",
                new InitializeCollectionRequest
                {
                    CollectionName = collectionName,
                    VectorSize = 3,
                    Distance = "Cosine"
                });

        Assert.Equal(HttpStatusCode.Created, initializeResponse.StatusCode);

        var initializePayload = await initializeResponse.Content
            .ReadFromJsonAsync<InitializeCollectionResponse>();

        Assert.NotNull(initializePayload);
        Assert.Equal(collectionName, initializePayload.CollectionName);
        Assert.True(initializePayload.Created);

        var upsertResponse = await client
            .PostAsJsonAsync(
                "/api/v1/vectors/upsert",
                new VectorUpsertRequest
                {
                    Collection = collectionName,
                    Points =
                    [
                        new VectorPointRequest
                        {
                            Id = "doc-1:0",
                            Vector = [0.12f, -0.09f, 0.31f],
                            Payload = new VectorPayloadRequest
                            {
                                DocId = "doc-1",
                                Source = "docs/intro.md",
                                Tags = ["getting-started"],
                                Content = "Run AppHost and wait."
                            }
                        },
                        new VectorPointRequest
                        {
                            Id = "doc-1:1",
                            Vector = [0.10f, -0.06f, 0.28f],
                            Payload = new VectorPayloadRequest
                            {
                                DocId = "doc-1",
                                Source = "docs/intro.md",
                                Tags = ["setup"],
                                Content = "Qdrant endpoint is configured."
                            }
                        }
                    ]
                });

        Assert.Equal(HttpStatusCode.Created, upsertResponse.StatusCode);

        var upsertPayload = await upsertResponse.Content
            .ReadFromJsonAsync<VectorUpsertResponse>();

        Assert.NotNull(upsertPayload);
        Assert.Equal(collectionName, upsertPayload.Collection);
        Assert.Equal(2, upsertPayload.UpsertedCount);

        var searchResponse = await client
            .PostAsJsonAsync(
                "/api/v1/vectors/search",
                new VectorSearchRequest
                {
                    Collection = collectionName,
                    QueryVector = [0.11f, -0.05f, 0.27f],
                    TopK = 5,
                    MinScore = 0,
                    Filter = new VectorSearchFilterRequest
                    {
                        TagsAny = ["getting-started"],
                        SourceEquals = "docs/intro.md"
                    }
                });

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var searchPayload = await searchResponse.Content
            .ReadFromJsonAsync<VectorSearchResponse>();

        Assert.NotNull(searchPayload);

        var hit = Assert.Single(searchPayload.Hits);
        Assert.Equal("doc-1:0", hit.ChunkId);
        Assert.Equal("docs/intro.md", hit.Source);
        Assert.Contains("getting-started", hit.Tags);
    }

    [Fact]
    public async Task InitializeCollection_IsIdempotent()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        var firstResponse = await client
            .PostAsJsonAsync(
                "/api/v1/admin/collections/init",
                new InitializeCollectionRequest
                {
                    CollectionName = collectionName,
                    VectorSize = 3,
                    Distance = "Cosine"
                });

        var secondResponse = await client
            .PostAsJsonAsync(
                "/api/v1/admin/collections/init",
                new InitializeCollectionRequest
                {
                    CollectionName = collectionName,
                    VectorSize = 3,
                    Distance = "Cosine"
                });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstPayload = await firstResponse.Content
            .ReadFromJsonAsync<InitializeCollectionResponse>();

        var secondPayload = await secondResponse.Content
            .ReadFromJsonAsync<InitializeCollectionResponse>();

        Assert.NotNull(firstPayload);
        Assert.NotNull(secondPayload);
        Assert.True(firstPayload.Created);
        Assert.False(secondPayload.Created);
    }

    [Fact]
    public async Task Search_WithInvalidRequest_ReturnsValidationErrors()
    {
        var client = _fixture.Client;
        var response = await client
            .PostAsJsonAsync(
                "/api/v1/vectors/search",
                new VectorSearchRequest
                {
                    Collection = _fixture.CreateCollectionName(),
                    QueryVector = [],
                    TopK = 0
                });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        Assert.True(document.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("queryVector", out _));
        Assert.True(errors.TryGetProperty("topK", out _));
    }
}

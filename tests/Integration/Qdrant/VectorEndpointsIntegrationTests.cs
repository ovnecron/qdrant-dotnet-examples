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

    [Fact]
    public async Task GetById_ReturnsStoredVectorRecord()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeCollectionAsync(client, collectionName);
        await UpsertIntroDocumentAsync(client, collectionName);

        var response = await client.GetAsync($"/api/v1/vectors/{collectionName}/doc-1:0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<VectorRecordResponse>();

        Assert.NotNull(payload);
        Assert.Equal(collectionName, payload.Collection);
        Assert.Equal("doc-1:0", payload.ChunkId);
        Assert.Equal("doc-1", payload.DocId);
        Assert.Equal("docs/intro.md", payload.Source);
        Assert.Contains("getting-started", payload.Tags);
        Assert.Equal("Run AppHost and wait.", payload.Content);
        Assert.Equal(Normalize([0.12f, -0.09f, 0.31f]), payload.Vector, new FloatArrayComparer(1e-5f));
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenChunkDoesNotExist()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeCollectionAsync(client, collectionName);

        var response = await client.GetAsync($"/api/v1/vectors/{collectionName}/missing-chunk");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesStoredVector_AndSearchNoLongerReturnsIt()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeCollectionAsync(client, collectionName);
        await UpsertIntroDocumentAsync(client, collectionName);

        var deleteResponse = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/v1/vectors")
            {
                Content = JsonContent.Create(
                    new VectorDeleteRequest
                    {
                        Collection = collectionName,
                        ChunkIds = [" doc-1:0 ", "doc-1:0"]
                    })
            });

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var deletePayload = await deleteResponse.Content.ReadFromJsonAsync<VectorDeleteResponse>();

        Assert.NotNull(deletePayload);
        Assert.Equal(collectionName, deletePayload.Collection);
        Assert.Equal(1, deletePayload.DeletedCount);

        var getResponse = await client.GetAsync($"/api/v1/vectors/{collectionName}/doc-1:0");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var searchResponse = await client.PostAsJsonAsync(
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

        var searchPayload = await searchResponse.Content.ReadFromJsonAsync<VectorSearchResponse>();

        Assert.NotNull(searchPayload);
        Assert.Empty(searchPayload.Hits);
    }

    [Fact]
    public async Task Delete_IsIdempotent_WhenChunkIdsDoNotExist()
    {
        var client = _fixture.Client;
        var collectionName = _fixture.CreateCollectionName();

        await InitializeCollectionAsync(client, collectionName);

        var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/v1/vectors")
            {
                Content = JsonContent.Create(
                    new VectorDeleteRequest
                    {
                        Collection = collectionName,
                        ChunkIds = ["missing-1", "missing-1", "missing-2"]
                    })
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<VectorDeleteResponse>();

        Assert.NotNull(payload);
        Assert.Equal(collectionName, payload.Collection);
        Assert.Equal(0, payload.DeletedCount);
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

    private static async Task UpsertIntroDocumentAsync(HttpClient client, string collectionName)
    {
        var response = await client.PostAsJsonAsync(
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
                    }
                ]
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static float[] Normalize(IReadOnlyList<float> vector)
    {
        var magnitude = MathF.Sqrt(vector.Sum(component => component * component));
        return vector.Select(component => component / magnitude).ToArray();
    }

    private sealed class FloatArrayComparer : IEqualityComparer<float>
    {
        private readonly float _tolerance;

        public FloatArrayComparer(float tolerance)
        {
            _tolerance = tolerance;
        }

        public bool Equals(float x, float y)
        {
            return MathF.Abs(x - y) <= _tolerance;
        }

        public int GetHashCode(float value)
        {
            return value.GetHashCode();
        }
    }
}

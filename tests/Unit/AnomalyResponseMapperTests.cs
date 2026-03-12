using Api.Services;
using Api.Services.Mappers;

using VectorStore.Abstractions.Contracts;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class AnomalyResponseMapperTests
{
    [Fact]
    public void ToScoreResponse_MapsComputationAndNeighbors()
    {
        var mapper = new AnomalyResponseMapper();

        var response = mapper.ToScoreResponse(
            traceId: "trace-123",
            collectionName: "anomaly_vectors",
            includeNeighbors: true,
            neighbors:
            [
                new SearchResult
                {
                    ChunkId = "baseline-1",
                    DocId = "acct-42",
                    Score = 0.91f,
                    Source = "fixtures/anomaly-baseline.json",
                    Tags = ["baseline", "normal"],
                    Content = "baseline"
                }
            ],
            threshold: 0.35f,
            computation: new AnomalyScoreComputation(
                AnomalyScore: 0.09f,
                MeanNeighborSimilarity: 0.91f,
                MaxNeighborSimilarity: 0.91f,
                IsAnomalous: false));

        Assert.Equal("trace-123", response.TraceId);
        Assert.Equal("anomaly_vectors", response.Collection);
        Assert.Equal(0.09f, response.AnomalyScore);
        Assert.False(response.IsAnomalous);
        Assert.Equal(1, response.NeighborCount);

        var neighbor = Assert.Single(response.Neighbors);
        Assert.Equal("baseline-1", neighbor.Id);
        Assert.Equal("acct-42", neighbor.DocId);
        Assert.Contains("baseline", neighbor.Tags);
    }

    [Fact]
    public void ToScoreResponse_OmitsNeighbors_WhenDisabled()
    {
        var mapper = new AnomalyResponseMapper();

        var response = mapper.ToScoreResponse(
            traceId: "trace-123",
            collectionName: "anomaly_vectors",
            includeNeighbors: false,
            neighbors:
            [
                new SearchResult
                {
                    ChunkId = "baseline-1",
                    DocId = "acct-42",
                    Score = 0.91f,
                    Source = "fixtures/anomaly-baseline.json",
                    Content = "baseline"
                }
            ],
            threshold: 0.35f,
            computation: new AnomalyScoreComputation(
                AnomalyScore: 0.09f,
                MeanNeighborSimilarity: 0.91f,
                MaxNeighborSimilarity: 0.91f,
                IsAnomalous: false));

        Assert.Empty(response.Neighbors);
        Assert.Equal(1, response.NeighborCount);
    }
}

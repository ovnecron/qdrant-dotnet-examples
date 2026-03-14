using Api.Services.Results;

using Grpc.Core;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class ServiceResultExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult_ForSuccessfulOperation()
    {
        var executor = new ServiceResultExecutor();

        var result = await executor.ExecuteAsync(
            () => Task.FromResult(new DummyResponse { Value = "ok" }),
            unexpectedTitle: "unexpected");

        var response = Assert.IsType<DummyResponse>(result.Value);
        Assert.Equal("ok", response.Value);
        Assert.Null(result.Failure);
        Assert.Null(result.ValidationErrors);
        Assert.False(result.IsCreated);
    }

    [Fact]
    public async Task ExecuteAsync_MapsUnavailableRpcException_ToVectorStoreUnavailable()
    {
        var executor = new ServiceResultExecutor();

        var result = await executor.ExecuteAsync(
            () => Task.FromException<DummyResponse>(
                new RpcException(new Status(StatusCode.Unavailable, "qdrant down"))),
            unexpectedTitle: "unexpected");

        Assert.Null(result.Value);
        Assert.NotNull(result.Failure);
        Assert.Equal(ServiceFailureKind.VectorStoreUnavailable, result.Failure.Kind);
        Assert.Equal("Vector store unavailable", result.Failure.Title);
    }

    [Fact]
    public async Task ExecuteAsync_MapsInvalidOperationException_ToConfigurationInvalid()
    {
        var executor = new ServiceResultExecutor();

        var result = await executor.ExecuteAsync(
            () => Task.FromException<DummyResponse>(
                new InvalidOperationException("invalid config")),
            unexpectedTitle: "unexpected");

        Assert.Null(result.Value);
        Assert.NotNull(result.Failure);
        Assert.Equal(ServiceFailureKind.ConfigurationInvalid, result.Failure.Kind);
        Assert.Equal("Vector store configuration invalid", result.Failure.Title);
    }

    [Fact]
    public async Task ExecuteOptionalAsync_ReturnsNotFoundFailure_WhenOperationReturnsNull()
    {
        var executor = new ServiceResultExecutor();
        var missingFailure = new ServiceFailure(
            ServiceFailureKind.NotFound,
            "Vector not found",
            "missing");

        var result = await executor.ExecuteOptionalAsync<DummyResponse>(
            () => Task.FromResult<DummyResponse?>(null),
            unexpectedTitle: "unexpected",
            missingFailure: missingFailure);

        Assert.Null(result.Value);
        Assert.NotNull(result.Failure);
        Assert.Equal(ServiceFailureKind.NotFound, result.Failure.Kind);
        Assert.Equal("Vector not found", result.Failure.Title);
    }

    [Fact]
    public async Task ExecuteAsync_MapsServiceFailureException_ToProvidedFailure()
    {
        var executor = new ServiceResultExecutor();

        var result = await executor.ExecuteAsync(
            () => Task.FromException<DummyResponse>(
                new ServiceFailureException(
                    new ServiceFailure(
                        ServiceFailureKind.AnswerProviderUnavailable,
                        "Answer provider unavailable",
                        "ollama down"))),
            unexpectedTitle: "unexpected");

        Assert.Null(result.Value);
        Assert.NotNull(result.Failure);
        Assert.Equal(ServiceFailureKind.AnswerProviderUnavailable, result.Failure.Kind);
        Assert.Equal("Answer provider unavailable", result.Failure.Title);
    }

    private sealed class DummyResponse
    {
        public required string Value { get; init; }
    }
}

using Grpc.Core;

namespace Api.Services.Results;

internal interface IServiceResultExecutor
{
    Task<ServiceResult<TValue>> ExecuteAsync<TValue>(
        Func<Task<TValue>> operation,
        string unexpectedTitle)
        where TValue : class;

    Task<ServiceResult<TValue>> ExecuteAsync<TValue>(
        Func<Task<(TValue Value, bool IsCreated)>> operation,
        string unexpectedTitle)
        where TValue : class;
}

internal sealed class ServiceResultExecutor : IServiceResultExecutor
{
    public Task<ServiceResult<TValue>> ExecuteAsync<TValue>(
        Func<Task<TValue>> operation,
        string unexpectedTitle)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteAsync(
            async () =>
            {
                var value = await operation().ConfigureAwait(false);
                return (Value: value, IsCreated: false);
            },
            unexpectedTitle);
    }

    public async Task<ServiceResult<TValue>> ExecuteAsync<TValue>(
        Func<Task<(TValue Value, bool IsCreated)>> operation,
        string unexpectedTitle)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            var outcome = await operation().ConfigureAwait(false);
            return ServiceResult<TValue>.Success(outcome.Value, outcome.IsCreated);
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return ServiceResult<TValue>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.VectorStoreUnavailable,
                    "Vector store unavailable",
                    exception.Status.Detail));
        }
        catch (InvalidOperationException exception)
        {
            return ServiceResult<TValue>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.ConfigurationInvalid,
                    "Vector store configuration invalid",
                    exception.Message));
        }
        catch (Exception exception)
        {
            return ServiceResult<TValue>.Failed(
                new ServiceFailure(
                    ServiceFailureKind.Unexpected,
                    unexpectedTitle,
                    exception.Message));
        }
    }
}

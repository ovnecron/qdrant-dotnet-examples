using Grpc.Core;

namespace Api.Services.Results;

internal interface IServiceResultExecutor
{
    Task<ServiceResult<TValue>> ExecuteAsync<TValue>(
        Func<Task<TValue>> operation,
        string unexpectedTitle)
        where TValue : class;

    Task<ServiceResult<TValue>> ExecuteOptionalAsync<TValue>(
        Func<Task<TValue?>> operation,
        string unexpectedTitle,
        ServiceFailure missingFailure)
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
                var value = await operation();
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
            var outcome = await operation();
            return ServiceResult<TValue>.Success(outcome.Value, outcome.IsCreated);
        }
        catch (Exception exception)
        {
            return MapException<TValue>(exception, unexpectedTitle);
        }
    }

    public async Task<ServiceResult<TValue>> ExecuteOptionalAsync<TValue>(
        Func<Task<TValue?>> operation,
        string unexpectedTitle,
        ServiceFailure missingFailure)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(missingFailure);

        try
        {
            var value = await operation();
            return value is null
                ? ServiceResult<TValue>.Failed(missingFailure)
                : ServiceResult<TValue>.Success(value);
        }
        catch (Exception exception)
        {
            return MapException<TValue>(exception, unexpectedTitle);
        }
    }

    private static ServiceResult<TValue> MapException<TValue>(Exception exception, string unexpectedTitle)
        where TValue : class
    {
        return exception switch
        {
            RpcException rpcException when rpcException.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded =>
                ServiceResult<TValue>.Failed(
                    new ServiceFailure(
                        ServiceFailureKind.VectorStoreUnavailable,
                        "Vector store unavailable",
                        rpcException.Status.Detail)),
            InvalidOperationException invalidOperationException =>
                ServiceResult<TValue>.Failed(
                    new ServiceFailure(
                        ServiceFailureKind.ConfigurationInvalid,
                        "Vector store configuration invalid",
                        invalidOperationException.Message)),
            _ =>
                ServiceResult<TValue>.Failed(
                    new ServiceFailure(
                        ServiceFailureKind.Unexpected,
                        unexpectedTitle,
                        exception.Message))
        };
    }
}

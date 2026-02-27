namespace Api.Services.Results;

public sealed class ServiceResult<TValue>
    where TValue : class
{
    private ServiceResult(
        TValue? value,
        bool isCreated,
        IReadOnlyDictionary<string, string[]>? validationErrors,
        ServiceFailure? failure)
    {
        Value = value;
        IsCreated = isCreated;
        ValidationErrors = validationErrors;
        Failure = failure;
    }

    public TValue? Value { get; }

    public bool IsCreated { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public ServiceFailure? Failure { get; }

    public static ServiceResult<TValue> Success(TValue value, bool isCreated = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ServiceResult<TValue>(value, isCreated, validationErrors: null, failure: null);
    }

    public static ServiceResult<TValue> Validation(IReadOnlyDictionary<string, string[]> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);
        return new ServiceResult<TValue>(
            value: null,
            isCreated: false,
            validationErrors: validationErrors,
            failure: null);
    }

    public static ServiceResult<TValue> Failed(ServiceFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new ServiceResult<TValue>(
            value: null,
            isCreated: false,
            validationErrors: null,
            failure: failure);
    }
}

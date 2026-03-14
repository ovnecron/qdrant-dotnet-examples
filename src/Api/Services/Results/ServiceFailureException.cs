namespace Api.Services.Results;

internal sealed class ServiceFailureException : Exception
{
    public ServiceFailureException(ServiceFailure failure)
        : base(failure?.Detail)
    {
        Failure = failure ?? throw new ArgumentNullException(nameof(failure));
    }

    public ServiceFailure Failure { get; }
}

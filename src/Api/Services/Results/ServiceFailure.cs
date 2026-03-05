namespace Api.Services.Results;

public sealed record ServiceFailure(
    ServiceFailureKind Kind,
    string Title,
    string Detail);

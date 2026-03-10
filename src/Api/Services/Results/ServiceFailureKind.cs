namespace Api.Services.Results;

public enum ServiceFailureKind
{
    InsufficientGrounding,
    NotFound,
    VectorStoreUnavailable,
    ConfigurationInvalid,
    Unexpected
}

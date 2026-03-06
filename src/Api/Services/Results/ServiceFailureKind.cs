namespace Api.Services.Results;

public enum ServiceFailureKind
{
    NotFound,
    VectorStoreUnavailable,
    ConfigurationInvalid,
    Unexpected
}

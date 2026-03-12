namespace Api.Services.Results;

public enum ServiceFailureKind
{
    InsufficientAnomalyBaseline,
    InsufficientGrounding,
    NotFound,
    VectorStoreUnavailable,
    ConfigurationInvalid,
    Unexpected
}

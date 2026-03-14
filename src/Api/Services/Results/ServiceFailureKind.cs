namespace Api.Services.Results;

public enum ServiceFailureKind
{
    AnswerProviderUnavailable,
    InsufficientAnomalyBaseline,
    InsufficientGrounding,
    NotFound,
    VectorStoreUnavailable,
    ConfigurationInvalid,
    Unexpected
}

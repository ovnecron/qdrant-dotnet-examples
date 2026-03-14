using Api.Options;

using Embeddings.Options;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class EmbeddingOptionsValidatorTests
{
    [Fact]
    public void Validate_AllowsDeterministicProvider()
    {
        var validator = new EmbeddingOptionsValidator();
        var result = validator.Validate(
            name: null,
            new EmbeddingOptions
            {
                Provider = EmbeddingProvider.Deterministic,
                Model = "hashing-text-v1",
                Dimension = 384,
                BatchSize = 16,
                SchemaVersion = "v1"
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_AllowsOllamaProvider_WithAbsoluteBaseUrl()
    {
        var validator = new EmbeddingOptionsValidator();
        var result = validator.Validate(
            name: null,
            new EmbeddingOptions
            {
                Provider = EmbeddingProvider.Ollama,
                Model = "embeddinggemma",
                Dimension = 768,
                BatchSize = 8,
                SchemaVersion = "v1",
                BaseUrl = "http://localhost:11434/api"
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsForOllama_WhenBaseUrlIsInvalid()
    {
        var validator = new EmbeddingOptionsValidator();
        var result = validator.Validate(
            name: null,
            new EmbeddingOptions
            {
                Provider = EmbeddingProvider.Ollama,
                Model = "embeddinggemma",
                Dimension = 768,
                BatchSize = 8,
                SchemaVersion = "v1",
                BaseUrl = "localhost:11434"
            });

        Assert.False(result.Succeeded);
        var failures = Assert.IsAssignableFrom<IEnumerable<string>>(result.Failures);
        Assert.Contains(failures, failure => failure.Contains("Embedding:BaseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsForUnsupportedProviderValue()
    {
        var validator = new EmbeddingOptionsValidator();
        var result = validator.Validate(
            name: null,
            new EmbeddingOptions
            {
                Provider = (EmbeddingProvider)999,
                Model = "model",
                Dimension = 10,
                BatchSize = 1,
                SchemaVersion = "v1"
            });

        Assert.False(result.Succeeded);
        var failures = Assert.IsAssignableFrom<IEnumerable<string>>(result.Failures);
        Assert.Contains(failures, failure => failure.Contains("Embedding:Provider", StringComparison.Ordinal));
    }
}

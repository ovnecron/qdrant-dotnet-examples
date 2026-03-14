using Api.Options;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class RagOptionsValidatorTests
{
    [Fact]
    public void Validate_AllowsDeterministicProvider()
    {
        var validator = new RagOptionsValidator();
        var result = validator.Validate(
            name: null,
            new RagOptions
            {
                AnswerProvider = RagAnswerProvider.Deterministic,
                MaxAnswerTokens = 256,
                RequestTimeoutSeconds = 30,
                Temperature = 0.0f
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_AllowsOllamaProvider_WithAbsoluteBaseUrlAndModel()
    {
        var validator = new RagOptionsValidator();
        var result = validator.Validate(
            name: null,
            new RagOptions
            {
                AnswerProvider = RagAnswerProvider.Ollama,
                AnswerModel = "llama3.2:3b",
                BaseUrl = "http://localhost:11434/api",
                MaxAnswerTokens = 256,
                RequestTimeoutSeconds = 30,
                Temperature = 0.1f
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsForOllama_WhenModelIsMissing()
    {
        var validator = new RagOptionsValidator();
        var result = validator.Validate(
            name: null,
            new RagOptions
            {
                AnswerProvider = RagAnswerProvider.Ollama,
                AnswerModel = " ",
                BaseUrl = "http://localhost:11434/api",
                MaxAnswerTokens = 256,
                RequestTimeoutSeconds = 30,
                Temperature = 0.0f
            });

        Assert.False(result.Succeeded);
        var failures = Assert.IsAssignableFrom<IEnumerable<string>>(result.Failures);
        Assert.Contains(failures, failure => failure.Contains("Rag:AnswerModel", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsForOllama_WhenBaseUrlIsInvalid()
    {
        var validator = new RagOptionsValidator();
        var result = validator.Validate(
            name: null,
            new RagOptions
            {
                AnswerProvider = RagAnswerProvider.Ollama,
                AnswerModel = "llama3.2:3b",
                BaseUrl = "localhost:11434",
                MaxAnswerTokens = 256,
                RequestTimeoutSeconds = 30,
                Temperature = 0.0f
            });

        Assert.False(result.Succeeded);
        var failures = Assert.IsAssignableFrom<IEnumerable<string>>(result.Failures);
        Assert.Contains(failures, failure => failure.Contains("Rag:BaseUrl", StringComparison.Ordinal));
    }
}

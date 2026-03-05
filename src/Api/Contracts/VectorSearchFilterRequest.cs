namespace Api.Contracts;

public sealed record VectorSearchFilterRequest
{
    public string? DocIdEquals { get; init; }

    public string? SourceEquals { get; init; }

    public IReadOnlyList<string> TagsAny { get; init; } = [];

    public string? TenantIdEquals { get; init; }

    public DateTimeOffset? UpdatedAtUtcFrom { get; init; }

    public DateTimeOffset? UpdatedAtUtcTo { get; init; }
}

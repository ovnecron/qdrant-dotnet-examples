using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal static class SearchFilterRequestMapper
{
    public static SearchFilter Map(VectorSearchFilterRequest? filter)
    {
        if (filter is null)
        {
            return new SearchFilter();
        }

        return new SearchFilter
        {
            DocIdEquals = string.IsNullOrWhiteSpace(filter.DocIdEquals) ? null : filter.DocIdEquals.Trim(),
            SourceEquals = string.IsNullOrWhiteSpace(filter.SourceEquals) ? null : filter.SourceEquals.Trim(),
            TagsAny = filter.TagsAny
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            TenantIdEquals = string.IsNullOrWhiteSpace(filter.TenantIdEquals) ? null : filter.TenantIdEquals.Trim(),
            UpdatedAtUtcFrom = filter.UpdatedAtUtcFrom,
            UpdatedAtUtcTo = filter.UpdatedAtUtcTo
        };
    }
}

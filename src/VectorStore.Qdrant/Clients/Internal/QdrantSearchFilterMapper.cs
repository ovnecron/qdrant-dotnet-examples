using Qdrant.Client.Grpc;

using VectorStore.Abstractions.Contracts;

using static Qdrant.Client.Grpc.Conditions;

namespace VectorStore.Qdrant.Clients.Internal;

internal static class QdrantSearchFilterMapper
{
    public static Filter? BuildFilter(SearchFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var conditions = new List<Condition>();

        if (!string.IsNullOrWhiteSpace(filter.DocIdEquals))
        {
            conditions.Add(MatchKeyword("docId", filter.DocIdEquals));
        }

        if (!string.IsNullOrWhiteSpace(filter.SourceEquals))
        {
            conditions.Add(MatchKeyword("source", filter.SourceEquals));
        }

        if (!string.IsNullOrWhiteSpace(filter.TenantIdEquals))
        {
            conditions.Add(MatchKeyword("tenantId", filter.TenantIdEquals));
        }
        else if (filter.TenantIdIsNull)
        {
            conditions.Add(IsNull("tenantId"));
        }

        if (filter.TagsAny.Count > 0)
        {
            var tags = filter.TagsAny
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (tags.Length > 0)
            {
                conditions.Add(Match("tags", tags));
            }
        }

        if (filter.UpdatedAtUtcFrom is not null || filter.UpdatedAtUtcTo is not null)
        {
            conditions.Add(DatetimeRange(
                "updatedAtUtc",
                gte: filter.UpdatedAtUtcFrom?.UtcDateTime,
                lte: filter.UpdatedAtUtcTo?.UtcDateTime));
        }

        if (conditions.Count == 0)
        {
            return null;
        }

        var filterDefinition = new Filter();
        foreach (var condition in conditions)
        {
            filterDefinition.Must.Add(condition);
        }

        return filterDefinition;
    }
}

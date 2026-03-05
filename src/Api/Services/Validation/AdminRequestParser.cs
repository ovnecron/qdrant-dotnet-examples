using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal interface IAdminRequestParser
{
    bool TryParseInitializeCollectionRequest(
        InitializeCollectionRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out CollectionDefinition? definition,
        out Dictionary<string, string[]> errors);
}

internal sealed class AdminRequestParser : IAdminRequestParser
{
    private static readonly IReadOnlyList<string> DefaultPayloadIndexes =
    [
        "docId",
        "source",
        "tags",
        "tenantId",
        "updatedAtUtc"
    ];

    public bool TryParseInitializeCollectionRequest(
        InitializeCollectionRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out CollectionDefinition? definition,
        out Dictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(request);

        errors = new Dictionary<string, string[]>();
        definition = null;

        var collectionName = ResolveCollectionName(request.CollectionName, defaultCollectionName);
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collectionName"] = ["Collection name is required."];
        }

        var vectorSize = request.VectorSize;
        if (vectorSize is null || vectorSize <= 0)
        {
            errors["vectorSize"] = ["Vector size must be greater than zero."];
        }

        if (!TryParseDistance(request.Distance, out var distance))
        {
            errors["distance"] = ["Distance must be one of: Cosine, Dot, Euclid, Manhattan."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        var resolvedVectorSize = vectorSize.GetValueOrDefault();

        definition = new CollectionDefinition
        {
            Name = collectionName,
            VectorSize = resolvedVectorSize,
            Distance = distance,
            PayloadIndexes = ResolvePayloadIndexes(request.PayloadIndexes)
        };

        return true;
    }

    private static string ResolveCollectionName(string? requestedCollection, string defaultCollectionName)
    {
        if (!string.IsNullOrWhiteSpace(requestedCollection))
        {
            return requestedCollection.Trim();
        }

        return defaultCollectionName;
    }

    private static IReadOnlyList<string> ResolvePayloadIndexes(IReadOnlyList<string> requestedIndexes)
    {
        if (requestedIndexes.Count == 0)
        {
            return DefaultPayloadIndexes;
        }

        return requestedIndexes
            .Where(static index => !string.IsNullOrWhiteSpace(index))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryParseDistance(string? rawDistance, out VectorDistance distance)
    {
        if (string.IsNullOrWhiteSpace(rawDistance))
        {
            distance = VectorDistance.Cosine;
            return true;
        }

        return Enum.TryParse(rawDistance, ignoreCase: true, out distance);
    }
}

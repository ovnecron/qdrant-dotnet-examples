using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

namespace Api.Services.Validation;

internal sealed class AnomalyRequestParser : IAnomalyRequestParser
{
    private const float DefaultThreshold = 0.35f;

    public bool TryParseScoreRequest(
        AnomalyScoreRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out AnomalyScoreCommand? command,
        out Dictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(request);

        errors = new Dictionary<string, string[]>();
        command = null;

        var collectionName = ResolveCollectionName(request.Collection, defaultCollectionName);
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            errors["collection"] = ["Collection is required."];
        }

        var vector = request.Vector;
        if (vector.Count == 0)
        {
            errors["vector"] = ["Vector must contain at least one value."];
        }

        if (request.TopK <= 0)
        {
            errors["topK"] = ["TopK must be greater than zero."];
        }

        var threshold = request.Threshold ?? DefaultThreshold;
        if (threshold is < 0f or > 1f)
        {
            errors["threshold"] = ["Threshold must be between 0 and 1."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        command = new AnomalyScoreCommand(
            collectionName,
            vector.ToArray(),
            request.TopK,
            threshold,
            SearchFilterRequestMapper.Map(request.Filter),
            request.IncludeNeighbors);

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
}

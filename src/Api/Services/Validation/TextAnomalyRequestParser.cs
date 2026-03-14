using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

namespace Api.Services.Validation;

internal sealed class TextAnomalyRequestParser : ITextAnomalyRequestParser
{
    public bool TryParseScoreRequest(
        TextAnomalyScoreRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out TextAnomalyScoreCommand? command,
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

        var text = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            errors["text"] = ["Text is required."];
        }

        if (request.TopK <= 0)
        {
            errors["topK"] = ["TopK must be greater than zero."];
        }

        var threshold = request.Threshold ?? AnomalyDefaults.DefaultThreshold;
        if (threshold is < 0f or > 1f)
        {
            errors["threshold"] = ["Threshold must be between 0 and 1."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        command = new TextAnomalyScoreCommand(
            collectionName,
            text!,
            request.TopK,
            threshold,
            SearchFilterRequestMapper.Map(request.Filter),
            request.IncludeNeighbors,
            request.IncludeDebug);

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

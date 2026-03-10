using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

namespace Api.Services.Validation;

internal sealed class RagRequestParser : IRagRequestParser
{
    public bool TryParseQueryRequest(
        RagQueryRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out RagQueryCommand? command,
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

        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            errors["question"] = ["Question is required."];
        }

        if (request.TopK <= 0)
        {
            errors["topK"] = ["TopK must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        command = new RagQueryCommand(
            collectionName,
            question!,
            request.TopK,
            request.MinScore,
            SearchFilterRequestMapper.Map(request.Filter),
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

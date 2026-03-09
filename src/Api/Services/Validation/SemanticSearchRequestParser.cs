using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal sealed record SemanticSearchQueryCommand(
    string CollectionName,
    string QueryText,
    int TopK,
    float? MinScore,
    SearchFilter Filter);

internal interface ISemanticSearchRequestParser
{
    bool TryParseQueryRequest(
        SemanticSearchQueryRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out SemanticSearchQueryCommand? command,
        out Dictionary<string, string[]> errors);
}

internal sealed class SemanticSearchRequestParser : ISemanticSearchRequestParser
{
    public bool TryParseQueryRequest(
        SemanticSearchQueryRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out SemanticSearchQueryCommand? command,
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

        var queryText = request.QueryText?.Trim();
        if (string.IsNullOrWhiteSpace(queryText))
        {
            errors["queryText"] = ["Query text is required."];
        }

        if (request.TopK <= 0)
        {
            errors["topK"] = ["TopK must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        var resolvedQueryText = queryText!;

        command = new SemanticSearchQueryCommand(
            collectionName,
            resolvedQueryText,
            request.TopK,
            request.MinScore,
            SearchFilterRequestMapper.Map(request.Filter));

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

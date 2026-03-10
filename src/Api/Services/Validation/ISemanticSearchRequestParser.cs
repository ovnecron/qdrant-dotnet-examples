using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

namespace Api.Services.Validation;

internal interface ISemanticSearchRequestParser
{
    bool TryParseQueryRequest(
        SemanticSearchQueryRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out SemanticSearchQueryCommand? command,
        out Dictionary<string, string[]> errors);
}

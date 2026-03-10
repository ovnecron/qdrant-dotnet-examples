using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

namespace Api.Services.Validation;

internal interface IRagRequestParser
{
    bool TryParseQueryRequest(
        RagQueryRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out RagQueryCommand? command,
        out Dictionary<string, string[]> errors);
}

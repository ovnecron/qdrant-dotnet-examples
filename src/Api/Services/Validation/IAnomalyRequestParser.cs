using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

namespace Api.Services.Validation;

internal interface IAnomalyRequestParser
{
    bool TryParseScoreRequest(
        AnomalyScoreRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out AnomalyScoreCommand? command,
        out Dictionary<string, string[]> errors);
}

using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

namespace Api.Services.Validation;

internal interface ITextAnomalyRequestParser
{
    bool TryParseScoreRequest(
        TextAnomalyScoreRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out TextAnomalyScoreCommand? command,
        out Dictionary<string, string[]> errors);
}

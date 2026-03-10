using System.Diagnostics.CodeAnalysis;

using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Validation;

internal interface IVectorRequestParser
{
    bool TryParseDeleteRequest(
        VectorDeleteRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out DeleteVectorsCommand? command,
        out Dictionary<string, string[]> errors);

    bool TryParseUpsertRequest(
        VectorUpsertRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out UpsertVectorsCommand? command,
        out Dictionary<string, string[]> errors);

    bool TryParseSearchRequest(
        VectorSearchRequest request,
        string defaultCollectionName,
        [NotNullWhen(true)] out SearchRequest? searchRequest,
        out Dictionary<string, string[]> errors);
}

using Api.Contracts;

using VectorStore.Abstractions.Contracts;

namespace Api.Services.Mappers;

internal interface IAdminResponseMapper
{
    InitializeCollectionResponse ToResponse(CollectionInitResult result);
}

internal sealed class AdminResponseMapper : IAdminResponseMapper
{
    public InitializeCollectionResponse ToResponse(CollectionInitResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new InitializeCollectionResponse
        {
            CollectionName = result.CollectionName,
            Created = result.Created
        };
    }
}

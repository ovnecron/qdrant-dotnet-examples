using Integration.Fixtures;

namespace Integration;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<ApiIntegrationFixture>
{
    public const string Name = "api-integration";
}

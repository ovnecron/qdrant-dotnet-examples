using QdrantDotNetExample.Common;

namespace Unit;

[Trait("Category", "Unit")]
public sealed class QdrantContainerImageTests
{
    [Fact]
    public void Parse_UsesDefaultImage_WhenValueIsMissing()
    {
        var resolved = QdrantContainerImage.Parse(null);

        Assert.Equal(QdrantContainerImage.DefaultRepository, resolved.Repository);
        Assert.Equal(QdrantContainerImage.DefaultTag, resolved.Tag);
    }

    [Fact]
    public void Parse_UsesConfiguredRepositoryAndTag_WhenTagIsProvided()
    {
        var resolved = QdrantContainerImage.Parse("qdrant/qdrant:v1.15.0");

        Assert.Equal("qdrant/qdrant", resolved.Repository);
        Assert.Equal("v1.15.0", resolved.Tag);
    }

    [Fact]
    public void Parse_UsesDefaultTag_WhenRepositoryHasNoTag()
    {
        var resolved = QdrantContainerImage.Parse("ghcr.io/acme/qdrant");

        Assert.Equal("ghcr.io/acme/qdrant", resolved.Repository);
        Assert.Equal(QdrantContainerImage.DefaultTag, resolved.Tag);
    }

    [Fact]
    public void Parse_UsesDefaultTag_WhenRegistryPortIsPresentWithoutTag()
    {
        var resolved = QdrantContainerImage.Parse("localhost:5000/qdrant");

        Assert.Equal("localhost:5000/qdrant", resolved.Repository);
        Assert.Equal(QdrantContainerImage.DefaultTag, resolved.Tag);
    }

    [Fact]
    public void Parse_ParsesTag_WhenRegistryPortAndTagArePresent()
    {
        var resolved = QdrantContainerImage.Parse("localhost:5000/qdrant:v1.15.0");

        Assert.Equal("localhost:5000/qdrant", resolved.Repository);
        Assert.Equal("v1.15.0", resolved.Tag);
    }
}

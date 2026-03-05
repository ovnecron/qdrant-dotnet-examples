namespace QdrantDotNetExample.Common;

internal static class QdrantContainerImage
{
    public const string EnvironmentVariableName = "QDRANT_CONTAINER_IMAGE";
    public const string DefaultRepository = "qdrant/qdrant";
    public const string DefaultTag = "v1.17.0";

    public static (string Repository, string Tag) Resolve()
    {
        var configuredImage = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        return Parse(configuredImage);
    }

    internal static (string Repository, string Tag) Parse(string? configuredImage)
    {
        if (string.IsNullOrWhiteSpace(configuredImage))
        {
            return (DefaultRepository, DefaultTag);
        }

        var trimmedImage = configuredImage.Trim();
        var separatorIndex = trimmedImage.LastIndexOf(':');
        var lastSlashIndex = trimmedImage.LastIndexOf('/');
        var hasExplicitTag = separatorIndex > 0 &&
            separatorIndex < trimmedImage.Length - 1 &&
            separatorIndex > lastSlashIndex;

        if (!hasExplicitTag)
        {
            return (trimmedImage, DefaultTag);
        }

        var repository = trimmedImage[..separatorIndex];
        var tag = trimmedImage[(separatorIndex + 1)..];
        return (repository, tag);
    }
}

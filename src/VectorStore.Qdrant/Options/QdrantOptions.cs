namespace VectorStore.Qdrant.Options;

public sealed record QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string? EndpointRest { get; set; }

    public string? EndpointGrpc { get; set; }

    public string? ApiKey { get; set; }

    public string Collection { get; set; } = "knowledge_chunks";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

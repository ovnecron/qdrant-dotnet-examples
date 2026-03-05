using QdrantDotNetExample.Common;

var builder = DistributedApplication.CreateBuilder(args);
var (qdrantRepository, qdrantTag) = QdrantContainerImage.Resolve();

var qdrant = builder
    .AddContainer("qdrant", qdrantRepository, qdrantTag)
    .WithEndpoint(
        targetPort: 6333,
        scheme: "http",
        name: "rest",
        env: "QDRANT_REST_PORT",
        isExternal: true,
        isProxied: false)
    .WithEndpoint(
        targetPort: 6334,
        scheme: "http",
        name: "grpc",
        env: "QDRANT_GRPC_PORT",
        isExternal: true,
        isProxied: false)
    .WithVolume("qdrant-data", "/qdrant/storage");

var qdrantRestEndpoint = qdrant.GetEndpoint("rest");
var qdrantGrpcEndpoint = qdrant.GetEndpoint("grpc");

builder
    .AddProject("api", "../Api/Api.csproj")
    .WithEnvironment("QDRANT__ENDPOINT_REST", qdrantRestEndpoint)
    .WithEnvironment("QDRANT__ENDPOINT_GRPC", qdrantGrpcEndpoint)
    .WaitFor(qdrant);

builder.Build().Run();

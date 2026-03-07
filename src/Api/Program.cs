using Api.Endpoints;
using Api.HealthChecks;
using Api.Options;
using Api.Services;
using Api.Services.Ingestion;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using VectorStore.Qdrant;
using VectorStore.Qdrant.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1");
builder.Services.AddSingleton<IValidateOptions<QdrantOptions>, QdrantOptionsValidator>();
builder.Services
    .AddOptions<QdrantOptions>()
    .Bind(builder.Configuration.GetSection(QdrantOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddQdrantVectorStore();
builder.Services.AddSingleton<IAdminRequestParser, AdminRequestParser>();
builder.Services.AddSingleton<IVectorRequestParser, VectorRequestParser>();
builder.Services.AddSingleton<IMarkdownChunker, MarkdownChunker>();
builder.Services.AddSingleton<IAdminResponseMapper, AdminResponseMapper>();
builder.Services.AddSingleton<IVectorResponseMapper, VectorResponseMapper>();
builder.Services.AddSingleton<IServiceResultExecutor, ServiceResultExecutor>();
builder.Services.AddScoped<IAdminEndpointService, AdminEndpointService>();
builder.Services.AddScoped<IVectorEndpointService, VectorEndpointService>();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<QdrantConfigurationHealthCheck>("qdrant-config", tags: ["ready"]);

var app = builder.Build();

app.MapOpenApi("/swagger/{documentName}/swagger.json");
app.MapHealthEndpoints();
app.MapAdminEndpoints();
app.MapVectorEndpoints();

app.Run();

public partial class Program;

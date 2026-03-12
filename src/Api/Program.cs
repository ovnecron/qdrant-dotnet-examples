using Api.Endpoints;
using Api.HealthChecks;
using Api.Options;
using Api.Services;
using Api.Services.Ingestion;
using Api.Services.Mappers;
using Api.Services.Results;
using Api.Services.Validation;

using Embeddings.Clients;
using Embeddings.Interfaces;
using Embeddings.Options;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using VectorStore.Qdrant;
using VectorStore.Qdrant.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IValidateOptions<EmbeddingOptions>, EmbeddingOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<QdrantOptions>, QdrantOptionsValidator>();
builder.Services
    .AddOptions<EmbeddingOptions>()
    .Bind(builder.Configuration.GetSection(EmbeddingOptions.SectionName))
    .ValidateOnStart();
builder.Services
    .AddOptions<QdrantOptions>()
    .Bind(builder.Configuration.GetSection(QdrantOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddConfiguredTextEmbeddingClient();
builder.Services.AddQdrantVectorStore();
builder.Services.AddSingleton<IAdminRequestParser, AdminRequestParser>();
builder.Services.AddSingleton<IAnomalyRequestParser, AnomalyRequestParser>();
builder.Services.AddSingleton<IMarkdownIngestRequestParser, MarkdownIngestRequestParser>();
builder.Services.AddSingleton<IRagRequestParser, RagRequestParser>();
builder.Services.AddSingleton<ISemanticSearchRequestParser, SemanticSearchRequestParser>();
builder.Services.AddSingleton<IVectorRequestParser, VectorRequestParser>();
builder.Services.AddSingleton<IMarkdownChunker, MarkdownChunker>();
builder.Services.AddSingleton<IChunkEmbeddingTextFactory, ChunkEmbeddingTextFactory>();
builder.Services.AddSingleton<IIngestionJobStore, InMemoryIngestionJobStore>();
builder.Services.AddSingleton<IMarkdownIngestionQueue, MarkdownIngestionQueue>();
builder.Services.AddSingleton<IAdminResponseMapper, AdminResponseMapper>();
builder.Services.AddSingleton<IAnomalyResponseMapper, AnomalyResponseMapper>();
builder.Services.AddSingleton<IRagResponseMapper, RagResponseMapper>();
builder.Services.AddSingleton<ISemanticSearchResponseMapper, SemanticSearchResponseMapper>();
builder.Services.AddSingleton<IVectorResponseMapper, VectorResponseMapper>();
builder.Services.AddSingleton<IServiceResultExecutor, ServiceResultExecutor>();
builder.Services.AddScoped<IAdminEndpointService, AdminEndpointService>();
builder.Services.AddScoped<IAnomalyScoreCalculator, CosineAnomalyScoreCalculator>();
builder.Services.AddScoped<IAnomalyEndpointService, AnomalyEndpointService>();
builder.Services.AddScoped<IRagContextAssembler, RagContextAssembler>();
builder.Services.AddScoped<IRagAnswerGenerator, DeterministicRagAnswerGenerator>();
builder.Services.AddScoped<IRagEndpointService, RagEndpointService>();
builder.Services.AddScoped<IMarkdownIngestionProcessor, MarkdownIngestionProcessor>();
builder.Services.AddScoped<IIngestionEndpointService, IngestionEndpointService>();
builder.Services.AddScoped<ITextRetrievalService, TextRetrievalService>();
builder.Services.AddScoped<ISemanticSearchEndpointService, SemanticSearchEndpointService>();
builder.Services.AddScoped<IVectorEndpointService, VectorEndpointService>();
builder.Services.AddHostedService<MarkdownIngestionBackgroundService>();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<QdrantConfigurationHealthCheck>("qdrant-config", tags: ["ready"]);

var app = builder.Build();

app.MapOpenApi("/swagger/{documentName}/swagger.json");
app.MapHealthEndpoints();
app.MapAdminEndpoints();
app.MapAnomalyEndpoints();
app.MapIngestEndpoints();
app.MapRagEndpoints();
app.MapSemanticSearchEndpoints();
app.MapVectorEndpoints();

app.Run();

public partial class Program;

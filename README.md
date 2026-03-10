# Qdrant + .NET 10 RAG Starter

[![CI](https://github.com/ovnecron/qdrant-dotnet-examples/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/ovnecron/qdrant-dotnet-examples/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Qdrant](https://img.shields.io/badge/Qdrant-v1.17.0-EA2845)](https://qdrant.tech/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Starter repository for a Qdrant-backed .NET 10 RAG stack with local orchestration via Aspire.

## Prerequisites

- .NET SDK 10.x
- Docker Desktop (or compatible container runtime)
- Git

## Quickstart (5-10 minutes)

```bash
git clone https://github.com/ovnecron/qdrant-dotnet-examples.git
cd qdrant-dotnet-examples
```

Check SDK from `global.json`:

```bash
dotnet --info
```

Restore packages:

```bash
dotnet restore QdrantDotNetExample.sln
```

Start the local stack (AppHost + API + Qdrant):

```bash
dotnet run --project src/AppHost
```

## Tutorials

- `docs/tutorial/01-local-run.md`
- `docs/tutorial/02-qdrant-basics.md`
- `docs/tutorial/03-markdown-ingestion.md`
- `docs/tutorial/04-semantic-search.md`
- `docs/tutorial/05-rag-query.md`

Recommended order:

- Tutorial `02` uses manual 3-dimensional vectors.
- Tutorials `03` and `04` use the embedding-based ingestion/search path with the configured embedding dimension (default: `384`).
- Tutorial `04` builds directly on the ingestion flow from Tutorial `03`.
- Tutorial `05` builds on the retrieval path from Tutorial `04` and adds grounded answer generation with citations.

## Verify Local Runtime

In a second terminal:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}

curl -s "$API_BASE_URL/health"
curl -s "$API_BASE_URL/ready"
curl -s "$API_BASE_URL/swagger/v1/swagger.json" | head
```

Expected:

- `/health` returns HTTP `200`
- `/ready` returns HTTP `200` when API can reach Qdrant
- OpenAPI document is reachable at `/swagger/v1/swagger.json`

## Local Quality Commands

```bash
dotnet format QdrantDotNetExample.sln --verify-no-changes
dotnet build QdrantDotNetExample.sln -c Release
dotnet test tests/Unit/Unit.csproj -c Release
dotnet test tests/Integration/Integration.csproj -c Release
```

Notes:

- `tests/Unit` runs fast and does not require Docker.
- `tests/Integration` requires Docker (Qdrant testcontainer).
- `tests/Integration` also includes a deterministic semantic-search eval-lite regression check (`Recall@3`) against a small fixed dataset.

## Configuration

- `.env.example` contains local environment defaults and optional overrides (no secrets).
- Markdown ingestion uses the configured embedding dimension (default: `384`), so ingest collections must be initialized with the same vector size.
- `Embedding:Provider=Deterministic` is the default for tests, CI, and zero-secret local runs.
- `Embedding:Provider=Ollama` is supported for local real embeddings via `http://localhost:11434/api`.
- When using Ollama, make sure Ollama is running locally and that you have pulled a compatible embedding model first.
- When using Ollama, set `Embedding:Model` and `Embedding:Dimension` to the actual embedding model you run locally, then initialize Qdrant collections with the same vector size.
- If no endpoint is configured, the Qdrant gRPC client falls back to `http://localhost:6334`.
- If you set a custom REST endpoint port, set `QDRANT__ENDPOINT_GRPC` explicitly.
- Qdrant container image is pinned to `qdrant/qdrant:v1.17.0`. Override with `QDRANT_CONTAINER_IMAGE=<repository:tag>` in AppHost and integration tests.
- Keep secrets out of git.
- Use user-secrets for local development (for example Qdrant API key):

```bash
dotnet user-secrets init --project src/Api
dotnet user-secrets set --project src/Api "Qdrant:ApiKey" "<secret>"
```

Example local Ollama override:

```bash
ollama pull <your-ollama-embedding-model>

dotnet user-secrets set --project src/Api "Embedding:Provider" "Ollama"
dotnet user-secrets set --project src/Api "Embedding:Model" "<your-ollama-embedding-model>"
dotnet user-secrets set --project src/Api "Embedding:Dimension" "<your-model-dimension>"
dotnet user-secrets set --project src/Api "Embedding:BaseUrl" "http://localhost:11434/api"
```

## CI

GitHub Actions workflows are defined in:

- `.github/workflows/ci.yml`

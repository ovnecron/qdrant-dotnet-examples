# Qdrant + .NET 10 RAG Starter

Starter repository for a Qdrant-backed .NET 10 RAG stack with local orchestration via Aspire.

## Prerequisites

- .NET SDK 10.x
- Docker Desktop (or compatible container runtime)
- Git

## Quickstart (5-10 minutes)

```bash
git clone <repo-url>
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

Start the local stack (AppHost + API + Agent + Qdrant):

```bash
dotnet run --project src/AppHost
```

## Tutorials

- `docs/tutorial/01-local-run.md`
- `docs/tutorial/02-qdrant-basics.md`

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
- `/ready` returns HTTP `200` when required runtime config is present
- OpenAPI document is reachable at `/swagger/v1/swagger.json`

## Local Quality Commands

```bash
dotnet format QdrantDotNetExample.sln --verify-no-changes
dotnet build QdrantDotNetExample.sln -c Release
dotnet test QdrantDotNetExample.sln -c Release
```

Notes:

- `dotnet test QdrantDotNetExample.sln -c Release` currently includes integration tests and therefore requires Docker.
- Use `dotnet test tests/Integration/Integration.csproj -c Release` for an integration-only run.

## Configuration

- `.env.example` contains local environment defaults (no secrets).
- Keep secrets out of git.
- Use user-secrets for local development:

```bash
dotnet user-secrets init --project src/Api
dotnet user-secrets set --project src/Api "Llm:ApiKey" "<secret>"
dotnet user-secrets set --project src/Api "Embedding:ApiKey" "<secret>"

dotnet user-secrets init --project src/Agent
dotnet user-secrets set --project src/Agent "Llm:ApiKey" "<secret>"
```

## CI

GitHub Actions workflows are defined in:

- `.github/workflows/ci.yml`
- `.github/workflows/security.yml`
- `.github/workflows/codeql.yml`

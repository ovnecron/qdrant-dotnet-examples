# Tutorial 01: Local Run

This tutorial gets the local stack running with Aspire (`AppHost + API + Agent + Qdrant`) and verifies health + OpenAPI.

## Prerequisites

- .NET SDK 10.x
- Docker Desktop running
- Git

## Start

From repository root:

```bash
dotnet restore QdrantDotNetExample.sln
dotnet run --project src/AppHost
```

Default dashboard URL:

- `http://localhost:15111`

## Runtime Verification

Open a second terminal:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}

curl -i "$API_BASE_URL/health"
curl -i "$API_BASE_URL/ready"
curl -i "$API_BASE_URL/swagger/v1/swagger.json"
```

Expected:

- `/health` returns `200`
- `/ready` returns `200`
- OpenAPI JSON is reachable

## Troubleshooting

`/ready` returns `503` with `Qdrant REST endpoint is not configured.`:

- Start the API through AppHost (`dotnet run --project src/AppHost`) so Qdrant endpoints are injected.

AppHost dashboard not reachable or shows `localhost:0`:

- Start with the default profile: `dotnet run --project src/AppHost`
- If you force `--launch-profile https`, ensure Aspire OTLP env vars are present in launch settings.

Services stay on `Starting` in dashboard:

- Verify Docker is healthy and Qdrant container can start.
- Check for host port conflicts if you manually pin ports.

Quick Docker checks:

```bash
docker ps -a --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
docker logs <qdrant-container-name>
```

# Tutorial 03: Markdown Ingestion

This tutorial covers the background markdown ingestion flow:

- initialize a collection for the configured embedding dimension
- submit `POST /api/v1/ingest/markdown`
- poll job status via `GET /api/v1/ingest/jobs/{jobId}`

## Prerequisites

- Local stack is running (`dotnet run --project src/AppHost`)
- API reachable at `http://localhost:5010`
- embedding configuration is unchanged from the defaults (`Dimension = 384`)

Set base URL:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}
```

## 1) Initialize an Ingest Collection

This tutorial uses a dedicated collection to avoid clashing with the 3-dimensional examples from Tutorial 02.

```bash
curl -i -X POST "$API_BASE_URL/api/v1/admin/collections/init" \
  -H "Content-Type: application/json" \
  -d '{"collectionName":"knowledge_chunks_ingest","vectorSize":384,"distance":"Cosine"}'
```

Expected:

- first call: `201 Created`
- repeated call with same payload: `200 OK` and `created:false`

## 2) Submit a Markdown Ingest Job

```bash
curl -i -X POST "$API_BASE_URL/api/v1/ingest/markdown" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_ingest",
    "docId":"guide-local-run",
    "sourceId":"docs/local-run.md",
    "title":"Local Run",
    "markdown":"# Local Run\n\nRun AppHost and wait until resources are healthy.",
    "tags":["tutorial","local"],
    "tenantId":"tenant-a"
  }'
```

Expected:

- `202 Accepted`
- response contains `jobId`, `docId`, `docVersion`, `acceptedAtUtc`
- response also includes the active embedding metadata (`embeddingModel`, `embeddingSchemaVersion`)

Notes:

- `collection` is optional and otherwise falls back to the configured default collection
- the API generates `docVersion` automatically
- re-ingesting the same `docId` replaces the active chunks for that `docId` and `tenantId`

## 3) Poll Job Status

```bash
JOB_ID="<paste-job-id-here>"

curl -i "$API_BASE_URL/api/v1/ingest/jobs/$JOB_ID"
```

Expected terminal states:

- `Succeeded`
- `Failed`

On success, the payload includes:

- `startedAtUtc`
- `completedAtUtc`
- `result.chunkCount`
- `result.upsertedCount`

On failure, the payload includes:

- `error.code`
- `error.message`

## Current Runtime Model

The background queue and job status store are currently in-memory and live inside the API process.

That means:

- `202 Accepted` is a real background workflow, not a fake synchronous response
- queued and running jobs are lost if the API process restarts
- this is suitable for local learning and iteration, but not yet a durable production ingestion pipeline

## Eval-lite Retrieval Guard

Integration tests include a small eval-lite dataset with deterministic embeddings.

The goal is not to prove production retrieval quality. The goal is to catch regressions in:

- chunking
- embedding text preparation
- ingestion
- vector search behavior

The current guard evaluates `Recall@3` against a fixed local fixture dataset.

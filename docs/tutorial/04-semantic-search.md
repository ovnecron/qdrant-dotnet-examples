# Tutorial 04: Semantic Search

This tutorial covers the text-query semantic search slice:

- ingest markdown documents into a 384-dimensional collection
- call `POST /api/v1/search/query`
- inspect ranked hits returned from the configured embedding provider

## Prerequisites

- Local stack is running (`dotnet run --project src/AppHost`)
- API reachable at `http://localhost:5010`
- Either follow Tutorial 03 first or use the setup below

Set base URL:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}
```

## 1) Initialize a Search Collection

```bash
curl -i -X POST "$API_BASE_URL/api/v1/admin/collections/init" \
  -H "Content-Type: application/json" \
  -d '{"collectionName":"knowledge_chunks_search","vectorSize":384,"distance":"Cosine"}'
```

## 2) Ingest Two Small Documents

```bash
curl -i -X POST "$API_BASE_URL/api/v1/ingest/markdown" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_search",
    "docId":"guide-local-run",
    "sourceId":"docs/local-run.md",
    "title":"Local Run",
    "markdown":"# Local Run\n\nCheck the /health and /ready endpoints after starting AppHost.",
    "tags":["tutorial","local"],
    "tenantId":"tenant-a"
  }'
```

```bash
curl -i -X POST "$API_BASE_URL/api/v1/ingest/markdown" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_search",
    "docId":"guide-vector-delete",
    "sourceId":"docs/vector-delete.md",
    "title":"Delete Vectors",
    "markdown":"# Delete Vectors\n\nDelete vectors by chunk ids when you want to remove stale content.",
    "tags":["vector","admin"],
    "tenantId":"tenant-a"
  }'
```

Poll the returned job ids as shown in Tutorial 03 until both jobs reach `Succeeded`.

## 3) Run Semantic Search

```bash
curl -i -X POST "$API_BASE_URL/api/v1/search/query" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_search",
    "queryText":"How do I check health and ready endpoints locally?",
    "topK":3,
    "filter":{"tenantIdEquals":"tenant-a","tagsAny":["local"]}
  }'
```

Expected:

- `200 OK`
- response includes:
  - `traceId`
  - `collection`
  - `queryText`
  - `embeddingProvider`
  - `embeddingModel`
  - `embeddingSchemaVersion`
  - ranked `hits`

Typical hit fields:

- `chunkId`
- `docId`
- `score`
- `source`
- `title`
- `section`
- `contentPreview`
- `tags`

## Notes

- The endpoint uses the configured embedding provider for the query text.
- Default local runs use `Embedding:Provider=Deterministic`.
- If you switch to Ollama, the model dimension must still match the Qdrant collection vector size.
- The semantic-search eval-lite guard in integration tests runs through this endpoint, so regressions in text-query search should be caught by CI.

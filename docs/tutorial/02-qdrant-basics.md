# Tutorial 02: Qdrant Basics

This tutorial covers collection init, vector upsert, vector search, vector deletion, vector retrieval by id, and validation behavior via the API.

It uses manual 3-dimensional vectors. The markdown ingestion flow in Tutorial 03 uses the configured embedding dimension instead (default: `384`) and should use a separate collection or a freshly initialized default collection.

## Prerequisites

- Local stack is running (`dotnet run --project src/AppHost`)
- API reachable at `http://localhost:5010`

Set base URL:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}
```

## 1) Initialize Collection

```bash
curl -i -X POST "$API_BASE_URL/api/v1/admin/collections/init" \
  -H "Content-Type: application/json" \
  -d '{"collectionName":"knowledge_chunks","vectorSize":3,"distance":"Cosine"}'
```

Expected:

- first call: `201 Created`
- repeated call with same payload: `200 OK` and `created:false`

## 2) Upsert Vectors

```bash
curl -i -X POST "$API_BASE_URL/api/v1/vectors/upsert" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks",
    "points":[
      {
        "id":"doc-1:0",
        "vector":[0.12,-0.09,0.31],
        "payload":{"docId":"doc-1","source":"docs/intro.md","tags":["getting-started"],"content":"Run AppHost and wait."}
      },
      {
        "id":"doc-1:1",
        "vector":[0.10,-0.06,0.28],
        "payload":{"docId":"doc-1","source":"docs/intro.md","tags":["setup"],"content":"Qdrant endpoint is configured."}
      }
    ]
  }'
```

Expected:

- `201 Created`
- `upsertedCount` equals inserted point count

## 3) Search with Filters

```bash
curl -i -X POST "$API_BASE_URL/api/v1/vectors/search" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks",
    "queryVector":[0.11,-0.05,0.27],
    "topK":5,
    "minScore":0.0,
    "filter":{"tagsAny":["getting-started"],"sourceEquals":"docs/intro.md"}
  }'
```

Expected:

- `200 OK`
- response contains filtered hits (for example `doc-1:0`)

## 4) Fetch Vector by Id

```bash
curl -i "$API_BASE_URL/api/v1/vectors/knowledge_chunks/doc-1:0"
```

Expected:

- `200 OK`
- response includes full stored vector record including payload fields
- when the collection uses `Cosine`, Qdrant returns the normalized vector values

## 5) Delete Vectors by Chunk Id

```bash
curl -i -X DELETE "$API_BASE_URL/api/v1/vectors" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks",
    "chunkIds":["doc-1:0","doc-1:1"]
  }'
```

Expected:

- `200 OK`
- `deletedCount` equals the number of existing unique chunk ids that were removed
- unknown chunk ids are ignored and do not fail the request

## 6) Validation Errors

Collection init with invalid vector size:

```bash
curl -i -X POST "$API_BASE_URL/api/v1/admin/collections/init" \
  -H "Content-Type: application/json" \
  -d '{"vectorSize":0}'
```

Search with invalid payload:

```bash
curl -i -X POST "$API_BASE_URL/api/v1/vectors/search" \
  -H "Content-Type: application/json" \
  -d '{"collection":"knowledge_chunks","queryVector":[],"topK":0}'
```

Expected:

- both return `400 Bad Request`
- response includes structured validation errors

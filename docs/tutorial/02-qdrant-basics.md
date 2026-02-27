# Tutorial 02: Qdrant Basics

This tutorial covers collection init, vector upsert, vector search, and validation behavior via the API.

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

## 4) Validation Errors

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

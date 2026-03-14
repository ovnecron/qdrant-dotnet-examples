# Tutorial 05: RAG Query

This tutorial covers the first grounded answer slice:

- retrieve relevant chunks for a natural-language question
- call `POST /api/v1/rag/query`
- inspect the returned answer, citations, and optional debug metadata

## Prerequisites

- Local stack is running (`dotnet run --project src/AppHost`)
- API reachable at `http://localhost:5010`
- Either follow Tutorials 03 and 04 first or use the setup below

Set base URL:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}
```

## 1) Initialize a RAG Collection

```bash
curl -i -X POST "$API_BASE_URL/api/v1/admin/collections/init" \
  -H "Content-Type: application/json" \
  -d '{"collectionName":"knowledge_chunks_rag","vectorSize":384,"distance":"Cosine"}'
```

## 2) Ingest Two Small Documents

```bash
curl -i -X POST "$API_BASE_URL/api/v1/ingest/markdown" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_rag",
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
    "collection":"knowledge_chunks_rag",
    "docId":"guide-vector-delete",
    "sourceId":"docs/vector-delete.md",
    "title":"Delete Vectors",
    "markdown":"# Delete Vectors\n\nDelete vectors by chunk ids when you want to remove stale content.",
    "tags":["vector","admin"],
    "tenantId":"tenant-a"
  }'
```

Poll the returned job ids as shown in Tutorial 03 until both jobs reach `Succeeded`.

## 3) Run a Grounded RAG Query

```bash
curl -i -X POST "$API_BASE_URL/api/v1/rag/query" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_rag",
    "question":"How do I check health and ready endpoints locally?",
    "topK":3,
    "includeDebug":true,
    "filter":{"tenantIdEquals":"tenant-a","tagsAny":["local"]}
  }'
```

Expected:

- `200 OK`
- response includes:
  - `traceId`
  - `answer`
  - `citations`
  - optional `debug`

Typical citation fields:

- `chunkId`
- `docId`
- `source`
- `title`
- `section`
- `score`

When `includeDebug` is `true`, the response also includes:

- `collection`
- `embeddingProvider`
- `embeddingModel`
- `embeddingSchemaVersion`
- `answerProvider`
- `answerModel`
- `retrievedHitCount`

## 4) No-Evidence Path

If retrieval does not find grounded evidence, the endpoint returns `422 Unprocessable Entity`.

Example:

```bash
curl -i -X POST "$API_BASE_URL/api/v1/rag/query" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_rag",
    "question":"How do I check health and ready endpoints locally?",
    "topK":3,
    "filter":{"tenantIdEquals":"tenant-missing"}
  }'
```

Expected:

- `422 Unprocessable Entity`
- problem response title: `Insufficient grounded context`

## Notes

- The retrieval path reuses the same configured embedding provider as Tutorial 04.
- Default local runs use deterministic answer generation to keep the workflow reproducible for local runs, tests, and CI.
- The answer provider is selected through configuration, not through the request payload.
- If `Rag:AnswerProvider` is set to `Ollama`, the same `POST /api/v1/rag/query` endpoint uses Ollama's `/api/generate` endpoint for answer generation.
- If `Rag:AnswerProvider=Ollama` and Ollama is unavailable or misconfigured, `rag/query` returns `503 Service Unavailable` instead of silently falling back to deterministic answers.
- The response is still grounded by retrieved Qdrant evidence because citations come from the assembled retrieval context, not from the answer generator.
- This first RAG slice runs entirely inside the API process. A separate Agent/worker host can be extracted later if the workflow grows enough to justify it.

## Optional: Switch Answer Generation to Local Ollama

Pull a local Ollama model first:

```bash
ollama pull <your-ollama-answer-model>
```

Then configure the API:

```bash
dotnet user-secrets set --project src/Api "Rag:AnswerProvider" "Ollama"
dotnet user-secrets set --project src/Api "Rag:AnswerModel" "<your-ollama-answer-model>"
dotnet user-secrets set --project src/Api "Rag:BaseUrl" "http://localhost:11434/api"
```

After restarting `src/AppHost`, the same `rag/query` request will return:

- `answerProvider: "Ollama"` in `debug`
- `answerModel` set to the configured Ollama model

# Tutorial 07: Text Anomaly Detection

This tutorial covers the first modality-specific anomaly slice:

- ingest a small baseline of operational text
- call `POST /api/v1/anomaly/text/score`
- inspect the anomaly score, nearest textual neighbors, and optional embedding debug metadata

This slice reuses:

- the embedding-based ingestion path from Tutorial `03`
- the anomaly-scoring core introduced in Tutorial `06`

## Prerequisites

- Local stack is running (`dotnet run --project src/AppHost`)
- API reachable at `http://localhost:5010`

Set base URL:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}
```

## 1) Initialize a Text Anomaly Collection

```bash
curl -i -X POST "$API_BASE_URL/api/v1/admin/collections/init" \
  -H "Content-Type: application/json" \
  -d '{"collectionName":"knowledge_chunks_text_anomaly","vectorSize":384,"distance":"Cosine"}'
```

## 2) Ingest a Small Operational Baseline

```bash
curl -i -X POST "$API_BASE_URL/api/v1/ingest/markdown" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_text_anomaly",
    "docId":"ops-health-checks",
    "sourceId":"docs/ops-health.md",
    "title":"Operational Health Checks",
    "markdown":"# Operational Health Checks\n\nCheck the /health and /ready endpoints after startup.",
    "tags":["ops","health"],
    "tenantId":"tenant-a"
  }'
```

```bash
curl -i -X POST "$API_BASE_URL/api/v1/ingest/markdown" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_text_anomaly",
    "docId":"ops-runtime-status",
    "sourceId":"docs/runtime-status.md",
    "title":"Runtime Status",
    "markdown":"# Runtime Status\n\nVerify that AppHost starts cleanly and the API responds on /health and /ready.",
    "tags":["ops","runtime"],
    "tenantId":"tenant-a"
  }'
```

```bash
curl -i -X POST "$API_BASE_URL/api/v1/ingest/markdown" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_text_anomaly",
    "docId":"ops-readiness",
    "sourceId":"docs/readiness.md",
    "title":"Service Readiness",
    "markdown":"# Service Readiness\n\nConfirm readiness checks and local startup status before sending traffic.",
    "tags":["ops","readiness"],
    "tenantId":"tenant-a"
  }'
```

Poll the returned job ids as shown in Tutorial `03` until all jobs reach `Succeeded`.

## 3) Score a Similar Operational Text

```bash
curl -i -X POST "$API_BASE_URL/api/v1/anomaly/text/score" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_text_anomaly",
    "text":"Check the /health and /ready endpoints after startup.",
    "topK":1,
    "threshold":0.35,
    "includeDebug":true,
    "filter":{"tenantIdEquals":"tenant-a"}
  }'
```

Expected:

- `200 OK`
- low `anomalyScore`
- `isAnomalous:false`
- neighbor explanation in `neighbors`
- embedding metadata in `debug`

## 4) Score a Clearly Unrelated Text

```bash
curl -i -X POST "$API_BASE_URL/api/v1/anomaly/text/score" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_text_anomaly",
    "text":"Galaxies and nebulae drift through deep space far from health endpoint checks.",
    "topK":1,
    "threshold":0.35,
    "filter":{"tenantIdEquals":"tenant-a"}
  }'
```

Expected:

- `200 OK`
- higher `anomalyScore`
- `isAnomalous:true`

## 5) Missing Baseline Path

If no baseline neighbors are available after filtering, the endpoint returns `422 Unprocessable Entity`.

```bash
curl -i -X POST "$API_BASE_URL/api/v1/anomaly/text/score" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"knowledge_chunks_text_anomaly",
    "text":"The API should return 200 on /health and /ready after startup.",
    "topK":3,
    "filter":{"tenantIdEquals":"tenant-missing"}
  }'
```

Expected:

- `422 Unprocessable Entity`
- problem response title: `Insufficient anomaly baseline`

## Notes

- `07` does not introduce a second anomaly engine. It embeds text and then reuses the same anomaly-scoring core from Tutorial `06`.
- Local runs use the configured embedding provider. By default that remains the deterministic provider, so the workflow stays reproducible in tests and CI.
- Small text baselines are sensitive to `topK`. For a narrow baseline like this tutorial, `topK: 1` gives the clearest "nearest known text vs. clear outlier" demonstration.
- Later image and event/fraud anomaly slices should follow the same pattern: project their inputs into vectors and reuse the shared anomaly core.

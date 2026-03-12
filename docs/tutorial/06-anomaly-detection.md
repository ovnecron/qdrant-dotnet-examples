# Tutorial 06: Anomaly Detection

This tutorial covers the anomaly-detection foundation slice:

- initialize a vector collection for anomaly scoring
- upsert a small baseline cluster
- score a near-baseline point and a clear outlier
- inspect the returned anomaly score and nearest-neighbor explanation

This slice is intentionally vector-first. It is the common core for later:

- text anomaly detection
- image anomaly detection
- event / fraud anomaly detection

## Prerequisites

- Local stack is running (`dotnet run --project src/AppHost`)
- API reachable at `http://localhost:5010`

Set base URL:

```bash
API_BASE_URL=${API_BASE_URL:-http://localhost:5010}
```

## 1) Initialize an Anomaly Collection

```bash
curl -i -X POST "$API_BASE_URL/api/v1/admin/collections/init" \
  -H "Content-Type: application/json" \
  -d '{"collectionName":"anomaly_vectors","vectorSize":3,"distance":"Cosine"}'
```

## 2) Upsert a Small Baseline Cluster

```bash
curl -i -X POST "$API_BASE_URL/api/v1/vectors/upsert" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"anomaly_vectors",
    "points":[
      {
        "id":"baseline-1",
        "vector":[1.0,0.0,0.0],
        "payload":{
          "docId":"acct-42",
          "source":"fixtures/anomaly-baseline.json",
          "tags":["baseline","normal"],
          "content":"Baseline anomaly reference point.",
          "tenantId":"tenant-a"
        }
      },
      {
        "id":"baseline-2",
        "vector":[0.99,0.01,0.0],
        "payload":{
          "docId":"acct-42",
          "source":"fixtures/anomaly-baseline.json",
          "tags":["baseline","normal"],
          "content":"Baseline anomaly reference point.",
          "tenantId":"tenant-a"
        }
      },
      {
        "id":"baseline-3",
        "vector":[0.97,0.03,0.0],
        "payload":{
          "docId":"acct-42",
          "source":"fixtures/anomaly-baseline.json",
          "tags":["baseline","normal"],
          "content":"Baseline anomaly reference point.",
          "tenantId":"tenant-a"
        }
      }
    ]
  }'
```

## 3) Score a Near-Baseline Point

```bash
curl -i -X POST "$API_BASE_URL/api/v1/anomaly/score" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"anomaly_vectors",
    "vector":[0.98,0.02,0.0],
    "topK":3,
    "threshold":0.35,
    "filter":{"tenantIdEquals":"tenant-a"}
  }'
```

Expected:

- `200 OK`
- low `anomalyScore`
- `isAnomalous:false`
- neighbor explanation in `neighbors`

## 4) Score a Clear Outlier

```bash
curl -i -X POST "$API_BASE_URL/api/v1/anomaly/score" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"anomaly_vectors",
    "vector":[-1.0,0.0,0.0],
    "topK":3,
    "threshold":0.35,
    "filter":{"tenantIdEquals":"tenant-a"}
  }'
```

Expected:

- `200 OK`
- high `anomalyScore`
- `isAnomalous:true`

## 5) Missing Baseline Path

If no baseline neighbors are available after filtering, the endpoint returns `422 Unprocessable Entity`.

```bash
curl -i -X POST "$API_BASE_URL/api/v1/anomaly/score" \
  -H "Content-Type: application/json" \
  -d '{
    "collection":"anomaly_vectors",
    "vector":[0.98,0.02,0.0],
    "topK":3,
    "filter":{"tenantIdEquals":"tenant-missing"}
  }'
```

Expected:

- `422 Unprocessable Entity`
- problem response title: `Insufficient anomaly baseline`

## Notes

- `06` is deliberately generic and vector-based.
- Later text, image, and event/fraud anomaly slices should project their inputs into vectors and then reuse this same anomaly-scoring core.
- The current score is intentionally simple and explainable: it is derived from nearest-neighbor similarity in Qdrant, not from a heavier anomaly ensemble.

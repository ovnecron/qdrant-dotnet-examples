# Markdown Ingestion

Submit markdown documents with `POST /api/v1/ingest/markdown`.

The API returns `202 Accepted` and a `jobId`.

Poll `GET /api/v1/ingest/jobs/{jobId}` until the background job succeeds.

Re-ingesting the same `docId` replaces the active chunks for that `docId` and `tenantId`.

# Vector Search

Search with `POST /api/v1/vectors/search`.

Use `topK` to limit the number of returned hits.

Use `minScore` to remove weak matches.

Filters can include `tagsAny`, `sourceEquals`, `docIdEquals`, and `tenantIdEquals`.

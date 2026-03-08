# Collection Initialization

Create a collection with `POST /api/v1/admin/collections/init`.

Send `collectionName`, `vectorSize`, and `distance`.

The first successful request returns `201 Created`.

Repeating the same request returns `200 OK` because collection initialization is idempotent.

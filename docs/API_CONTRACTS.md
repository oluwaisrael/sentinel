# Week 1 API contracts

All request and response bodies are JSON. Validation failures return `400 Bad Request`; unauthenticated requests return `401`; a monitor that is not owned by the current user behaves as not found and returns `404`.

## Configuration

The API requires these external configuration values:

```text
ConnectionStrings__DefaultConnection
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey       # at least 32 characters; provide through a secret store
Jwt__AccessTokenMinutes
```

EF tooling uses `SENTINEL_CONNECTION_STRING` so migrations never need a password in source control.

## Authentication

### `POST /api/auth/register`

```json
{
  "email": "engineer@example.com",
  "password": "Strong-password-123!"
}
```

Returns `201 Created` with an access token. Identity enforces unique email, a 12-character minimum password, mixed character classes, and lockout after repeated failures.

### `POST /api/auth/login`

Accepts the same shape and returns `200 OK` with:

```json
{
  "accessToken": "<jwt>",
  "expiresAt": "2026-11-16T12:00:00Z"
}
```

Send the token as `Authorization: Bearer <jwt>`.

## Monitors

All monitor endpoints require authentication.

- `GET /api/monitors` — list the current user’s monitors.
- `GET /api/monitors/{id}` — retrieve one owned monitor.
- `POST /api/monitors` — create a monitor; returns `201 Created`.
- `PUT /api/monitors/{id}` — update an owned monitor.
- `DELETE /api/monitors/{id}` — delete an owned monitor; returns `204 No Content`.

Create/update body:

```json
{
  "name": "Public API",
  "url": "https://api.example.com/health",
  "intervalSeconds": 60,
  "timeoutMilliseconds": 5000,
  "isEnabled": true
}
```

The current URL policy accepts HTTP(S), rejects URL credentials, localhost, cloud metadata hostnames, loopback, private, link-local, and unspecified literal IP addresses. DNS-to-private-IP revalidation belongs to the Week 2 worker because it must happen immediately before each outbound request.


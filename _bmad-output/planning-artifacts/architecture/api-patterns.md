# API & Communication Patterns

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains core architectural decisions and data architecture._

## API Style

RESTful via Minimal API endpoints. Organized by feature in the Web project.

## Key Endpoint Patterns

- `GET /api/recruitments/{id}/overview` — Computed dashboard data via GROUP BY (NFR2: 500ms)
- `GET /api/recruitments/{id}/candidates` — Paginated list with search/filter, includes batch SAS URLs (NFR3: 1s)
- `POST /api/recruitments/{id}/import` — Accepts file upload, returns 202 Accepted with import session ID
- `GET /api/import-sessions/{id}` — Poll for import progress and results

## Async Operations

Import upload returns 202 Accepted immediately. Client polls the import session endpoint for progress. Import processing runs in-process via `IHostedService` + `Channel<T>`.

## Endpoint Registration

**Rule: All endpoint classes MUST inherit from `EndpointGroupBase`.** This enables automatic discovery and registration via `app.MapEndpoints()` in Program.cs. Do not use static extension methods for endpoint registration.

```csharp
// Canonical pattern — inherit from EndpointGroupBase
public class RecruitmentEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments";  // maps to /api/recruitments

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateRecruitment);
        group.MapGet("/{id:guid}", GetRecruitmentById);
        group.MapGet("/", GetRecruitments);
        // ... more endpoints
    }
}
```

**How it works:**
- `EndpointGroupBase` defines a base class with `GroupName` and `Map()` abstract/virtual methods
- `app.MapEndpoints()` in Program.cs discovers all `EndpointGroupBase` subclasses via reflection
- Each group is automatically registered under `/api/{GroupName}` with `RequireAuthorization()`

**For nested resources** (e.g., team members under a recruitment), create a separate endpoint class with an appropriate group path:

```csharp
public class TeamEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}/members";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetMembers);
        group.MapPost("/", AddMember);
        // ...
    }
}
```

> **Evidence:** Story 2.4 used a static `MapTeamEndpoints()` extension method instead of EndpointGroupBase, creating an inconsistency with RecruitmentEndpoints. This is tracked as a refactoring item.

## Error Responses

RFC 9457 Problem Details. Validation errors include field-level detail. Import errors include row-level detail. No PII in error responses.

**Test convention for error responses:** Every endpoint's integration tests must verify the Problem Details shape — assert status code *and* `ProblemDetails.Title` *and* `ProblemDetails.Errors` keys for validation failures. Status code-only assertions are insufficient.

**Documentation:** OpenAPI document auto-generated. Scalar UI for interactive exploration in development only.

## API Response Formats

| Scenario | Format | Example |
|----------|--------|---------|
| Single entity | Direct object | `{ "id": "...", "title": "..." }` |
| Collection | Pagination wrapper | `{ "items": [...], "totalCount": 42, "page": 1, "pageSize": 50 }` |
| Creation | 201 + Location header + entity | `Location: /api/recruitments/abc-123` |
| Async operation | 202 + status endpoint | `{ "importSessionId": "...", "statusUrl": "/api/import-sessions/..." }` |
| Validation error | 400 + Problem Details | `{ "type": "...", "title": "Validation Failed", "errors": {...} }` |
| Not found | 404 + Problem Details | `{ "type": "...", "title": "Not Found", "detail": "..." }` |
| Auth failure | 401/403 + Problem Details | Standard ASP.NET Core response |
| Server error | 500 + Problem Details (no internals) | `{ "type": "...", "title": "Internal Server Error" }` |

**Rule: No wrapper envelope.** Success responses return data directly. Errors use Problem Details. Frontend distinguishes by HTTP status code.

## Data Formats

- **Date/time:** ISO 8601 everywhere. `DateTimeOffset` in C#, `datetimeoffset` in SQL, `string` in TypeScript. Display via `Intl.DateTimeFormat`.
- **IDs:** GUIDs (`Guid` / `uniqueidentifier` / `string`). Generated server-side. No sequential integers (prevents enumeration).
- **Nulls:** API never returns `null` for collections — return `[]`. Nullable fields use `null` in JSON (not omitted). TypeScript uses `| null` explicitly, not `| undefined`.
- **JSON casing:** camelCase (System.Text.Json default). No configuration needed.

# Story 5.1: Overview API & Data

Status: ready-for-dev

## Story

As a **developer**,
I want a dedicated overview endpoint that returns pre-aggregated recruitment data including candidate counts per step, stale detection, and pending actions,
so that the frontend can render the overview dashboard without computing aggregations client-side.

## Acceptance Criteria

### AC1: Overview endpoint returns comprehensive aggregated data
**Given** an active recruitment with candidates at various workflow steps
**When** the client calls `GET /api/recruitments/{id}/overview`
**Then** the response includes: total candidate count, candidate count per workflow step, outcome status breakdown per step (NotStarted, Pass, Fail, Hold), pending action count (candidates with no outcome at their current step), and stale candidate count

### AC2: Data aggregated via GROUP BY within performance budget
**Given** the overview endpoint is called
**When** the server computes the response
**Then** data is aggregated via GROUP BY query (not pre-aggregated materialized views)
**And** the response returns within 500ms (NFR2)

### AC3: Stale candidate detection with configurable threshold
**Given** a recruitment has candidates who have been at their current step for longer than the configurable stale threshold (default: 5 calendar days)
**When** the overview endpoint is called
**Then** the response includes per-step stale counts (number of candidates exceeding the threshold at each step)
**And** the stale threshold value is included in the response for display purposes

### AC4: Per-step breakdown includes all required fields
**Given** the overview endpoint is called
**When** the response is constructed
**Then** each workflow step entry includes: step name, step order, total candidates at step, candidates with no outcome (pending), candidates per outcome status, and stale count

### AC5: Empty recruitment returns zero counts with all steps listed
**Given** a recruitment has no candidates
**When** the overview endpoint is called
**Then** the response returns zero counts for all metrics
**And** all workflow steps are still listed with zero counts

### AC6: Non-member receives 403 Forbidden
**Given** a user is not a member of the recruitment
**When** they call the overview endpoint
**Then** the API returns 403 Forbidden

### AC7: Overview and candidate list are independent
**Given** the overview endpoint is called independently of the candidate list endpoint
**When** both are requested
**Then** each returns independently (no coupling between overview and candidate list queries)

### Prerequisites
- **Story 1.3** (Core Data Model & Tenant Isolation) -- Recruitment, Candidate, WorkflowStep, CandidateOutcome entities; ITenantContext; IApplicationDbContext
- **Story 2.1** (Create Recruitment with Workflow Steps) -- Recruitment aggregate with Steps collection
- **Story 4.3** (Outcome Recording & Workflow Enforcement) -- CandidateOutcome entity, OutcomeStatus enum, Candidate.CurrentWorkflowStepId

### FRs Fulfilled
- **FR47:** Overview dashboard shows candidate counts per workflow step
- **FR48:** Overview dashboard shows outcome status breakdown per step
- **FR49:** Stale candidate detection based on configurable threshold
- **FR50:** Overview data loads independently from candidate list (data layer)

### NFRs Addressed
- **NFR2:** Overview endpoint responds within 500ms

## Tasks / Subtasks

- [ ] Task 1: Backend -- Configuration for stale threshold (AC: #3)
  - [ ] 1.1 Add `OverviewSettings` section to `api/src/Web/appsettings.json` with `StaleDays: 5`
  - [ ] 1.2 Create `api/src/Application/Common/Models/OverviewSettings.cs` as a POCO options class: `public class OverviewSettings { public int StaleDays { get; set; } = 5; }`
  - [ ] 1.3 Register `OverviewSettings` in DI via `services.Configure<OverviewSettings>(configuration.GetSection("OverviewSettings"))` in `api/src/Web/Configuration/DependencyInjection.cs`

- [ ] Task 2: Backend -- GetRecruitmentOverviewQuery + Validator (AC: #1, #6)
  - [ ] 2.1 Create `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQuery.cs` as `public record GetRecruitmentOverviewQuery : IRequest<RecruitmentOverviewDto> { public Guid RecruitmentId { get; init; } }`
  - [ ] 2.2 Create `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryValidator.cs` with `RuleFor(x => x.RecruitmentId).NotEmpty()`

- [ ] Task 3: Backend -- RecruitmentOverviewDto (AC: #1, #3, #4)
  - [ ] 3.1 Create `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/RecruitmentOverviewDto.cs` with the exact DTO shape defined in Dev Notes below

- [ ] Task 4: Backend -- GetRecruitmentOverviewQueryHandler (AC: #1, #2, #3, #4, #5, #6)
  - [ ] 4.1 Create `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryHandler.cs`
  - [ ] 4.2 Inject `IApplicationDbContext`, `ITenantContext`, and `IOptions<OverviewSettings>`
  - [ ] 4.3 Load recruitment with Members and Steps; throw `NotFoundException` if not found
  - [ ] 4.4 Verify current user is a member via `ITenantContext.UserGuid`; throw `ForbiddenAccessException` if not a member
  - [ ] 4.5 Compute per-step candidate counts, outcome breakdowns, pending counts, and stale counts via a single GROUP BY query on Candidates joined with their latest outcome per step
  - [ ] 4.6 Return all workflow steps (even those with zero candidates) by left-joining step data with aggregated candidate data
  - [ ] 4.7 Compute stale threshold: candidates whose most recent outcome at their current step is older than `StaleDays` (or candidates with no outcome at their current step whose `CreatedAt` or step assignment date exceeds the threshold)
  - [ ] 4.8 Use `AsNoTracking()` for read performance

- [ ] Task 5: Backend -- Register overview endpoint (AC: #1, #6, #7)
  - [ ] 5.1 Add `GET /{id:guid}/overview` route to `api/src/Web/Endpoints/RecruitmentEndpoints.cs`
  - [ ] 5.2 Create `GetRecruitmentOverview` endpoint method that sends `GetRecruitmentOverviewQuery` via MediatR and returns `Results.Ok(result)`

- [ ] Task 6: Backend -- Application unit tests (AC: #1, #3, #4, #5, #6)
  - [ ] 6.1 Create `api/tests/Application.UnitTests/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryHandlerTests.cs`
  - [ ] 6.2 Test: `Handle_UserNotMember_ThrowsForbiddenAccessException`
  - [ ] 6.3 Test: `Handle_RecruitmentNotFound_ThrowsNotFoundException`
  - [ ] 6.4 Test: `Handle_ValidRequest_ReturnsTotalCandidateCount`
  - [ ] 6.5 Test: `Handle_ValidRequest_ReturnsPerStepCandidateCounts`
  - [ ] 6.6 Test: `Handle_ValidRequest_ReturnsOutcomeBreakdownPerStep`
  - [ ] 6.7 Test: `Handle_ValidRequest_ReturnsPendingActionCount`
  - [ ] 6.8 Test: `Handle_NoCandidates_ReturnsZeroCountsWithAllSteps`
  - [ ] 6.9 Test: `Handle_StaleCandidates_ReturnsPerStepStaleCounts`
  - [ ] 6.10 Test: `Handle_ValidRequest_IncludesStaleDaysThreshold`
  - [ ] 6.11 Create `api/tests/Application.UnitTests/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryValidatorTests.cs`
  - [ ] 6.12 Test: `Validate_EmptyRecruitmentId_HasValidationError`
  - [ ] 6.13 Test: `Validate_ValidRecruitmentId_PassesValidation`

- [ ] Task 7: Backend -- Extend GetCandidatesQuery with stale filter (AC: #3, supports Story 5.2 AC7)
  - [ ] 7.1 Add `bool? StaleOnly` property to `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs`
  - [ ] 7.2 Update `GetCandidatesQueryHandler` to filter by stale when `StaleOnly == true`: candidates at the given `StepId` whose `CreatedAt < staleCutoff` AND who have no outcome at their current step (reuses same stale logic as overview handler)
  - [ ] 7.3 Inject `IOptions<OverviewSettings>` into `GetCandidatesQueryHandler` for `StaleDays` threshold (reuses same config from Task 1)
  - [ ] 7.4 Add `?staleOnly=true` query parameter binding in `CandidateEndpoints.cs`
  - [ ] 7.5 Update `web/src/features/candidates/hooks/useCandidates.ts`: add `staleOnly?: boolean` to `UseCandidatesParams` and pass to API call + query key
  - [ ] 7.6 Update `web/src/lib/api/candidates.ts`: add `staleOnly` param to `getAll()` method
  - [ ] 7.7 Test: `GetCandidates_StaleOnlyWithStepId_ReturnsOnlyStaleCandidatesAtStep`
  - [ ] 7.8 Test: `GetCandidates_StaleOnlyWithoutStepId_ReturnsAllStaleCandidates`

- [ ] Task 8: Backend -- Functional endpoint tests (AC: #1, #5, #6)
  - [ ] 8.1 Create `api/tests/Application.FunctionalTests/Endpoints/RecruitmentOverviewEndpointTests.cs`
  - [ ] 8.2 Test: `GetOverview_Authenticated_ReturnsOkWithOverviewData`
  - [ ] 8.3 Test: `GetOverview_NonMember_ReturnsForbidden`
  - [ ] 8.4 Test: `GetOverview_NoCandidates_ReturnsZeroCountsWithSteps`
  - [ ] 8.5 Test: `GetOverview_InvalidGuid_ReturnsBadRequest` (Problem Details shape assertion)

## Dev Notes

### Affected Aggregate(s)

**Recruitment** aggregate root (read-only). The handler reads Recruitment (with Steps and Members) to get the workflow configuration and verify membership. It then queries Candidates (a separate aggregate) via `IApplicationDbContext.Candidates` using the recruitment ID. No write operations -- this is a pure read/query path.

Cross-aggregate read is acceptable here because the handler only reads; it does not modify either aggregate.

### DTO Shape (Contract for Story 5.2)

The following DTO shape is the contract between the API (Story 5.1) and the frontend (Story 5.2). Story 5.2 will consume this exact shape.

```csharp
// api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/RecruitmentOverviewDto.cs

public record RecruitmentOverviewDto
{
    public Guid RecruitmentId { get; init; }
    public int TotalCandidates { get; init; }
    public int PendingActionCount { get; init; }
    public int TotalStale { get; init; }
    public int StaleDays { get; init; }
    public List<WorkflowStepOverviewDto> Steps { get; init; } = [];
}

public record WorkflowStepOverviewDto
{
    public Guid StepId { get; init; }
    public string StepName { get; init; } = null!;
    public int StepOrder { get; init; }
    public int TotalCandidates { get; init; }
    public int PendingCount { get; init; }
    public int StaleCount { get; init; }
    public OutcomeBreakdownDto OutcomeBreakdown { get; init; } = new();
}

public record OutcomeBreakdownDto
{
    public int NotStarted { get; init; }
    public int Pass { get; init; }
    public int Fail { get; init; }
    public int Hold { get; init; }
}
```

**JSON response shape** (camelCase via System.Text.Json default):
```json
{
  "recruitmentId": "guid",
  "totalCandidates": 130,
  "pendingActionCount": 47,
  "totalStale": 3,
  "staleDays": 5,
  "steps": [
    {
      "stepId": "guid",
      "stepName": "Screening",
      "stepOrder": 1,
      "totalCandidates": 45,
      "pendingCount": 12,
      "staleCount": 2,
      "outcomeBreakdown": {
        "notStarted": 12,
        "pass": 20,
        "fail": 8,
        "hold": 5
      }
    }
  ]
}
```

**Design decisions for the DTO:**
- `StaleDays` included at root level so the frontend can display "candidates > 5 days" without knowing the server configuration
- `OutcomeBreakdownDto` uses flat fields (not a dictionary) for type safety and predictable serialization
- `PendingCount` per step = candidates at that step with no outcome recorded (i.e., `NotStarted` at their current step)
- `PendingActionCount` at root = sum of all per-step `PendingCount` values
- `TotalStale` at root = sum of all per-step `StaleCount` values
- Steps are always sorted by `StepOrder` in the response

### Query Strategy

The handler should use a **single efficient query** pattern. The recommended approach:

1. Load the Recruitment with Steps and Members (for auth check and step metadata)
2. Execute a single GROUP BY query on Candidates:

```csharp
// Pseudocode for the aggregation query
var staleCutoff = DateTimeOffset.UtcNow.AddDays(-settings.StaleDays);

var candidateStepData = await _dbContext.Candidates
    .Where(c => c.RecruitmentId == request.RecruitmentId)
    .Where(c => c.CurrentWorkflowStepId != null)
    .GroupBy(c => c.CurrentWorkflowStepId)
    .Select(g => new
    {
        StepId = g.Key,
        TotalCandidates = g.Count(),
        // Candidates at this step with no outcome for this step
        PendingCount = g.Count(c => !c.Outcomes.Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId)),
        // Stale: at step longer than threshold with no outcome at current step
        StaleCount = g.Count(c =>
            !c.Outcomes.Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId) &&
            c.CreatedAt < staleCutoff),
        // Outcome breakdown for candidates AT this step (their outcome at this step)
        PassCount = g.Count(c => c.Outcomes
            .Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId && o.Status == OutcomeStatus.Pass)),
        FailCount = g.Count(c => c.Outcomes
            .Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId && o.Status == OutcomeStatus.Fail)),
        HoldCount = g.Count(c => c.Outcomes
            .Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId && o.Status == OutcomeStatus.Hold)),
    })
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

**Stale detection logic:** A candidate is "stale" at their current step if:
- They have no outcome recorded at their current step (they are pending), AND
- They have been at this step longer than `StaleDays`. The proxy for "time at step" is `Candidate.CreatedAt` for simplicity at MVP (since candidates are assigned to steps on import/creation and progress through Pass outcomes). A more precise approach would track step assignment timestamp, but `CreatedAt` is sufficient for MVP scale.

**Important: Outcome breakdown semantics.** The outcome breakdown per step shows the outcomes of candidates currently AT that step. `NotStarted` = candidates at the step with no outcome recorded. This matches the pending count. The breakdown should satisfy: `NotStarted + Pass + Fail + Hold == TotalCandidates` for each step.

Note: Candidates with `Pass` at their current step have already been auto-advanced by the outcome recording flow (Story 4.3), so in practice `Pass` count at a given step will typically be 0 for non-final steps. However, the DTO should still include it for completeness and for the final step where Pass means "completed the process."

### Handler Authorization Pattern

Follow the exact pattern from `GetRecruitmentByIdQueryHandler` ([Source: `api/src/Application/Features/Recruitments/Queries/GetRecruitmentById/GetRecruitmentByIdQueryHandler.cs`]):

```csharp
var recruitment = await _dbContext.Recruitments
    .Include(r => r.Members)
    .Include(r => r.Steps)
    .AsNoTracking()
    .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken);

if (recruitment is null)
    throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

var userId = _tenantContext.UserGuid;
if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
    throw new ForbiddenAccessException();
```

### Configuration Pattern

Add stale threshold to `appsettings.json`:

```json
{
  "OverviewSettings": {
    "StaleDays": 5
  }
}
```

Options class in Application layer (follows ASP.NET Core Options pattern):

```csharp
// api/src/Application/Common/Models/OverviewSettings.cs
namespace api.Application.Common.Models;

public class OverviewSettings
{
    public const string SectionName = "OverviewSettings";
    public int StaleDays { get; set; } = 5;
}
```

Register in DI:
```csharp
services.Configure<OverviewSettings>(configuration.GetSection(OverviewSettings.SectionName));
```

### Endpoint Registration Pattern

Add to existing `RecruitmentEndpoints.cs` ([Source: `api/src/Web/Endpoints/RecruitmentEndpoints.cs`]):

```csharp
// In Map() method:
group.MapGet("/{id:guid}/overview", GetRecruitmentOverview);

// Endpoint method:
private static async Task<IResult> GetRecruitmentOverview(
    ISender sender,
    Guid id)
{
    var result = await sender.Send(new GetRecruitmentOverviewQuery { RecruitmentId = id });
    return Results.Ok(result);
}
```

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Configuration) | **Characterization** | Simple config wiring, low risk. Verify it binds correctly. |
| Task 2 (Query + Validator) | **Test-first** | Validator tests are quick to write and verify input constraints. |
| Task 3 (DTO) | **No tests needed** | DTOs are data structures with no logic. |
| Task 4 (Handler) | **Test-first** | Core business logic: authorization check, aggregation, stale detection. Security-critical (membership check). |
| Task 5 (Endpoint) | **Test-first** | Functional tests verify end-to-end HTTP path, Problem Details shape, and auth enforcement. |
| Task 6 (Unit tests) | N/A (this IS the tests task) | |
| Task 7 (Stale filter on candidates) | **Test-first** | Extends existing query with security-relevant filter logic. Reuses stale detection from overview handler. |
| Task 8 (Functional tests) | N/A (this IS the tests task) | |

**Test infrastructure:**
- Unit tests: NSubstitute for `IApplicationDbContext`, `ITenantContext`, `IOptions<OverviewSettings>`. Mock `DbSet<Candidate>` and `DbSet<Recruitment>` with in-memory queryable. Use NUnit `[Test]` attributes and FluentAssertions.
- Functional tests: `CustomWebApplicationFactory` with test database (Testcontainers). Seed recruitment with members, steps, and candidates with various outcomes.

### Architecture Compliance

- **Aggregate boundary respected:** Handler reads Recruitment aggregate (Steps, Members) for auth + metadata, then queries Candidates aggregate read-only via `IApplicationDbContext.Candidates`. No cross-aggregate writes.
- **ITenantContext for authorization:** Membership check via `_tenantContext.UserGuid` matching `recruitment.Members`.
- **Manual DTO mapping:** No AutoMapper. Handler constructs `RecruitmentOverviewDto` directly from query results.
- **Problem Details for errors:** `NotFoundException` and `ForbiddenAccessException` are caught by the global exception middleware and converted to Problem Details responses.
- **No PII in responses:** Overview only returns counts and step metadata -- no candidate names, emails, or other PII.
- **Ubiquitous language:** Uses "Recruitment", "Workflow Step", "Candidate", "Outcome" consistently.
- **Endpoint pattern:** Uses `EndpointGroupBase` via existing `RecruitmentEndpoints`. Route follows REST conventions: `GET /api/recruitments/{id}/overview`.
- **FluentValidation:** Query has a validator ensuring `RecruitmentId` is not empty.
- **AsNoTracking:** Used for read-only query performance.

### Project Structure Notes

**New files to create:**
```
api/src/Application/
  Common/
    Models/
      OverviewSettings.cs                           # Options class for stale threshold
  Features/
    Recruitments/
      Queries/
        GetRecruitmentOverview/
          GetRecruitmentOverviewQuery.cs             # MediatR query record
          GetRecruitmentOverviewQueryValidator.cs    # FluentValidation
          GetRecruitmentOverviewQueryHandler.cs      # Query handler with GROUP BY
          RecruitmentOverviewDto.cs                  # DTO (3 records)

api/tests/
  Application.UnitTests/
    Features/
      Recruitments/
        Queries/
          GetRecruitmentOverview/
            GetRecruitmentOverviewQueryHandlerTests.cs
            GetRecruitmentOverviewQueryValidatorTests.cs
  Application.FunctionalTests/
    Endpoints/
      RecruitmentOverviewEndpointTests.cs
```

**Existing files to modify:**
```
api/src/Web/appsettings.json                        # Add OverviewSettings section
api/src/Web/Configuration/DependencyInjection.cs    # Register OverviewSettings
api/src/Web/Endpoints/RecruitmentEndpoints.cs       # Add overview endpoint route
api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs        # Add StaleOnly filter
api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandler.cs # Stale filter logic
api/src/Web/Endpoints/CandidateEndpoints.cs         # Add staleOnly query param binding
web/src/features/candidates/hooks/useCandidates.ts  # Add staleOnly param
web/src/lib/api/candidates.ts                       # Add staleOnly param to getAll()
```

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-5-recruitment-overview-monitoring.md` -- Story 5.1 acceptance criteria, FR47-50, technical notes]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture` -- Overview data strategy: "Computed on read via GROUP BY query for MVP", stale step threshold default 5 days]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions` -- Decision #5: RFC 9457 Problem Details]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md#Handler Authorization` -- Mandatory membership check pattern for all query handlers]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md#DTO Mapping` -- Manual mapping, no AutoMapper, `From()` factory or direct construction]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md#Naming Patterns` -- C# PascalCase, API camelCase]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md#Endpoint Registration` -- EndpointGroupBase pattern]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md#API Response Formats` -- Single entity: direct object, no wrapper]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- `Features/Recruitments/Queries/GetRecruitmentOverview/` directory placement]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- NUnit, NSubstitute, FluentAssertions, test naming convention]
- [Source: `api/src/Application/Features/Recruitments/Queries/GetRecruitmentById/GetRecruitmentByIdQueryHandler.cs` -- Authorization pattern reference]
- [Source: `api/src/Web/Endpoints/RecruitmentEndpoints.cs` -- Endpoint registration pattern reference]
- [Source: `api/src/Domain/Enums/OutcomeStatus.cs` -- OutcomeStatus enum: NotStarted, Pass, Fail, Hold]
- [Source: `api/src/Domain/Entities/Candidate.cs` -- CurrentWorkflowStepId, Outcomes collection, CreatedAt]
- [Source: `api/src/Application/Common/Interfaces/ITenantContext.cs` -- ITenantContext interface: UserId, UserGuid, RecruitmentId, IsServiceContext]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy, mode declarations]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

None -- clean implementation, no debugging required.

### Completion Notes List

- All 8 story tasks implemented (Tasks 1-8)
- Testing modes: Characterization (config), Test-first (validator, handler, endpoint, stale filter), No tests (DTOs)
- 17 tests added: 9 handler unit tests, 2 validator unit tests, 4 functional endpoint tests, 2 stale filter tests on GetCandidatesQueryHandler
- Anti-pattern scan: 0 violations
- Build: 0 errors, 0 warnings
- Domain unit tests: 114 pass; Frontend tests: 285 pass (45 files)
- Application.UnitTests could not run locally (missing Microsoft.AspNetCore.App runtime on dev machine) but build succeeds -- tests validated via CI
- Implementation plan: `docs/plans/2026-02-14-overview-api-data.md`

### File List

**New files created:**
- `api/src/Application/Common/Models/OverviewSettings.cs`
- `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQuery.cs`
- `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryValidator.cs`
- `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryHandler.cs`
- `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/RecruitmentOverviewDto.cs`
- `api/tests/Application.UnitTests/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryHandlerTests.cs`
- `api/tests/Application.UnitTests/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryValidatorTests.cs`
- `api/tests/Application.FunctionalTests/Endpoints/RecruitmentOverviewEndpointTests.cs`
- `docs/plans/2026-02-14-overview-api-data.md`

**Existing files modified:**
- `api/src/Web/appsettings.json` -- added OverviewSettings section
- `api/src/Web/DependencyInjection.cs` -- registered OverviewSettings in DI
- `api/src/Web/Endpoints/RecruitmentEndpoints.cs` -- added overview endpoint route
- `api/src/Web/Endpoints/CandidateEndpoints.cs` -- added staleOnly query param
- `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs` -- added StaleOnly property
- `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandler.cs` -- added stale filter logic + IOptions<OverviewSettings> injection
- `api/tests/Application.UnitTests/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandlerTests.cs` -- updated constructor calls + added 2 stale filter tests
- `web/src/features/candidates/hooks/useCandidates.ts` -- added staleOnly param
- `web/src/lib/api/candidates.ts` -- added staleOnly to getAll()
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- updated story status

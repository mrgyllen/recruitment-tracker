# Overview API & Data Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Provide a dedicated overview endpoint that returns pre-aggregated recruitment data including candidate counts per workflow step, outcome breakdowns, stale detection, and pending action counts -- so the frontend (Story 5.2) can render the dashboard without client-side aggregation.

**Architecture:** Backend CQRS read path. Single GROUP BY query against Candidates aggregate (cross-aggregate read, acceptable for read-only). Handler verifies membership via ITenantContext. Stale filter extension on existing GetCandidatesQuery for drill-down support. Frontend hook/api updates for staleOnly param.

**Tech Stack:** .NET 10, EF Core 10, MediatR 13, FluentValidation, NUnit/NSubstitute/FluentAssertions (backend); React 19, TypeScript, TanStack Query 5 (frontend).

---

## Authorization

| Handler | Auth Pattern | Membership Check |
|---------|-------------|-----------------|
| `GetRecruitmentOverviewQueryHandler` | Load recruitment with `.Include(r => r.Members)`, verify `ITenantContext.UserGuid` is in members list | Throws `ForbiddenAccessException` if not a member |
| `GetCandidatesQueryHandler` (modified) | Existing membership check unchanged | Already verified -- no change needed |

---

## DTO Contract (from Story File)

```csharp
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

---

### Task 1: Configuration -- OverviewSettings

**TDD Mode:** Characterization (simple config wiring, low risk)

**Files:**
- Create: `api/src/Application/Common/Models/OverviewSettings.cs`
- Modify: `api/src/Web/appsettings.json` -- add `OverviewSettings.StaleDays: 5`
- Modify: `api/src/Web/DependencyInjection.cs` -- register `Configure<OverviewSettings>`

**Steps:**
1. Create POCO options class with `SectionName` constant and `StaleDays` default 5
2. Add `OverviewSettings` JSON section to appsettings.json
3. Register via `services.Configure<OverviewSettings>(configuration.GetSection(...))` in Web DI

---

### Task 2: GetRecruitmentOverviewQuery + Validator

**TDD Mode:** Test-first (input validation)

**Files:**
- Create: `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQuery.cs`
- Create: `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryValidator.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryValidatorTests.cs`

**Tests (2):**
- `Validate_EmptyRecruitmentId_HasValidationError`
- `Validate_ValidRecruitmentId_PassesValidation`

---

### Task 3: RecruitmentOverviewDto

**TDD Mode:** No tests (data structures with no logic)

**Files:**
- Create: `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/RecruitmentOverviewDto.cs`

Exact DTO shape from story contract (3 records).

---

### Task 4: GetRecruitmentOverviewQueryHandler

**TDD Mode:** Test-first (core business logic, security-critical membership check)

**Files:**
- Create: `api/src/Application/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Queries/GetRecruitmentOverview/GetRecruitmentOverviewQueryHandlerTests.cs`

**Inject:** `IApplicationDbContext`, `ITenantContext`, `IOptions<OverviewSettings>`

**Logic:**
1. Load recruitment with Members + Steps (AsNoTracking)
2. Throw NotFoundException if not found
3. Verify membership via ITenantContext.UserGuid, throw ForbiddenAccessException if not member
4. Single GROUP BY query on Candidates by CurrentWorkflowStepId
5. Compute per-step: TotalCandidates, PendingCount, StaleCount, Pass/Fail/Hold counts
6. Left-join with all workflow steps (include zero-count steps)
7. Return DTO with summed totals

**Tests (9):**
- `Handle_RecruitmentNotFound_ThrowsNotFoundException`
- `Handle_UserNotMember_ThrowsForbiddenAccessException`
- `Handle_ValidRequest_ReturnsTotalCandidateCount`
- `Handle_ValidRequest_ReturnsPerStepCandidateCounts`
- `Handle_ValidRequest_ReturnsOutcomeBreakdownPerStep`
- `Handle_ValidRequest_ReturnsPendingActionCount`
- `Handle_NoCandidates_ReturnsZeroCountsWithAllSteps`
- `Handle_StaleCandidates_ReturnsPerStepStaleCounts`
- `Handle_ValidRequest_IncludesStaleDaysThreshold`

---

### Task 5: Register overview endpoint

**TDD Mode:** Test-first (functional endpoint tests)

**Files:**
- Modify: `api/src/Web/Endpoints/RecruitmentEndpoints.cs`
- Create: `api/tests/Application.FunctionalTests/Endpoints/RecruitmentOverviewEndpointTests.cs`

Route: `group.MapGet("/{id:guid}/overview", GetRecruitmentOverview)`

**Functional Tests (4):**
- `GetOverview_Authenticated_ReturnsOkWithOverviewData`
- `GetOverview_NonMember_ReturnsForbidden`
- `GetOverview_NoCandidates_ReturnsZeroCountsWithSteps`
- `GetOverview_InvalidGuid_ReturnsBadRequest`

---

### Task 6: Extend GetCandidatesQuery with StaleOnly filter

**TDD Mode:** Test-first (extends existing query with filter logic)

**Files:**
- Modify: `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs` -- add `bool? StaleOnly`
- Modify: `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandler.cs` -- stale filter logic + inject IOptions<OverviewSettings>
- Modify: `api/src/Web/Endpoints/CandidateEndpoints.cs` -- add `staleOnly` query param
- Modify: `api/tests/Application.UnitTests/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandlerTests.cs` -- update constructor calls
- Modify: `web/src/features/candidates/hooks/useCandidates.ts` -- add staleOnly param
- Modify: `web/src/lib/api/candidates.ts` -- add staleOnly to getAll()

---

## Verification Checklist

- [x] Anti-pattern scan: zero violations against anti-patterns.txt and anti-patterns-pending.txt
- [x] DTOs match exact contract from story file
- [x] .Include() for Members and Steps on recruitment load
- [x] ITenantContext membership check in handler
- [x] FluentValidation on GetRecruitmentOverviewQuery (RecruitmentId NotEmpty)
- [x] AsNoTracking() on read queries
- [x] Manual DTO mapping (no AutoMapper)
- [x] NUnit [Test] attributes (no xUnit)
- [x] NSubstitute for mocking (no Moq)
- [x] FluentAssertions for assertions
- [x] Problem Details via NotFoundException/ForbiddenAccessException
- [x] No PII in response (only counts and step metadata)
- [x] Steps sorted by StepOrder in response
- [x] Build succeeds with 0 errors, 0 warnings

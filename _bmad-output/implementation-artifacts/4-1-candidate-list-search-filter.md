# Story 4.1: Candidate List & Search/Filter

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **user**,
I want to **view a paginated list of candidates within a recruitment and search or filter them by name, email, step, or outcome status**,
so that **I can quickly find specific candidates or focus on candidates at a particular stage**.

## Acceptance Criteria

### AC1: Candidate list display
**Given** an active recruitment has candidates
**When** the user navigates to the recruitment view
**Then** a candidate list is displayed showing each candidate's name, email, current workflow step, and outcome status
**And** results are paginated (up to 50 per page)
**And** the list loads within 1 second (NFR3)

### AC2: Search by name or email
**Given** the candidate list is displayed
**When** the user types in the search field
**Then** the list filters to candidates matching by name or email (case-insensitive, substring match)
**And** search is debounced to avoid excessive API calls

### AC3: Filter by workflow step
**Given** the candidate list is displayed
**When** the user selects a workflow step filter
**Then** only candidates currently at that step are shown

### AC4: Filter by outcome status
**Given** the candidate list is displayed
**When** the user selects an outcome status filter (Not Started, Pass, Fail, Hold)
**Then** only candidates with that outcome at their current step are shown

### AC5: Combined filters
**Given** the user applies both a step filter and an outcome filter
**When** the list updates
**Then** both filters are applied together (AND logic)
**And** the active filters are visually indicated and individually clearable

### AC6: Pagination with filters
**Given** the candidate list has more than 50 candidates matching the current filters
**When** the user views the list
**Then** pagination controls are displayed
**And** navigating between pages maintains the active search and filter state

### AC7: Empty state
**Given** a recruitment has no candidates
**When** the user views the candidate list
**Then** an empty state is shown with guidance: "No candidates yet" and actions for "Import from Workday" and "Add Candidate"

### AC8: Virtualized list rendering
**Given** the candidate list has 130+ candidates
**When** the user scrolls the list
**Then** the list renders efficiently using virtualization (react-virtuoso) to keep the DOM light

### AC9: Candidate detail view
**Given** the user clicks a candidate in the list
**When** the candidate is selected
**Then** the candidate's complete profile is displayed: imported data (name, email, phone, location, date applied), linked documents, and outcome history across all completed steps

### Prerequisites
- **Story 3.1** (Manual Candidate Management) -- Candidate aggregate CRUD, CandidateEndpoints, CandidateList.tsx, CandidateDetail.tsx
- **Story 3.5** (CV Auto-Match, Manual Assignment & Individual Upload) -- CandidateDto with Document, CandidateDetail with DocumentUpload
- **Epic 2** -- Recruitment with WorkflowSteps, RecruitmentEndpoints

### FRs Fulfilled
- **FR36:** Users can view which workflow step each candidate is currently at
- **FR45:** Users can search candidates within a recruitment by name or email
- **FR46:** Users can filter candidates within a recruitment by current step and outcome status
- **FR51:** Users can view a candidate's complete profile including imported data, documents, and outcome history across all steps

## Tasks / Subtasks

- [ ] Task 1: Backend -- Extend GetCandidatesQuery with search/filter params (AC: #1, #2, #3, #4, #5, #6)
  - [ ] 1.1 Add `Search` (string?), `StepId` (Guid?), `OutcomeStatus` (OutcomeStatus?) parameters to `GetCandidatesQuery` in `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs`
  - [ ] 1.2 Update `GetCandidatesQueryHandler` to apply search filter: case-insensitive substring match on `FullName` or `Email` using `EF.Functions.Like()` or `Contains()` with `StringComparison.OrdinalIgnoreCase`
  - [ ] 1.3 Update handler to apply step filter: join with `CandidateOutcome` to find candidates whose most recent outcome's `WorkflowStepId` matches the requested step, or candidates with no outcomes who are at the first step
  - [ ] 1.4 Update handler to apply outcome status filter: filter by outcome status at the candidate's current step
  - [ ] 1.5 Ensure both filters combine with AND logic when both are provided
  - [ ] 1.6 Ensure `TotalCount` reflects the filtered count (not total unfiltered)
  - [ ] 1.7 Unit test: `Handle_WithSearchTerm_FiltersByNameSubstring`
  - [ ] 1.8 Unit test: `Handle_WithSearchTerm_FiltersByEmailSubstring`
  - [ ] 1.9 Unit test: `Handle_WithStepIdFilter_ReturnsOnlyCandidatesAtStep`
  - [ ] 1.10 Unit test: `Handle_WithOutcomeStatusFilter_ReturnsOnlyCandidatesWithStatus`
  - [ ] 1.11 Unit test: `Handle_WithCombinedFilters_AppliesAndLogic`
  - [ ] 1.12 Unit test: `Handle_WithSearchAndFilters_PaginationReflectsFilteredCount`

- [ ] Task 2: Backend -- Extend CandidateDto with current step and outcome info (AC: #1)
  - [ ] 2.1 Add `CurrentWorkflowStepId` (Guid?), `CurrentWorkflowStepName` (string?), and `CurrentOutcomeStatus` (string?) properties to `CandidateDto` in `api/src/Application/Features/Candidates/CandidateDto.cs`
  - [ ] 2.2 Update `CandidateDto.From()` to compute current step: the step corresponding to the latest outcome (or first step if no outcomes exist). This requires passing the recruitment's workflow steps into the mapping function
  - [ ] 2.3 Update `CandidateDto.From()` to compute current outcome status: the outcome status at the current step (or "NotStarted" if no outcome exists at that step)
  - [ ] 2.4 Update `GetCandidatesQueryHandler` to include workflow steps when building DTOs (load `recruitment.Steps` ordered by `Order`)
  - [ ] 2.5 Unit test: `From_CandidateWithNoOutcomes_CurrentStepIsFirstStep`
  - [ ] 2.6 Unit test: `From_CandidateWithPassOutcome_CurrentStepIsNextStep`
  - [ ] 2.7 Unit test: `From_CandidateAtLastStepWithPass_CurrentStepIsLastStep`
  - [ ] 2.8 Unit test: `From_CandidateWithFailOutcome_CurrentStepIsFailedStep`

- [ ] Task 3: Backend -- GetCandidateByIdQuery for detail view (AC: #9)
  - [ ] 3.1 Create `GetCandidateByIdQuery` record in `api/src/Application/Features/Candidates/Queries/GetCandidateById/` with `RecruitmentId` and `CandidateId` params
  - [ ] 3.2 Create `CandidateDetailDto` with all candidate fields plus `OutcomeHistory` (list of outcome records with step name, status, reason, recorded by, recorded at) and `Documents` (list with SAS URLs)
  - [ ] 3.3 Create `GetCandidateByIdQueryHandler`:
    - Load recruitment with members, verify user is member (`ITenantContext.UserGuid`)
    - Load candidate with outcomes and documents
    - Generate SAS URL for each document via `IBlobStorageService.GenerateSasUri()`
    - Map to `CandidateDetailDto` including outcome history with step names
  - [ ] 3.4 Unit test: `Handle_ValidRequest_ReturnsCandidateWithOutcomeHistory`
  - [ ] 3.5 Unit test: `Handle_NonMemberUser_ThrowsForbiddenAccessException`
  - [ ] 3.6 Unit test: `Handle_CandidateNotFound_ThrowsNotFoundException`
  - [ ] 3.7 Unit test: `Handle_CandidateWithDocuments_IncludesSasUrls`

- [ ] Task 4: Backend -- API endpoint for GetCandidateById (AC: #9)
  - [ ] 4.1 Add `GET /{candidateId:guid}` to `CandidateEndpoints.cs` mapped to `GetCandidateByIdQuery`
  - [ ] 4.2 Update existing `GetCandidates` endpoint to accept `search`, `stepId`, and `outcomeStatus` query parameters
  - [ ] 4.3 Integration test: `GetCandidateById_ValidId_Returns200WithDetail`
  - [ ] 4.4 Integration test: `GetCandidateById_NonMember_Returns403`
  - [ ] 4.5 Integration test: `GetCandidates_WithSearch_ReturnsFilteredResults`
  - [ ] 4.6 Integration test: `GetCandidates_WithStepFilter_ReturnsFilteredResults`

- [ ] Task 5: Frontend -- API client and types (AC: #1, #2, #3, #4, #9)
  - [ ] 5.1 Extend `CandidateResponse` in `web/src/lib/api/candidates.types.ts` with `currentWorkflowStepId`, `currentWorkflowStepName`, `currentOutcomeStatus` fields
  - [ ] 5.2 Add `CandidateDetailResponse` type with full candidate data, outcome history list, and documents with SAS URLs
  - [ ] 5.3 Add `OutcomeHistoryEntry` type with `stepId`, `stepName`, `status`, `reason`, `recordedBy`, `recordedAt`
  - [ ] 5.4 Update `candidateApi.getAll()` in `web/src/lib/api/candidates.ts` to accept optional `search`, `stepId`, `outcomeStatus` params and include them in the query string
  - [ ] 5.5 Add `candidateApi.getById()` method for fetching single candidate detail

- [ ] Task 6: Frontend -- Search and filter hooks (AC: #2, #3, #4, #5, #6)
  - [ ] 6.1 Update `useCandidates` hook in `web/src/features/candidates/hooks/useCandidates.ts` to accept `search`, `stepId`, `outcomeStatus`, and `page` params
  - [ ] 6.2 Include all filter params in the TanStack Query `queryKey` so cache is per-filter-combination
  - [ ] 6.3 Create `useDebounce` hook in `web/src/hooks/useDebounce.ts` (or verify it exists) for debouncing search input (300ms)
  - [ ] 6.4 Create `useCandidateById` hook in `web/src/features/candidates/hooks/useCandidateById.ts` for fetching single candidate

- [ ] Task 7: Frontend -- Search and filter controls UI (AC: #2, #3, #4, #5)
  - [ ] 7.1 Add search input field to `CandidateList.tsx` with placeholder "Search by name or email..."
  - [ ] 7.2 Add workflow step filter dropdown using shadcn/ui `Select` component, populated from the recruitment's workflow steps
  - [ ] 7.3 Add outcome status filter dropdown with options: Not Started, Pass, Fail, Hold
  - [ ] 7.4 Add active filter badges that are individually clearable (click X to remove a filter)
  - [ ] 7.5 Wire search input to debounced state that triggers `useCandidates` refetch
  - [ ] 7.6 Wire filter dropdowns to state that triggers `useCandidates` refetch
  - [ ] 7.7 Unit test: "should display search input and filter controls"
  - [ ] 7.8 Unit test: "should filter candidates when search term is entered"
  - [ ] 7.9 Unit test: "should filter candidates when step filter is selected"
  - [ ] 7.10 Unit test: "should show active filter badges that are clearable"
  - [ ] 7.11 Unit test: "should maintain filters when navigating between pages"

- [ ] Task 8: Frontend -- Enhanced candidate list display (AC: #1, #8)
  - [ ] 8.1 Update `CandidateList.tsx` to display current workflow step name and outcome status for each candidate row using `StatusBadge` component
  - [ ] 8.2 Install react-virtuoso: `npm install react-virtuoso`
  - [ ] 8.3 Replace static list rendering with `Virtuoso` component from react-virtuoso for virtualized scrolling when list has 50+ items
  - [ ] 8.4 Ensure candidate row layout accommodates step name, outcome status badge, and document indicator
  - [ ] 8.5 Unit test: "should display workflow step and outcome status for each candidate"
  - [ ] 8.6 Unit test: "should render empty state with import and add actions when no candidates"

- [ ] Task 9: Frontend -- Pagination controls (AC: #6)
  - [ ] 9.1 Add `PaginationControls` component to `CandidateList.tsx` (use shared component from `web/src/components/PaginationControls.tsx`)
  - [ ] 9.2 Wire pagination state to `useCandidates` hook's `page` parameter
  - [ ] 9.3 Display total count and current page info ("Showing 1-50 of 130")
  - [ ] 9.4 Ensure page resets to 1 when search or filter params change
  - [ ] 9.5 Unit test: "should display pagination controls when totalCount exceeds pageSize"
  - [ ] 9.6 Unit test: "should reset to page 1 when filter changes"

- [ ] Task 10: Frontend -- Enhanced CandidateDetail with outcome history (AC: #9)
  - [ ] 10.1 Update `CandidateDetail.tsx` to use `useCandidateById` hook instead of finding candidate from list data
  - [ ] 10.2 Add outcome history section: display all completed steps with outcome status (using `StatusBadge`), reason text, who recorded it, and when
  - [ ] 10.3 Display linked documents section with SAS-authenticated download link
  - [ ] 10.4 Display current workflow step prominently at top of detail view
  - [ ] 10.5 Unit test: "should display candidate profile with all imported data fields"
  - [ ] 10.6 Unit test: "should display outcome history for completed steps"
  - [ ] 10.7 Unit test: "should display empty outcome history message when no outcomes recorded"
  - [ ] 10.8 Unit test: "should display linked documents with download action"

- [ ] Task 11: Frontend -- MSW handlers and fixtures (AC: #1, #2, #3, #9)
  - [ ] 11.1 Update MSW handler for `GET /api/recruitments/:id/candidates` to accept `search`, `stepId`, `outcomeStatus` query params and filter mock data accordingly
  - [ ] 11.2 Add MSW handler for `GET /api/recruitments/:id/candidates/:candidateId` returning `CandidateDetailResponse`
  - [ ] 11.3 Extend candidate fixtures in `web/src/mocks/fixtures/candidates.ts` with `currentWorkflowStepId`, `currentWorkflowStepName`, `currentOutcomeStatus` fields
  - [ ] 11.4 Add outcome history fixtures for candidate detail testing

## Dev Notes

### Affected Aggregate(s)

**Candidate** (aggregate root) -- Read-only in this story. The `Candidate` entity at `api/src/Domain/Entities/Candidate.cs` is loaded with its `Outcomes` and `Documents` collections for display. No domain state changes.

**Recruitment** (read-only) -- Loaded to verify membership and to access `Steps` (workflow steps) for computing the candidate's current step position and for populating the step filter dropdown.

### Current Step Computation Logic

A candidate's "current step" is determined by their outcome history and the recruitment's workflow step order:

1. If the candidate has no outcomes, their current step is the first step (lowest `Order`) in the workflow
2. If the candidate's latest outcome is `Pass`, their current step is the next step after the passed step (by `Order`)
3. If the candidate's latest outcome is `Fail` or `Hold`, their current step remains the step where that outcome was recorded
4. If the candidate passed the last step, their current step is still the last step (they've completed all steps)

This logic must be consistent between the backend DTO mapping and the frontend display. The backend computes it authoritatively and the frontend displays what the backend returns.

### Search Implementation

Server-side search uses EF Core's `string.Contains()` with case-insensitive comparison. The search term is applied as OR across `FullName` and `Email`:

```csharp
if (!string.IsNullOrWhiteSpace(request.Search))
{
    var searchTerm = request.Search.Trim();
    query = query.Where(c =>
        c.FullName!.Contains(searchTerm) ||
        c.Email!.Contains(searchTerm));
}
```

Note: SQL Server's default collation (SQL_Latin1_General_CP1_CI_AS) is case-insensitive, so `Contains()` translates to case-insensitive `LIKE '%term%'` in SQL. No `StringComparison` parameter needed for EF Core LINQ-to-SQL.

### Filter Implementation

Step and outcome filtering require joining with the `CandidateOutcome` data. The approach:

1. **Step filter:** Find candidates whose most recent `Pass` outcome corresponds to the step before the filtered step (meaning they've advanced to the filtered step), OR candidates with no outcomes when the filtered step is the first step
2. **Outcome filter:** Filter by the outcome status at the candidate's current step

Since current step is computed from outcome history, the filtering logic in the handler must mirror the current step computation.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Query handler search/filter) | **Test-first** | Core business logic with search/filter SQL generation |
| Task 2 (DTO mapping with step info) | **Test-first** | Business rule: current step computation from outcome history |
| Task 3 (GetCandidateById query) | **Test-first** | Query handler with security checks and SAS URL generation |
| Task 4 (API endpoints) | **Test-first** | Integration boundary: verify status codes and query param binding |
| Task 5 (API client) | **Characterization** | Thin wrapper: test via component integration tests |
| Task 6 (Hooks) | **Characterization** | Thin TanStack Query wrapper: test via component tests |
| Task 7 (Search/filter UI) | **Test-first** | User-facing interaction with debounce, filter state |
| Task 8 (Enhanced list) | **Test-first** | User-facing display with virtualization |
| Task 9 (Pagination) | **Test-first** | State management: page reset on filter change |
| Task 10 (CandidateDetail) | **Test-first** | User-facing display with outcome history |
| Task 11 (MSW handlers) | **Characterization** | Test infrastructure supporting other tests |

### Technical Requirements

**Backend -- Extended GetCandidatesQuery:**

```csharp
// api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs
public record GetCandidatesQuery : IRequest<PaginatedCandidateListDto>
{
    public Guid RecruitmentId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? Search { get; init; }
    public Guid? StepId { get; init; }
    public OutcomeStatus? OutcomeStatus { get; init; }
}
```

**Backend -- Extended CandidateDto:**

```csharp
// api/src/Application/Features/Candidates/CandidateDto.cs
public record CandidateDto
{
    public Guid Id { get; init; }
    public Guid RecruitmentId { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset DateApplied { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DocumentDto? Document { get; init; }
    public Guid? CurrentWorkflowStepId { get; init; }
    public string? CurrentWorkflowStepName { get; init; }
    public string? CurrentOutcomeStatus { get; init; }

    public static CandidateDto From(
        Candidate candidate,
        IReadOnlyList<WorkflowStep> workflowSteps)
    {
        var (currentStep, outcomeStatus) = ComputeCurrentStep(candidate, workflowSteps);

        return new()
        {
            Id = candidate.Id,
            RecruitmentId = candidate.RecruitmentId,
            FullName = candidate.FullName!,
            Email = candidate.Email!,
            PhoneNumber = candidate.PhoneNumber,
            Location = candidate.Location,
            DateApplied = candidate.DateApplied,
            CreatedAt = candidate.CreatedAt,
            Document = candidate.Documents.FirstOrDefault() is { } doc
                ? DocumentDto.From(doc)
                : null,
            CurrentWorkflowStepId = currentStep?.Id,
            CurrentWorkflowStepName = currentStep?.Name,
            CurrentOutcomeStatus = outcomeStatus?.ToString(),
        };
    }

    private static (WorkflowStep? step, OutcomeStatus? status) ComputeCurrentStep(
        Candidate candidate,
        IReadOnlyList<WorkflowStep> steps)
    {
        if (steps.Count == 0)
            return (null, null);

        var orderedSteps = steps.OrderBy(s => s.Order).ToList();

        if (candidate.Outcomes.Count == 0)
            return (orderedSteps[0], OutcomeStatus.NotStarted);

        // Find the latest outcome by step order (highest step order with an outcome)
        var latestOutcome = candidate.Outcomes
            .OrderByDescending(o => orderedSteps.FindIndex(s => s.Id == o.WorkflowStepId))
            .ThenByDescending(o => o.RecordedAt)
            .First();

        var currentStepIndex = orderedSteps.FindIndex(s => s.Id == latestOutcome.WorkflowStepId);

        if (latestOutcome.Status == Domain.Enums.OutcomeStatus.Pass
            && currentStepIndex < orderedSteps.Count - 1)
        {
            // Passed: advance to next step
            return (orderedSteps[currentStepIndex + 1], Domain.Enums.OutcomeStatus.NotStarted);
        }

        // Fail, Hold, or passed last step: stay at current step
        return (orderedSteps[currentStepIndex], latestOutcome.Status);
    }

    // Keep backward compatibility: overload without steps
    public static CandidateDto From(Candidate candidate) =>
        From(candidate, Array.Empty<WorkflowStep>());
}
```

**Backend -- GetCandidateByIdQuery:**

```csharp
// api/src/Application/Features/Candidates/Queries/GetCandidateById/GetCandidateByIdQuery.cs
public record GetCandidateByIdQuery : IRequest<CandidateDetailDto>
{
    public Guid RecruitmentId { get; init; }
    public Guid CandidateId { get; init; }
}
```

**Backend -- CandidateDetailDto:**

```csharp
// api/src/Application/Features/Candidates/Queries/GetCandidateById/CandidateDetailDto.cs
public record CandidateDetailDto
{
    public Guid Id { get; init; }
    public Guid RecruitmentId { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset DateApplied { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CurrentWorkflowStepId { get; init; }
    public string? CurrentWorkflowStepName { get; init; }
    public string? CurrentOutcomeStatus { get; init; }
    public List<DocumentDetailDto> Documents { get; init; } = [];
    public List<OutcomeHistoryDto> OutcomeHistory { get; init; } = [];
}

public record DocumentDetailDto
{
    public Guid Id { get; init; }
    public string DocumentType { get; init; } = null!;
    public string SasUrl { get; init; } = null!;
    public DateTimeOffset UploadedAt { get; init; }
}

public record OutcomeHistoryDto
{
    public Guid WorkflowStepId { get; init; }
    public string WorkflowStepName { get; init; } = null!;
    public int StepOrder { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedByUserId { get; init; }
}
```

**Backend -- Handler authorization pattern (mandatory):**

```csharp
// Both GetCandidatesQueryHandler and GetCandidateByIdQueryHandler MUST:
var recruitment = await dbContext.Recruitments
    .Include(r => r.Members)
    .Include(r => r.Steps)
    .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
    ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

var userId = tenantContext.UserGuid;
if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
    throw new ForbiddenAccessException();
```

**Frontend -- Extended types:**

```typescript
// web/src/lib/api/candidates.types.ts (extend existing)
export interface CandidateResponse {
  id: string
  recruitmentId: string
  fullName: string
  email: string
  phoneNumber: string | null
  location: string | null
  dateApplied: string
  createdAt: string
  document: CandidateDocumentDto | null
  currentWorkflowStepId: string | null
  currentWorkflowStepName: string | null
  currentOutcomeStatus: string | null
}

export interface CandidateDetailResponse {
  id: string
  recruitmentId: string
  fullName: string
  email: string
  phoneNumber: string | null
  location: string | null
  dateApplied: string
  createdAt: string
  currentWorkflowStepId: string | null
  currentWorkflowStepName: string | null
  currentOutcomeStatus: string | null
  documents: DocumentDetailDto[]
  outcomeHistory: OutcomeHistoryEntry[]
}

export interface DocumentDetailDto {
  id: string
  documentType: string
  sasUrl: string
  uploadedAt: string
}

export interface OutcomeHistoryEntry {
  workflowStepId: string
  workflowStepName: string
  stepOrder: number
  status: string
  recordedAt: string
  recordedByUserId: string
}
```

**Frontend -- Extended API client:**

```typescript
// web/src/lib/api/candidates.ts (extend existing)
export const candidateApi = {
  // ... existing methods ...

  getAll: (
    recruitmentId: string,
    page = 1,
    pageSize = 50,
    search?: string,
    stepId?: string,
    outcomeStatus?: string,
  ) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    })
    if (search) params.set('search', search)
    if (stepId) params.set('stepId', stepId)
    if (outcomeStatus) params.set('outcomeStatus', outcomeStatus)
    return apiGet<PaginatedCandidateList>(
      `/recruitments/${recruitmentId}/candidates?${params}`,
    )
  },

  getById: (recruitmentId: string, candidateId: string) =>
    apiGet<CandidateDetailResponse>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}`,
    ),
}
```

**Frontend -- Updated useCandidates hook:**

```typescript
// web/src/features/candidates/hooks/useCandidates.ts
import { useQuery } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

interface UseCandidatesParams {
  recruitmentId: string
  page?: number
  pageSize?: number
  search?: string
  stepId?: string
  outcomeStatus?: string
}

export function useCandidates({
  recruitmentId,
  page = 1,
  pageSize = 50,
  search,
  stepId,
  outcomeStatus,
}: UseCandidatesParams) {
  return useQuery({
    queryKey: ['candidates', recruitmentId, { page, search, stepId, outcomeStatus }],
    queryFn: () => candidateApi.getAll(recruitmentId, page, pageSize, search, stepId, outcomeStatus),
    enabled: !!recruitmentId,
  })
}
```

**Frontend -- useCandidateById hook:**

```typescript
// web/src/features/candidates/hooks/useCandidateById.ts
import { useQuery } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

export function useCandidateById(recruitmentId: string, candidateId: string) {
  return useQuery({
    queryKey: ['candidate', recruitmentId, candidateId],
    queryFn: () => candidateApi.getById(recruitmentId, candidateId),
    enabled: !!recruitmentId && !!candidateId,
  })
}
```

**Frontend -- useDebounce hook:**

```typescript
// web/src/hooks/useDebounce.ts
import { useState, useEffect } from 'react'

export function useDebounce<T>(value: T, delay = 300): T {
  const [debouncedValue, setDebouncedValue] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])

  return debouncedValue
}
```

### Architecture Compliance

- **Read-only aggregate access:** This story only reads Candidate and Recruitment aggregates. No domain state changes. No aggregate root methods called.
- **Handler authorization:** Both `GetCandidatesQueryHandler` and `GetCandidateByIdQueryHandler` verify membership via `ITenantContext.UserGuid`. No unscoped queries.
- **Ubiquitous language:** Use "Candidate" (not applicant), "Workflow Step" (not stage/phase), "Outcome" (not result/verdict), "Recruitment" (not job/position).
- **Manual DTO mapping:** `CandidateDto.From()` and `CandidateDetailDto` use static factory methods. No AutoMapper.
- **Problem Details for errors:** `NotFoundException` maps to 404, `ForbiddenAccessException` maps to 403 via global exception middleware.
- **NSubstitute for ALL mocking** (never Moq).
- **Empty state handling:** CandidateList shows EmptyState component when no candidates exist.
- **StatusBadge for status display:** Outcome status uses the shared `StatusBadge` component.
- **SkeletonLoader for loading states:** No "Loading..." text.
- **httpClient.ts as single HTTP entry point:** All API calls through `apiGet`/`apiPost`. Frontend never calls `fetch` directly.
- **SAS URLs for documents:** `GetCandidateByIdQueryHandler` generates SAS URLs via `IBlobStorageService.GenerateSasUri()`. No direct blob URLs.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. `Include()` for loading candidates with outcomes. `AsNoTracking()` for read queries. |
| MediatR | 13.x | `IRequest<T>` for queries returning DTOs. |
| React | 19.x | Hooks for state management. |
| TypeScript | 5.7.x | Strict mode. |
| TanStack Query | 5.x | `queryKey` includes filter params for cache isolation. `enabled` flag for conditional fetching. |
| react-virtuoso | Latest | `Virtuoso` component for virtualized list rendering. |
| shadcn/ui | Installed | `Select` for filter dropdowns. `Input` for search field. `Badge` for active filters. |
| Tailwind CSS | 4.x | CSS-first config. |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Candidates/
  Queries/
    GetCandidateById/
      GetCandidateByIdQuery.cs
      GetCandidateByIdQueryHandler.cs
      CandidateDetailDto.cs

api/tests/Application.UnitTests/Features/Candidates/
  Queries/
    GetCandidatesQueryHandlerTests.cs    (search/filter tests)
    GetCandidateByIdQueryHandlerTests.cs

web/src/features/candidates/hooks/
  useCandidateById.ts

web/src/hooks/
  useDebounce.ts                         (if not already present)
```

**Existing files to modify:**
```
api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs
  -- Add Search, StepId, OutcomeStatus params

api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandler.cs
  -- Add search/filter logic, include outcomes + steps

api/src/Application/Features/Candidates/CandidateDto.cs
  -- Add CurrentWorkflowStepId, CurrentWorkflowStepName, CurrentOutcomeStatus + From() overload with steps

api/src/Web/Endpoints/CandidateEndpoints.cs
  -- Add GET /{candidateId:guid} endpoint, update GetCandidates with query params

web/src/lib/api/candidates.types.ts
  -- Add CandidateDetailResponse, DocumentDetailDto, OutcomeHistoryEntry, extend CandidateResponse

web/src/lib/api/candidates.ts
  -- Update getAll() with search/filter params, add getById()

web/src/features/candidates/hooks/useCandidates.ts
  -- Update to accept search/filter/page params

web/src/features/candidates/CandidateList.tsx
  -- Add search input, filter dropdowns, StatusBadge display, virtualization, pagination

web/src/features/candidates/CandidateDetail.tsx
  -- Use useCandidateById hook, add outcome history section, documents with SAS URLs

web/src/mocks/fixtures/candidates.ts
  -- Add currentWorkflowStep fields, outcome history fixtures

web/src/mocks/candidateHandlers.ts
  -- Add search/filter query param handling, add GET by ID handler
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**

GetCandidatesQuery handler (search/filter):
- `Handle_WithSearchTerm_FiltersByNameSubstring`
- `Handle_WithSearchTerm_FiltersByEmailSubstring`
- `Handle_WithSearchTerm_CaseInsensitive`
- `Handle_WithStepIdFilter_ReturnsOnlyCandidatesAtStep`
- `Handle_WithOutcomeStatusFilter_ReturnsOnlyCandidatesWithStatus`
- `Handle_WithCombinedFilters_AppliesAndLogic`
- `Handle_WithFilters_TotalCountReflectsFilteredResults`
- `Handle_NonMemberUser_ThrowsForbiddenAccessException`

CandidateDto (current step computation):
- `From_CandidateWithNoOutcomes_CurrentStepIsFirstStep`
- `From_CandidateWithPassOutcome_CurrentStepIsNextStep`
- `From_CandidateAtLastStepWithPass_CurrentStepIsLastStep`
- `From_CandidateWithFailOutcome_CurrentStepIsSameStep`
- `From_CandidateWithHoldOutcome_CurrentStepIsSameStep`
- `From_CandidateWithNoSteps_CurrentStepIsNull`

GetCandidateByIdQuery handler:
- `Handle_ValidRequest_ReturnsCandidateWithAllFields`
- `Handle_ValidRequest_IncludesOutcomeHistoryWithStepNames`
- `Handle_ValidRequest_IncludesDocumentsWithSasUrls`
- `Handle_NonMemberUser_ThrowsForbiddenAccessException`
- `Handle_CandidateNotFound_ThrowsNotFoundException`

Integration tests (API endpoints):
- `GetCandidateById_ValidId_Returns200`
- `GetCandidateById_NotFound_Returns404ProblemDetails`
- `GetCandidateById_NonMember_Returns403`
- `GetCandidates_WithSearch_ReturnsFilteredResults`
- `GetCandidates_WithStepFilter_ReturnsFilteredResults`
- `GetCandidates_WithOutcomeFilter_ReturnsFilteredResults`
- `GetCandidates_WithCombinedFilters_ReturnsFilteredResults`

**Frontend tests (Vitest + Testing Library + MSW):**

CandidateList (search/filter):
- "should display search input and filter dropdowns"
- "should filter candidates when search term is entered after debounce"
- "should filter candidates when step filter is selected"
- "should filter candidates when outcome filter is selected"
- "should show active filter badges that are individually clearable"
- "should display current step and outcome status for each candidate"
- "should display pagination controls when results exceed page size"
- "should reset to page 1 when filter changes"
- "should display empty state when no candidates match filters"
- "should render empty state with import and add actions when recruitment has no candidates"

CandidateDetail (outcome history):
- "should display all candidate profile fields"
- "should display outcome history with step name, status, and recorded date"
- "should display documents with download action"
- "should show empty outcome history when no outcomes recorded"
- "should show loading skeleton while data is fetching"

MSW handlers:
- `GET /api/recruitments/:id/candidates` -- accepts search, stepId, outcomeStatus params
- `GET /api/recruitments/:id/candidates/:candidateId` -- returns CandidateDetailResponse

### Previous Story Intelligence

**From Story 3.1 (Manual Candidate Management):**
- `CandidateList.tsx` exists with basic list display -- this story enhances it with search, filter, step/outcome display, virtualization, and pagination
- `CandidateDetail.tsx` exists with basic profile + `DocumentUpload` -- this story enhances it with outcome history and SAS-authenticated documents
- `useCandidates` hook exists -- this story updates its signature to accept filter params
- `CandidateEndpoints.cs` exists with `GET /` and CRUD -- this story adds `GET /{candidateId}` and search/filter query params

**From Story 3.5 (CV Auto-Match, Manual Assignment & Individual Upload):**
- `CandidateDto` includes `DocumentDto` -- this story adds current step and outcome fields
- `CandidateDetail.tsx` includes `DocumentUpload` component -- this story adds outcome history display

**From Story 1.4 (Shared UI Components):**
- `StatusBadge` component for outcome status display (Pass=green, Fail=red, Hold=amber, NotStarted=gray)
- `EmptyState` component for no-candidates state
- `SkeletonLoader` for loading states
- `PaginationControls` for pagination UI
- `useAppToast()` for toast notifications

**From Story 2.1-2.5 (Recruitment CRUD):**
- `Recruitment` aggregate with `WorkflowStep` children -- used for step filter dropdown and current step computation
- `useRecruitment` hook provides recruitment data including workflow steps

**From Story 1.3 (Core Data Model):**
- `CandidateOutcome` entity with `CandidateId`, `WorkflowStepId`, `Status`, `RecordedAt`, `RecordedByUserId`
- `OutcomeStatus` enum: NotStarted, Pass, Fail, Hold

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(4.1): extend CandidateDto with current step computation + tests`
2. `feat(4.1): add search/filter to GetCandidatesQuery handler + tests`
3. `feat(4.1): add GetCandidateByIdQuery with SAS URLs + tests`
4. `feat(4.1): add candidate detail endpoint + search/filter query params + integration tests`
5. `feat(4.1): extend frontend API client and types for search/filter/detail`
6. `feat(4.1): add useDebounce + useCandidateById hooks`
7. `feat(4.1): add search and filter controls to CandidateList + tests`
8. `feat(4.1): add virtualized list rendering with react-virtuoso`
9. `feat(4.1): add pagination controls to CandidateList + tests`
10. `feat(4.1): enhance CandidateDetail with outcome history + documents + tests`
11. `feat(4.1): update MSW handlers and fixtures for search/filter/detail`

### Latest Tech Information

- **.NET 10.0:** Use primary constructors for handler DI. `AsNoTracking()` for read-only queries.
- **EF Core 10:** `Include()` chains for eager loading. `StringComparison` not needed for SQL Server case-insensitive matching -- default collation handles it.
- **MediatR 13.x:** `IRequest<T>` for queries returning DTOs.
- **React 19.2:** Controlled inputs for search field. `useState` + `useEffect` for debounce.
- **TanStack Query 5.90.x:** `queryKey` array includes filter params -- automatic refetch on key change. `keepPreviousData` (now `placeholderData`) for smooth pagination transitions.
- **react-virtuoso 4.x:** `Virtuoso` component with `totalCount`, `itemContent`, and `style` props. Handles variable-height rows.
- **shadcn/ui Select:** Controlled select with `onValueChange`. `SelectTrigger`, `SelectContent`, `SelectItem` pattern.

### Project Structure Notes

- `GetCandidateByIdQuery` follows the one-query-per-folder pattern under `Application/Features/Candidates/Queries/GetCandidateById/`
- `CandidateDetailDto` is colocated with its query in the `GetCandidateById/` folder
- `useDebounce` is a shared hook in `web/src/hooks/` (not feature-specific)
- `useCandidateById` is a candidate-feature hook in `web/src/features/candidates/hooks/`
- react-virtuoso is a new dependency; add via `npm install react-virtuoso`

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-screening-outcome-recording.md` -- Story 4.1 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries (Candidate with Outcomes, Recruitment with Steps), ITenantContext, handler authorization]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- CQRS structure, handler authorization, DTO mapping, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, StatusBadge, EmptyState, loading states]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Response formats, pagination wrapper, Problem Details]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, query folder structure]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- API client contract, TanStack Query patterns]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Test frameworks, naming conventions, mandatory security scenarios]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR36, FR45, FR46, FR51 requirements]
- [Source: `api/src/Domain/Entities/Candidate.cs` -- Candidate aggregate with Outcomes and Documents collections]
- [Source: `api/src/Domain/Entities/CandidateOutcome.cs` -- Outcome entity with WorkflowStepId, Status, RecordedAt]
- [Source: `api/src/Domain/Entities/WorkflowStep.cs` -- Step entity with Name, Order]
- [Source: `api/src/Application/Features/Candidates/CandidateDto.cs` -- Existing DTO with From() mapping]
- [Source: `api/src/Application/Features/Candidates/Queries/GetCandidates/` -- Existing query, handler, paginated DTO]
- [Source: `web/src/features/candidates/CandidateList.tsx` -- Existing component to enhance]
- [Source: `web/src/features/candidates/CandidateDetail.tsx` -- Existing component to enhance]
- [Source: `web/src/lib/api/candidates.ts` -- Existing API client to extend]
- [Source: `web/src/lib/api/candidates.types.ts` -- Existing types to extend]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy, mode declarations]

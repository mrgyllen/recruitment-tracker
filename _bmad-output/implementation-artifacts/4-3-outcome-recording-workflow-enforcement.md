# Story 4.3: Outcome Recording & Workflow Enforcement

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **user (Lina)**,
I want to **record an outcome (Pass, Fail, or Hold) for a candidate at their current workflow step with an optional reason, and advance passing candidates to the next step**,
so that **screening decisions are documented and candidates progress through the hiring pipeline**.

## Acceptance Criteria

### AC1: Outcome controls display
**Given** a candidate is at a workflow step with no outcome recorded
**When** the user views the outcome controls
**Then** three outcome buttons are displayed: Pass, Fail, and Hold
**And** an always-visible reason textarea is shown (not hidden behind an "add note" button)
**And** a confirm button is available

### AC2: Outcome recording
**Given** the user selects an outcome (Pass, Fail, or Hold)
**When** they click the confirm button (or press Enter)
**Then** the outcome is recorded for the candidate at their current step
**And** the optional reason text is saved with the outcome
**And** visual confirmation is shown within 500ms (NFR5)

### AC3: Pass auto-advance
**Given** the user records a "Pass" outcome
**When** the outcome is saved
**Then** the candidate is automatically advanced to the next workflow step
**And** the candidate's status at the new step is "Not Started"

### AC4: Fail/Hold stay at step
**Given** the user records a "Fail" or "Hold" outcome
**When** the outcome is saved
**Then** the candidate remains at their current step
**And** the candidate's outcome status reflects the recorded decision

### AC5: Last step pass
**Given** a candidate is at the last workflow step
**When** the user records a "Pass" outcome
**Then** the candidate is marked as having completed all steps
**And** no further advancement occurs

### AC6: Re-record outcome
**Given** a candidate already has an outcome recorded at their current step
**When** the user views the outcome controls
**Then** the previously recorded outcome and reason are displayed
**And** the user can update the outcome (re-record with a different decision)

### AC7: Workflow sequence enforcement
**Given** a candidate is at step 3 of a 5-step workflow
**When** the user attempts to record an outcome at step 5 directly
**Then** the system enforces the workflow step sequence
**And** the candidate can only have outcomes recorded at their current step

### AC8: Outcome history display
**Given** the user views a candidate's detail
**When** they look at the outcome history section
**Then** all completed steps are shown with their recorded outcome, reason, who recorded it, and when

### AC9: Closed recruitment read-only
**Given** a closed recruitment
**When** the user views a candidate's outcome controls
**Then** outcome recording is disabled (read-only mode)

### Prerequisites
- **Epic 1** -- Domain model (Candidate aggregate with CandidateOutcome, WorkflowStep, Recruitment entity)
- **Epic 3** -- Candidate management (CandidateList.tsx, CandidateDetail.tsx, CandidateEndpoints.cs, candidates.ts API client)
- **Story 4.1** (if implemented first) -- GetCandidatesQuery with pagination/filtering, CandidateList.tsx screening-ready version
- **Story 4.2** (if implemented first) -- PdfViewer.tsx for inline CV viewing

### FRs Fulfilled
- **FR37:** Users can record an outcome (Pass, Fail, Hold) for a candidate at a specific workflow step
- **FR38:** Outcomes include who recorded them and when (full audit trail)
- **FR39:** Candidates with a "Pass" outcome at their current step advance to the next step automatically
- **FR40:** The system enforces the correct step sequence -- no skipping steps

## Tasks / Subtasks

- [ ] Task 1: Backend -- Enhance Candidate.RecordOutcome domain method with workflow enforcement (AC: #2, #3, #4, #5, #7)
  - [ ] 1.1 Extend `Candidate.RecordOutcome()` in `api/src/Domain/Entities/Candidate.cs` to accept `reason` (string?), the list of workflow steps (ordered), and enforce step sequence
  - [ ] 1.2 Add `CurrentWorkflowStepId` property to `Candidate` entity (nullable Guid, tracks which step the candidate is currently at)
  - [ ] 1.3 Add `IsCompleted` property to `Candidate` entity (bool, true when passed the last step)
  - [ ] 1.4 Enforce workflow sequence: if `workflowStepId != CurrentWorkflowStepId`, throw `InvalidWorkflowTransitionException`
  - [ ] 1.5 On Pass: if not last step, advance `CurrentWorkflowStepId` to next step in order. If last step, set `IsCompleted = true`
  - [ ] 1.6 On Fail/Hold: keep `CurrentWorkflowStepId` unchanged
  - [ ] 1.7 Allow re-recording: if an outcome already exists for this step, update it (remove old, add new)
  - [ ] 1.8 Extend `CandidateOutcome` entity to include `Reason` (string?) property
  - [ ] 1.9 Domain test: `RecordOutcome_ValidStep_CreatesOutcomeWithReason`
  - [ ] 1.10 Domain test: `RecordOutcome_WrongStep_ThrowsInvalidWorkflowTransitionException`
  - [ ] 1.11 Domain test: `RecordOutcome_PassNotLastStep_AdvancesToNextStep`
  - [ ] 1.12 Domain test: `RecordOutcome_PassLastStep_SetsIsCompleted`
  - [ ] 1.13 Domain test: `RecordOutcome_FailOrHold_StaysAtCurrentStep`
  - [ ] 1.14 Domain test: `RecordOutcome_ReRecord_ReplacesExistingOutcome`

- [ ] Task 2: Backend -- RecordOutcomeCommand + handler + validator (AC: #2, #7, #9)
  - [ ] 2.1 Create `RecordOutcomeCommand` record in `api/src/Application/Features/Screening/Commands/RecordOutcome/`
  - [ ] 2.2 Fields: `RecruitmentId` (Guid), `CandidateId` (Guid), `WorkflowStepId` (Guid), `Outcome` (OutcomeStatus), `Reason` (string?)
  - [ ] 2.3 Create `RecordOutcomeCommandValidator` with FluentValidation: RecruitmentId, CandidateId, WorkflowStepId required; Outcome must be Pass/Fail/Hold (not NotStarted); Reason max 500 characters
  - [ ] 2.4 Create `RecordOutcomeCommandHandler`:
    - Load recruitment with members and steps, verify user is member (`ITenantContext.UserGuid`)
    - Verify recruitment is not closed (throw `RecruitmentClosedException`)
    - Verify workflowStepId exists in recruitment's steps
    - Load candidate with outcomes
    - Call `candidate.RecordOutcome(workflowStepId, outcome, tenantContext.UserGuid, reason, recruitment.Steps)` -- domain enforces step sequence
    - Save changes
    - Return `OutcomeResultDto` with outcome details and new current step info
  - [ ] 2.5 Create `OutcomeResultDto` record: `OutcomeId`, `CandidateId`, `WorkflowStepId`, `Outcome`, `Reason`, `RecordedAt`, `RecordedBy`, `NewCurrentStepId`, `IsCompleted`
  - [ ] 2.6 Unit test: `Handle_ValidOutcome_RecordsAndReturnsDto`
  - [ ] 2.7 Unit test: `Handle_ClosedRecruitment_ThrowsRecruitmentClosedException`
  - [ ] 2.8 Unit test: `Handle_NonMemberUser_ThrowsForbiddenAccessException`
  - [ ] 2.9 Unit test: `Handle_InvalidStep_ThrowsNotFoundException`
  - [ ] 2.10 Unit test: `Handle_WrongStepSequence_ThrowsInvalidWorkflowTransitionException`

- [ ] Task 3: Backend -- GetCandidateOutcomeHistoryQuery (AC: #8)
  - [ ] 3.1 Create `GetCandidateOutcomeHistoryQuery` in `api/src/Application/Features/Screening/Queries/GetCandidateOutcomeHistory/`
  - [ ] 3.2 Fields: `RecruitmentId` (Guid), `CandidateId` (Guid)
  - [ ] 3.3 Create `GetCandidateOutcomeHistoryQueryHandler`:
    - Load recruitment with members, verify user is member
    - Load candidate with outcomes
    - Join outcomes with workflow steps to get step names and order
    - Return ordered list of `OutcomeHistoryDto`
  - [ ] 3.4 Create `OutcomeHistoryDto` record: `WorkflowStepId`, `WorkflowStepName`, `StepOrder`, `Outcome`, `Reason`, `RecordedAt`, `RecordedByUserId`, `RecordedByDisplayName`
  - [ ] 3.5 Unit test: `Handle_ValidRequest_ReturnsOrderedHistory`
  - [ ] 3.6 Unit test: `Handle_NonMemberUser_ThrowsForbiddenAccessException`
  - [ ] 3.7 Unit test: `Handle_CandidateNotFound_ThrowsNotFoundException`

- [ ] Task 4: Backend -- Screening API endpoints (AC: #2, #8, #9)
  - [ ] 4.1 Create `ScreeningEndpoints.cs` in `api/src/Web/Endpoints/` inheriting from `EndpointGroupBase`
  - [ ] 4.2 GroupName: `"recruitments/{recruitmentId:guid}/candidates/{candidateId:guid}/screening"`
  - [ ] 4.3 `POST /outcome` -- accepts `RecordOutcomeRequest` body, sends `RecordOutcomeCommand` via MediatR, returns `OutcomeResultDto`
  - [ ] 4.4 `GET /history` -- sends `GetCandidateOutcomeHistoryQuery` via MediatR, returns list of `OutcomeHistoryDto`
  - [ ] 4.5 Integration test: `RecordOutcome_ValidRequest_Returns200WithDto`
  - [ ] 4.6 Integration test: `RecordOutcome_ClosedRecruitment_Returns400ProblemDetails`
  - [ ] 4.7 Integration test: `RecordOutcome_InvalidStepSequence_Returns400ProblemDetails`
  - [ ] 4.8 Integration test: `GetOutcomeHistory_ValidRequest_ReturnsOrderedList`

- [ ] Task 5: Frontend -- Screening API client and types (AC: #2, #8)
  - [ ] 5.1 Create `web/src/lib/api/screening.types.ts` with `RecordOutcomeRequest`, `OutcomeResultDto`, `OutcomeHistoryDto`, `OutcomeStatus` type
  - [ ] 5.2 Create `web/src/lib/api/screening.ts` with `screeningApi.recordOutcome()` and `screeningApi.getOutcomeHistory()` methods using `apiPost` and `apiGet` from httpClient.ts

- [ ] Task 6: Frontend -- OutcomeForm component (AC: #1, #2, #4, #6, #9)
  - [ ] 6.1 Create `web/src/features/screening/OutcomeForm.tsx` with Pass/Fail/Hold buttons, reason textarea, and confirm button
  - [ ] 6.2 Props: `recruitmentId`, `candidateId`, `currentStepId`, `currentStepName`, `existingOutcome` (OutcomeHistoryDto | null), `isClosed`, `onOutcomeRecorded` callback
  - [ ] 6.3 Outcome buttons visually indicate selection state (highlight selected, dim others)
  - [ ] 6.4 Reason textarea is always visible (not hidden behind a toggle)
  - [ ] 6.5 If `existingOutcome` is provided, pre-fill the form with the existing outcome and reason
  - [ ] 6.6 Confirm button disabled until an outcome is selected; shows inline spinner while mutation is pending
  - [ ] 6.7 On successful recording, call `onOutcomeRecorded` callback with result DTO
  - [ ] 6.8 When `isClosed` is true, all controls are disabled/read-only
  - [ ] 6.9 Create `useRecordOutcome` mutation hook in `web/src/features/screening/hooks/useRecordOutcome.ts`
  - [ ] 6.10 Test: "should display Pass, Fail, Hold buttons"
  - [ ] 6.11 Test: "should show reason textarea always visible"
  - [ ] 6.12 Test: "should disable confirm button when no outcome selected"
  - [ ] 6.13 Test: "should call API with correct parameters on confirm"
  - [ ] 6.14 Test: "should pre-fill form with existing outcome"
  - [ ] 6.15 Test: "should disable all controls when recruitment is closed"
  - [ ] 6.16 Test: "should show success feedback within 500ms"

- [ ] Task 7: Frontend -- Outcome history display (AC: #8)
  - [ ] 7.1 Create `web/src/features/screening/OutcomeHistory.tsx` showing completed steps with outcome, reason, who, when
  - [ ] 7.2 Create `useOutcomeHistory` query hook in `web/src/features/screening/hooks/useOutcomeHistory.ts`
  - [ ] 7.3 Display steps in order with `StatusBadge` component for outcome status
  - [ ] 7.4 Each entry shows: step name, outcome badge, reason (if any), recorded by display name, formatted date
  - [ ] 7.5 Test: "should display outcome history ordered by step"
  - [ ] 7.6 Test: "should show reason text when provided"
  - [ ] 7.7 Test: "should display empty state when no outcomes recorded"

- [ ] Task 8: Frontend -- MSW handlers and fixtures (AC: #2, #8)
  - [ ] 8.1 Create `web/src/mocks/screeningHandlers.ts` with MSW handlers for POST outcome and GET history
  - [ ] 8.2 Add screening fixtures to `web/src/mocks/fixtures/` (sample outcomes, history data)
  - [ ] 8.3 Register handlers in `web/src/mocks/handlers.ts`

## Dev Notes

### Affected Aggregate(s)

**Candidate** (aggregate root) -- Primary aggregate for this story. The `Candidate` entity at `api/src/Domain/Entities/Candidate.cs` owns `CandidateOutcome` as a child entity. Outcome recording flows through the aggregate root's `RecordOutcome()` method.

Key existing domain methods and state:
- `Candidate.RecordOutcome(workflowStepId, status, recordedByUserId)` -- EXISTING method that creates a `CandidateOutcome` and adds it. **This story extends it** to accept `reason`, enforce step sequence, and handle auto-advance
- `CandidateOutcome.Create(candidateId, workflowStepId, status, recordedByUserId)` -- EXISTING internal factory. **This story extends it** to accept `reason`
- `_outcomes` collection -- existing list of `CandidateOutcome` owned by candidate

New state added by this story:
- `Candidate.CurrentWorkflowStepId` (Guid?) -- tracks which step the candidate is currently at. Set when candidate is first added to a recruitment (to the first step) and updated on Pass outcomes
- `Candidate.IsCompleted` (bool) -- set to true when the candidate passes the last workflow step

**Recruitment** (read-only in this story) -- Loaded to verify membership, closed status, and to retrieve the ordered workflow steps. Not modified.

Cross-aggregate: The handler loads Recruitment (read-only) to get the ordered steps list, then operates on Candidate. Single aggregate write per transaction.

### Important Domain Logic Notes

**Current `RecordOutcome` method (at `api/src/Domain/Entities/Candidate.cs:48-53`) is simple -- it just creates and adds an outcome without any workflow enforcement.** This story must extend it to:

1. Accept `reason` (string?) parameter
2. Accept `workflowSteps` (ordered list) parameter for step sequence validation
3. Validate that `workflowStepId == CurrentWorkflowStepId` (throw `InvalidWorkflowTransitionException` if not)
4. Handle re-recording: if an outcome already exists at this step, remove it before adding the new one
5. On Pass: advance `CurrentWorkflowStepId` to next step in order, or set `IsCompleted = true` if last step
6. On Fail/Hold: no advancement

**The existing `InvalidWorkflowTransitionException` (at `api/src/Domain/Exceptions/InvalidWorkflowTransitionException.cs`) takes `from` and `to` strings.** When throwing for step sequence violations, use the step names or IDs to provide a meaningful message.

**CandidateOutcome entity (at `api/src/Domain/Entities/CandidateOutcome.cs`) currently lacks a `Reason` property.** This story adds `Reason` (string?) and extends the `Create` factory to accept it.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Domain RecordOutcome enhancement) | **Test-first** | Core business logic with step sequence enforcement, auto-advance, and invariant protection |
| Task 2 (RecordOutcomeCommand handler) | **Test-first** | Command handler with security checks, domain delegation, and DTO mapping |
| Task 3 (GetCandidateOutcomeHistoryQuery) | **Test-first** | Query handler with authorization and data projection |
| Task 4 (API endpoints) | **Test-first** | Integration boundary -- verify status codes, Problem Details, and response shapes |
| Task 5 (API client) | **Characterization** | Thin wrapper -- tested via component integration tests |
| Task 6 (OutcomeForm component) | **Test-first** | User-facing form with selection state, validation, and mutation |
| Task 7 (OutcomeHistory display) | **Test-first** | Data display with ordering and status badges |
| Task 8 (MSW handlers) | **Characterization** | Test infrastructure -- verified through component test usage |

### Technical Requirements

**Backend -- Enhanced Candidate.RecordOutcome method:**

```csharp
// Extend api/src/Domain/Entities/Candidate.cs
// Replace the existing RecordOutcome method with:
public void RecordOutcome(
    Guid workflowStepId,
    OutcomeStatus status,
    Guid recordedByUserId,
    string? reason,
    IReadOnlyList<WorkflowStep> orderedSteps)
{
    // 1. Enforce step sequence
    if (CurrentWorkflowStepId is null)
        throw new InvalidOperationException("Candidate has not been assigned to a workflow step.");

    if (workflowStepId != CurrentWorkflowStepId)
        throw new InvalidWorkflowTransitionException(
            CurrentWorkflowStepId.ToString()!,
            workflowStepId.ToString());

    // 2. Remove existing outcome at this step (re-record support)
    var existing = _outcomes.FirstOrDefault(o => o.WorkflowStepId == workflowStepId);
    if (existing is not null)
        _outcomes.Remove(existing);

    // 3. Record new outcome
    var outcome = CandidateOutcome.Create(Id, workflowStepId, status, recordedByUserId, reason);
    _outcomes.Add(outcome);

    // 4. Handle advancement
    if (status == OutcomeStatus.Pass)
    {
        var currentStep = orderedSteps.First(s => s.Id == workflowStepId);
        var nextStep = orderedSteps
            .Where(s => s.Order > currentStep.Order)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        if (nextStep is not null)
        {
            CurrentWorkflowStepId = nextStep.Id;
        }
        else
        {
            IsCompleted = true;
        }
    }

    AddDomainEvent(new OutcomeRecordedEvent(Id, workflowStepId));
}

// New method to initialize candidate's position in workflow
public void AssignToWorkflowStep(Guid firstStepId)
{
    CurrentWorkflowStepId = firstStepId;
}
```

**Backend -- Extended CandidateOutcome entity:**

```csharp
// Extend api/src/Domain/Entities/CandidateOutcome.cs
public class CandidateOutcome : GuidEntity
{
    public Guid CandidateId { get; private set; }
    public Guid WorkflowStepId { get; private set; }
    public OutcomeStatus Status { get; private set; }
    public string? Reason { get; private set; }  // NEW
    public DateTimeOffset RecordedAt { get; private set; }
    public Guid RecordedByUserId { get; private set; }

    private CandidateOutcome() { } // EF Core

    internal static CandidateOutcome Create(
        Guid candidateId, Guid workflowStepId, OutcomeStatus status,
        Guid recordedByUserId, string? reason = null)  // reason param added
    {
        return new CandidateOutcome
        {
            CandidateId = candidateId,
            WorkflowStepId = workflowStepId,
            Status = status,
            Reason = reason,
            RecordedAt = DateTimeOffset.UtcNow,
            RecordedByUserId = recordedByUserId,
        };
    }
}
```

**Backend -- RecordOutcomeCommand:**

```csharp
// api/src/Application/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommand.cs
public record RecordOutcomeCommand(
    Guid RecruitmentId,
    Guid CandidateId,
    Guid WorkflowStepId,
    OutcomeStatus Outcome,
    string? Reason
) : IRequest<OutcomeResultDto>;

// Validator
public class RecordOutcomeCommandValidator : AbstractValidator<RecordOutcomeCommand>
{
    public RecordOutcomeCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.WorkflowStepId).NotEmpty();
        RuleFor(x => x.Outcome)
            .IsInEnum()
            .Must(o => o != OutcomeStatus.NotStarted)
            .WithMessage("Outcome must be Pass, Fail, or Hold.");
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => x.Reason is not null);
    }
}
```

**Backend -- RecordOutcomeCommandHandler pattern:**

```csharp
// api/src/Application/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommandHandler.cs
public class RecordOutcomeCommandHandler(
    IApplicationDbContext context,
    ITenantContext tenantContext)
    : IRequestHandler<RecordOutcomeCommand, OutcomeResultDto>
{
    public async Task<OutcomeResultDto> Handle(RecordOutcomeCommand request, CancellationToken ct)
    {
        var recruitment = await context.Recruitments
            .Include(r => r.Members)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        // MANDATORY: Verify current user is a member
        if (!recruitment.Members.Any(m => m.UserId == tenantContext.UserGuid))
            throw new ForbiddenAccessException();

        // MANDATORY: Verify recruitment is not closed
        if (recruitment.Status == RecruitmentStatus.Closed)
            throw new RecruitmentClosedException(recruitment.Id);

        // Verify step exists in this recruitment
        if (!recruitment.Steps.Any(s => s.Id == request.WorkflowStepId))
            throw new NotFoundException(nameof(WorkflowStep), request.WorkflowStepId);

        var candidate = await context.Candidates
            .Include(c => c.Outcomes)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        // Get ordered steps for domain method
        var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        // Domain enforces step sequence and handles advancement
        candidate.RecordOutcome(
            request.WorkflowStepId,
            request.Outcome,
            tenantContext.UserGuid,
            request.Reason,
            orderedSteps);

        await context.SaveChangesAsync(ct);

        return OutcomeResultDto.From(candidate, request.WorkflowStepId);
    }
}
```

**Backend -- DTOs:**

```csharp
// api/src/Application/Features/Screening/Commands/RecordOutcome/OutcomeResultDto.cs
public record OutcomeResultDto
{
    public Guid OutcomeId { get; init; }
    public Guid CandidateId { get; init; }
    public Guid WorkflowStepId { get; init; }
    public OutcomeStatus Outcome { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedBy { get; init; }
    public Guid? NewCurrentStepId { get; init; }
    public bool IsCompleted { get; init; }

    public static OutcomeResultDto From(Candidate candidate, Guid stepId)
    {
        var outcome = candidate.Outcomes.First(o => o.WorkflowStepId == stepId);
        return new OutcomeResultDto
        {
            OutcomeId = outcome.Id,
            CandidateId = candidate.Id,
            WorkflowStepId = stepId,
            Outcome = outcome.Status,
            Reason = outcome.Reason,
            RecordedAt = outcome.RecordedAt,
            RecordedBy = outcome.RecordedByUserId,
            NewCurrentStepId = candidate.CurrentWorkflowStepId,
            IsCompleted = candidate.IsCompleted,
        };
    }
}

// api/src/Application/Features/Screening/Queries/GetCandidateOutcomeHistory/OutcomeHistoryDto.cs
public record OutcomeHistoryDto
{
    public Guid WorkflowStepId { get; init; }
    public string WorkflowStepName { get; init; } = null!;
    public int StepOrder { get; init; }
    public OutcomeStatus Outcome { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedByUserId { get; init; }
}
```

**Backend -- ScreeningEndpoints:**

```csharp
// api/src/Web/Endpoints/ScreeningEndpoints.cs
public class ScreeningEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}/candidates/{candidateId:guid}/screening";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/outcome", RecordOutcome);
        group.MapGet("/history", GetOutcomeHistory);
    }

    private async Task<IResult> RecordOutcome(
        Guid recruitmentId, Guid candidateId,
        RecordOutcomeRequest request, ISender sender)
    {
        var command = new RecordOutcomeCommand(
            recruitmentId, candidateId,
            request.WorkflowStepId, request.Outcome, request.Reason);
        var result = await sender.Send(command);
        return Results.Ok(result);
    }

    private async Task<IResult> GetOutcomeHistory(
        Guid recruitmentId, Guid candidateId, ISender sender)
    {
        var query = new GetCandidateOutcomeHistoryQuery(recruitmentId, candidateId);
        var result = await sender.Send(query);
        return Results.Ok(result);
    }
}

public record RecordOutcomeRequest(
    Guid WorkflowStepId,
    OutcomeStatus Outcome,
    string? Reason);
```

**Frontend -- Screening API types:**

```typescript
// web/src/lib/api/screening.types.ts

export type OutcomeStatus = 'NotStarted' | 'Pass' | 'Fail' | 'Hold';

export interface RecordOutcomeRequest {
  workflowStepId: string;
  outcome: OutcomeStatus;
  reason?: string;
}

export interface OutcomeResultDto {
  outcomeId: string;
  candidateId: string;
  workflowStepId: string;
  outcome: OutcomeStatus;
  reason: string | null;
  recordedAt: string;   // ISO 8601
  recordedBy: string;
  newCurrentStepId: string | null;
  isCompleted: boolean;
}

export interface OutcomeHistoryDto {
  workflowStepId: string;
  workflowStepName: string;
  stepOrder: number;
  outcome: OutcomeStatus;
  reason: string | null;
  recordedAt: string;   // ISO 8601
  recordedByUserId: string;
}
```

**Frontend -- Screening API client:**

```typescript
// web/src/lib/api/screening.ts
import { apiGet, apiPost } from './httpClient';
import type {
  RecordOutcomeRequest,
  OutcomeResultDto,
  OutcomeHistoryDto,
} from './screening.types';

export const screeningApi = {
  recordOutcome: (recruitmentId: string, candidateId: string, data: RecordOutcomeRequest) =>
    apiPost<OutcomeResultDto>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/screening/outcome`,
      data
    ),

  getOutcomeHistory: (recruitmentId: string, candidateId: string) =>
    apiGet<OutcomeHistoryDto[]>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/screening/history`
    ),
};
```

**Frontend -- OutcomeForm component structure:**

```typescript
// web/src/features/screening/OutcomeForm.tsx
import { useState } from 'react';
import { ActionButton } from '@/components/ActionButton';
import { StatusBadge } from '@/components/StatusBadge';
import { useAppToast } from '@/components/Toast/useAppToast';
import { useRecordOutcome } from './hooks/useRecordOutcome';
import type { OutcomeHistoryDto, OutcomeStatus } from '@/lib/api/screening.types';

interface OutcomeFormProps {
  recruitmentId: string;
  candidateId: string;
  currentStepId: string;
  currentStepName: string;
  existingOutcome: OutcomeHistoryDto | null;
  isClosed: boolean;
  onOutcomeRecorded?: (result: OutcomeResultDto) => void;
}

export function OutcomeForm({
  recruitmentId,
  candidateId,
  currentStepId,
  currentStepName,
  existingOutcome,
  isClosed,
  onOutcomeRecorded,
}: OutcomeFormProps) {
  const [selectedOutcome, setSelectedOutcome] = useState<OutcomeStatus | null>(
    existingOutcome?.outcome ?? null
  );
  const [reason, setReason] = useState(existingOutcome?.reason ?? '');
  const { toast } = useAppToast();
  const recordOutcome = useRecordOutcome();

  const handleConfirm = () => {
    if (!selectedOutcome) return;
    recordOutcome.mutate(
      { recruitmentId, candidateId, data: { workflowStepId: currentStepId, outcome: selectedOutcome, reason: reason || undefined } },
      {
        onSuccess: (result) => {
          toast({ description: `${selectedOutcome} recorded`, variant: 'success' });
          onOutcomeRecorded?.(result);
        },
      }
    );
  };

  // Render: Pass/Fail/Hold buttons, reason textarea, confirm button
  // All disabled when isClosed
}
```

**Frontend -- useRecordOutcome hook:**

```typescript
// web/src/features/screening/hooks/useRecordOutcome.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { screeningApi } from '@/lib/api/screening';
import type { RecordOutcomeRequest } from '@/lib/api/screening.types';

export function useRecordOutcome() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      recruitmentId,
      candidateId,
      data,
    }: {
      recruitmentId: string;
      candidateId: string;
      data: RecordOutcomeRequest;
    }) => screeningApi.recordOutcome(recruitmentId, candidateId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: ['screening', 'history', variables.candidateId],
      });
      queryClient.invalidateQueries({
        queryKey: ['candidates', variables.recruitmentId],
      });
    },
  });
}
```

**Backend -- EF Core configuration for new properties:**

```csharp
// Extend api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs
builder.Property(c => c.CurrentWorkflowStepId);
builder.Property(c => c.IsCompleted).HasDefaultValue(false);

// Extend CandidateOutcomeConfiguration.cs
builder.Property(o => o.Reason).HasMaxLength(500);
```

**Backend -- Handler authorization pattern (mandatory -- copy from patterns-backend.md):**

```csharp
// BOTH RecordOutcomeCommandHandler AND GetCandidateOutcomeHistoryQueryHandler MUST:
var recruitment = await _context.Recruitments
    .Include(r => r.Members)
    .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
    ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

// MANDATORY: Verify current user is a member
if (!recruitment.Members.Any(m => m.UserId == _tenantContext.UserGuid))
    throw new ForbiddenAccessException();
```

### Architecture Compliance

- **Aggregate root access only:** Call `candidate.RecordOutcome()`. NEVER directly add to `_outcomes` collection or modify `CandidateOutcome` properties.
- **Ubiquitous language:** Use "Outcome" (not result/score/verdict), "Candidate" (not applicant), "Workflow Step" (not stage/phase), "Screening" (not review/evaluation).
- **Manual DTO mapping:** `OutcomeResultDto.From(Candidate entity, Guid stepId)` and `OutcomeHistoryDto` static factory. No AutoMapper.
- **Problem Details for errors:** `RecruitmentClosedException` maps to 400. `InvalidWorkflowTransitionException` maps to 400. `ForbiddenAccessException` maps to 403. `NotFoundException` maps to 404.
- **No PII in audit events/logs:** `OutcomeRecordedEvent` contains only `CandidateId` and `WorkflowStepId` (Guids). No names or reasons in events.
- **NSubstitute for ALL backend mocking** (never Moq).
- **One aggregate per transaction:** Handler loads Candidate, records outcome, saves. Recruitment is loaded read-only for validation/authorization.
- **ITenantContext:** Handler verifies membership via `ITenantContext.UserGuid`. EF global query filters scope candidate data.
- **httpClient.ts as single HTTP entry point:** `screeningApi` uses `apiPost` and `apiGet` from httpClient.ts. Never calls `fetch` directly.
- **EndpointGroupBase for API registration:** `ScreeningEndpoints` inherits from `EndpointGroupBase` with nested resource group path.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Primary constructors for DI in handlers. |
| EF Core | 10.x | Fluent API only. `Include()` for loading outcomes with candidate. New migration for `CurrentWorkflowStepId`, `IsCompleted`, and `Reason` columns. |
| MediatR | 13.x | `IRequest<T>` for commands returning DTOs. Pipeline behaviors for validation. |
| FluentValidation | Latest | Outcome enum validation, reason max length. `AbstractValidator<T>`. |
| NSubstitute | Latest | All handler unit tests. `Substitute.For<IApplicationDbContext>()`, `Substitute.For<ITenantContext>()`. |
| FluentAssertions | Latest | `.Should().Be()`, `.Should().Throw<T>()` in all test assertions. |
| React | 19.x | Controlled form state. `useState` for selection and reason text. |
| TypeScript | 5.7.x | Strict mode. Union type for `OutcomeStatus`. |
| TanStack Query | 5.x | `useMutation` with `isPending` for record outcome. `useQuery` for history. Query invalidation on success. |
| Tailwind CSS | 4.x | Outcome button styling. Color-coded for Pass (green), Fail (red), Hold (amber) -- matching `StatusBadge` palette. |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Screening/
  Commands/
    RecordOutcome/
      RecordOutcomeCommand.cs
      RecordOutcomeCommandValidator.cs
      RecordOutcomeCommandHandler.cs
      OutcomeResultDto.cs
  Queries/
    GetCandidateOutcomeHistory/
      GetCandidateOutcomeHistoryQuery.cs
      GetCandidateOutcomeHistoryQueryHandler.cs
      OutcomeHistoryDto.cs

api/src/Web/Endpoints/
  ScreeningEndpoints.cs

api/tests/Domain.UnitTests/Entities/
  CandidateOutcomeRecordingTests.cs

api/tests/Application.UnitTests/Features/Screening/
  Commands/
    RecordOutcome/
      RecordOutcomeCommandHandlerTests.cs
      RecordOutcomeCommandValidatorTests.cs
  Queries/
    GetCandidateOutcomeHistory/
      GetCandidateOutcomeHistoryQueryHandlerTests.cs

api/tests/Application.FunctionalTests/Endpoints/
  ScreeningEndpointTests.cs

web/src/lib/api/
  screening.ts
  screening.types.ts

web/src/features/screening/
  OutcomeForm.tsx
  OutcomeForm.test.tsx
  OutcomeHistory.tsx
  OutcomeHistory.test.tsx
  hooks/
    useRecordOutcome.ts
    useOutcomeHistory.ts

web/src/mocks/
  screeningHandlers.ts
  fixtures/
    screening.ts
```

**Existing files to modify:**
```
api/src/Domain/Entities/Candidate.cs            -- Extend RecordOutcome(), add CurrentWorkflowStepId, IsCompleted
api/src/Domain/Entities/CandidateOutcome.cs      -- Add Reason property, extend Create() factory
api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs       -- Add CurrentWorkflowStepId, IsCompleted columns
api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs -- Add Reason column with MaxLength(500)
api/tests/Domain.UnitTests/Entities/CandidateTests.cs                      -- Extend with outcome recording tests

web/src/mocks/handlers.ts                        -- Register screening handlers
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**

Domain -- Candidate.RecordOutcome:
- `RecordOutcome_ValidStep_CreatesOutcomeWithReason` -- outcome saved with all fields
- `RecordOutcome_WrongStep_ThrowsInvalidWorkflowTransitionException` -- step 3 attempted when at step 1
- `RecordOutcome_PassNotLastStep_AdvancesToNextStep` -- CurrentWorkflowStepId updated to next step
- `RecordOutcome_PassLastStep_SetsIsCompleted` -- IsCompleted = true, no more steps
- `RecordOutcome_FailOrHold_StaysAtCurrentStep` -- CurrentWorkflowStepId unchanged
- `RecordOutcome_ReRecord_ReplacesExistingOutcome` -- old outcome removed, new added
- `RecordOutcome_NoCurrentStep_ThrowsInvalidOperationException` -- candidate not assigned to workflow
- `RecordOutcome_RaisesOutcomeRecordedEvent` -- domain event raised

RecordOutcomeCommand handler:
- `Handle_ValidOutcome_RecordsAndReturnsDto` -- full success path
- `Handle_ClosedRecruitment_ThrowsRecruitmentClosedException`
- `Handle_NonMemberUser_ThrowsForbiddenAccessException`
- `Handle_RecruitmentNotFound_ThrowsNotFoundException`
- `Handle_StepNotInRecruitment_ThrowsNotFoundException`
- `Handle_CandidateNotFound_ThrowsNotFoundException`
- `Handle_WrongStepSequence_ThrowsInvalidWorkflowTransitionException`

RecordOutcomeCommand validator:
- `Validate_NotStartedOutcome_Fails` -- NotStarted is not a valid recording choice
- `Validate_EmptyRecruitmentId_Fails`
- `Validate_ReasonExceeds500Chars_Fails`
- `Validate_ValidCommand_Passes`

GetCandidateOutcomeHistoryQuery handler:
- `Handle_ValidRequest_ReturnsOrderedHistory` -- outcomes joined with step names, ordered
- `Handle_NonMemberUser_ThrowsForbiddenAccessException`
- `Handle_CandidateNotFound_ThrowsNotFoundException`
- `Handle_NoOutcomes_ReturnsEmptyList`

Integration tests (API endpoints):
- `RecordOutcome_ValidRequest_Returns200WithOutcomeResultDto`
- `RecordOutcome_ClosedRecruitment_Returns400ProblemDetails`
- `RecordOutcome_InvalidStepSequence_Returns400ProblemDetails`
- `RecordOutcome_NotStartedOutcome_Returns400ProblemDetails`
- `GetOutcomeHistory_ValidRequest_ReturnsOrderedList`

**Frontend tests (Vitest + Testing Library + MSW):**

OutcomeForm:
- "should display Pass, Fail, Hold buttons"
- "should show reason textarea always visible"
- "should disable confirm button when no outcome selected"
- "should enable confirm button when outcome selected"
- "should call API with correct parameters on confirm"
- "should pre-fill form with existing outcome"
- "should disable all controls when recruitment is closed"
- "should show success feedback after recording"
- "should disable confirm button while mutation is pending"

OutcomeHistory:
- "should display outcome history ordered by step"
- "should show reason text when provided"
- "should display empty state when no outcomes recorded"
- "should show status badge with correct color for each outcome"
- "should display recorded date and user"

MSW handlers:
- `POST /api/recruitments/:recruitmentId/candidates/:candidateId/screening/outcome` -- returns 200 with OutcomeResultDto
- `GET /api/recruitments/:recruitmentId/candidates/:candidateId/screening/history` -- returns 200 with OutcomeHistoryDto[]

### Previous Story Intelligence

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- use `apiPost` for JSON, `apiGet` for queries
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- `Candidate` entity exists with `RecordOutcome()` method (simple version) -- this story extends it significantly
- `CandidateOutcome` has `internal` constructor -- only creatable through `CandidateOutcome.Create()` factory
- `ApplicationDbContext` constructor requires `ITenantContext` -- mock it in tests
- MediatR 13: `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`

**From Story 1.4 (Shared UI Components):**
- 18 shadcn/ui components already installed in `web/src/components/ui/`
- `useAppToast()` hook for toast notifications (3-second auto-dismiss for success)
- `StatusBadge` component with Pass (green), Fail (red), Hold (amber) variants
- `ActionButton` component with Primary/Secondary/Destructive variants
- `cn()` utility in `web/src/lib/utils.ts` for className merging

**From Story 2.1 (Create Recruitment):**
- CQRS folder structure established: one command per folder with Command, Validator, Handler
- Frontend API client pattern established in `web/src/lib/api/recruitments.ts`
- TanStack Query mutation pattern established

**From Story 2.5 (Close Recruitment):**
- `RecruitmentClosedException` maps to 400 Problem Details via global exception middleware
- `isClosed` derived from `recruitment.status === 'Closed'` -- same pattern used here to disable outcome controls

**From Story 3.1 (Manual Candidate Management):**
- `Candidate` aggregate operations established -- follow same handler patterns
- `CandidateEndpoints.cs` created with base routes
- `candidates.ts` and `candidates.types.ts` API client created

**From Story 3.5 (CV Auto-Match):**
- `Candidate.ReplaceDocument()` shows how the aggregate root extends methods while preserving backward compatibility
- Handler authorization pattern consistently applied: load recruitment with members, verify user, verify not closed
- `ForbiddenAccessException` confirmed as the standard exception for non-member access
- NSubstitute pattern for mocking `IApplicationDbContext` with `DbSet<T>` returns

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(4.3): extend CandidateOutcome with Reason property + EF config`
2. `feat(4.3): enhance Candidate.RecordOutcome with workflow enforcement + domain tests`
3. `feat(4.3): add RecordOutcomeCommand with handler + validator + unit tests`
4. `feat(4.3): add GetCandidateOutcomeHistoryQuery with handler + unit tests`
5. `feat(4.3): add ScreeningEndpoints with integration tests`
6. `feat(4.3): add screening API client types and methods`
7. `feat(4.3): add OutcomeForm component with tests`
8. `feat(4.3): add OutcomeHistory component with tests`
9. `feat(4.3): add MSW screening handlers and fixtures`

### Latest Tech Information

- **.NET 10.0:** Primary constructors for DI injection in handlers (e.g., `public class Handler(IApplicationDbContext context, ITenantContext tenantContext)`). Record types for commands and DTOs.
- **EF Core 10:** New migration needed for `CurrentWorkflowStepId` (nullable Guid), `IsCompleted` (bool default false), and `Reason` (nvarchar(500)) columns. Use `dotnet ef migrations add AddOutcomeWorkflowEnforcement`.
- **MediatR 13.x:** `IRequest<T>` for commands returning DTOs. `IRequest` for void commands. Pipeline behaviors validate before handler executes.
- **FluentValidation:** `IsInEnum()` for enum validation. `Must()` for custom rules. `.When()` for conditional validation.
- **React 19.2:** Controlled form state with `useState`. No major changes affecting this story.
- **TanStack Query 5.90.x:** `useMutation` with `isPending` (not `isLoading`). `queryClient.invalidateQueries()` with query key array. Cache invalidation cascading to candidate list queries.
- **Tailwind CSS 4.x:** Use semantic color classes for outcome buttons: `bg-green-600` (Pass), `bg-red-600` (Fail), `bg-amber-500` (Hold). Match `StatusBadge` palette.

### Project Structure Notes

- Alignment with unified project structure: all paths follow Clean Architecture (`api/`) + Vite React (`web/`) split
- `Screening` feature folder is NEW in both backend (`Application/Features/Screening/`) and frontend (`features/screening/`)
- `ScreeningEndpoints.cs` uses nested resource path under recruitments/candidates -- consistent with document access boundary pattern
- `OutcomeForm.tsx` lives in `features/screening/` -- this is the screening feature, not candidates
- Test files mirror source structure: `Application.UnitTests/Features/Screening/Commands/RecordOutcome/` matches source path
- Frontend tests co-locate with source: `OutcomeForm.test.tsx` next to `OutcomeForm.tsx`

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-screening-outcome-recording.md` -- Story 4.3 acceptance criteria, FR mapping, technical notes]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries (Candidate owns CandidateOutcome), ITenantContext, enforcement guidelines, OutcomeStatus enum]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, handler authorization, DTO mapping, test conventions, transient domain state]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, component structure, StatusBadge patterns, toast patterns, loading states]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Response formats, Problem Details, Minimal API endpoint registration, EndpointGroupBase]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, screening feature paths, requirements mapping (FR37-40)]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- API client contract pattern, httpClient foundation, batch screening architecture]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Test frameworks (NUnit/NSubstitute/FluentAssertions), naming conventions, pragmatic TDD modes]
- [Source: `api/src/Domain/Entities/Candidate.cs` -- Existing RecordOutcome() method (line 48-53), outcomes collection, ReplaceDocument() pattern for reference]
- [Source: `api/src/Domain/Entities/CandidateOutcome.cs` -- Existing entity with internal Create() factory, current properties]
- [Source: `api/src/Domain/Entities/WorkflowStep.cs` -- Step entity with Order property for sequencing]
- [Source: `api/src/Domain/Entities/Recruitment.cs` -- Steps collection, EnsureNotClosed() pattern, member verification]
- [Source: `api/src/Domain/Enums/OutcomeStatus.cs` -- NotStarted, Pass, Fail, Hold enum values]
- [Source: `api/src/Domain/Exceptions/InvalidWorkflowTransitionException.cs` -- Existing exception with from/to string parameters]
- [Source: `api/src/Domain/Events/OutcomeRecordedEvent.cs` -- Existing event with CandidateId and WorkflowStepId]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy, mode declarations]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List

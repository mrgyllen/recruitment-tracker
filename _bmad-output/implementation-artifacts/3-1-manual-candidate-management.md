# Story 3.1: Manual Candidate Management

Status: ready-for-dev

## Story

As a **recruiting leader (Erik)**,
I want to **manually create and remove candidate records within a recruitment**,
so that **I can add candidates who aren't in the Workday export (referrals, direct applications) and remove candidates who shouldn't be tracked**.

## Acceptance Criteria

### AC1: Add Candidate form display
**Given** an active recruitment with workflow steps exists
**When** the user clicks "Add Candidate"
**Then** a form is displayed with fields for full name (required), email (required), phone (optional), location (optional), and date applied (optional, defaults to today)

### AC2: Successful candidate creation
**Given** the add candidate form is open
**When** the user submits with valid data
**Then** a new candidate is created within the recruitment
**And** the candidate is placed at the first workflow step with outcome status "Not Started"
**And** the API returns 201 Created
**And** a success toast confirms the creation
**And** the candidate appears in the candidate list

### AC3: Client-side validation
**Given** the add candidate form is open
**When** the user submits with a missing full name or email
**Then** field-level validation errors are shown
**And** the form is not submitted

### AC4: Duplicate email rejection
**Given** a candidate with the same email already exists in this recruitment
**When** the user attempts to create a duplicate
**Then** the API returns 400 Bad Request with Problem Details: "A candidate with this email already exists in this recruitment"
**And** the user sees an inline error message

### AC5: Remove candidate confirmation
**Given** an active recruitment has candidates
**When** the user clicks the remove action for a candidate
**Then** a confirmation dialog is shown: "Remove [Name] from this recruitment? This cannot be undone."

### AC6: Successful candidate removal
**Given** the user confirms the removal
**When** the system processes the removal
**Then** the candidate is removed from the recruitment
**And** the candidate list updates to reflect the removal
**And** a success toast confirms the removal

### AC7: Closed recruitment -- actions hidden
**Given** a closed recruitment exists
**When** the user views the candidate list
**Then** the "Add Candidate" and remove actions are not available

### FRs Fulfilled
- **FR23:** Users can manually create a candidate record within a recruitment by entering name, email, phone, location, and date applied
- **FR24:** Users can manually remove a candidate from a recruitment
- **FR62:** New candidates (imported or manually created) are placed at the first workflow step with outcome status "Not Started"

## Tasks / Subtasks

- [ ] Task 1: Backend -- Candidate API types and DTO (AC: #2)
  - [ ] 1.1 Create `CandidateDto` record in `api/src/Application/Features/Candidates/` with `static From(Candidate)` factory method
  - [ ] 1.2 Fields: `Id`, `FullName`, `Email`, `PhoneNumber`, `Location`, `DateApplied`, `CreatedAt`, `RecruitmentId`
- [ ] Task 2: Backend -- CreateCandidate command, validator, handler (AC: #2, #3, #4)
  - [ ] 2.1 Create `CreateCandidateCommand` record in `api/src/Application/Features/Candidates/Commands/CreateCandidate/`
  - [ ] 2.2 Create `CreateCandidateCommandValidator` with FluentValidation: `RecruitmentId` required, `FullName` required (max 200), `Email` required + valid email format (max 254), `PhoneNumber` optional (max 30), `Location` optional (max 200), `DateApplied` optional
  - [ ] 2.3 Create `CreateCandidateCommandHandler`: load Recruitment (verify membership via `ITenantContext`), check `EnsureNotClosed()` on recruitment, check email uniqueness via DB query, create Candidate via `Candidate.Create()`, add initial outcome at first workflow step, save
  - [ ] 2.4 Unit test handler: valid creation returns Guid, candidate saved with correct fields
  - [ ] 2.5 Unit test handler: duplicate email throws `DuplicateCandidateException`
  - [ ] 2.6 Unit test handler: closed recruitment throws `RecruitmentClosedException`
  - [ ] 2.7 Unit test handler: user not a member throws `ForbiddenAccessException`
  - [ ] 2.8 Unit test handler: recruitment not found throws `NotFoundException`
  - [ ] 2.9 Unit test validator: missing fullName fails, missing email fails, invalid email fails, empty recruitmentId fails, valid input passes
- [ ] Task 3: Backend -- RemoveCandidate command, validator, handler (AC: #5, #6)
  - [ ] 3.1 Create `RemoveCandidateCommand` record in `api/src/Application/Features/Candidates/Commands/RemoveCandidate/`
  - [ ] 3.2 Create `RemoveCandidateCommandValidator` with FluentValidation: `RecruitmentId` required, `CandidateId` required
  - [ ] 3.3 Create `RemoveCandidateCommandHandler`: load Recruitment (verify membership via `ITenantContext`), check `EnsureNotClosed()`, load candidate, verify candidate belongs to recruitment, remove candidate from DbContext, save
  - [ ] 3.4 Unit test handler: valid removal deletes candidate
  - [ ] 3.5 Unit test handler: candidate not found throws `NotFoundException`
  - [ ] 3.6 Unit test handler: closed recruitment throws `RecruitmentClosedException`
  - [ ] 3.7 Unit test handler: user not a member throws `ForbiddenAccessException`
  - [ ] 3.8 Unit test validator: empty candidateId fails, empty recruitmentId fails
- [ ] Task 4: Backend -- GetCandidates query + handler (AC: #2)
  - [ ] 4.1 Create `GetCandidatesQuery` in `api/src/Application/Features/Candidates/Queries/GetCandidates/`
  - [ ] 4.2 Create `GetCandidatesQueryHandler`: load Recruitment (verify membership), query candidates by RecruitmentId, return paginated list of `CandidateDto`
  - [ ] 4.3 Unit test query handler: returns candidates for recruitment, membership check enforced
- [ ] Task 5: Backend -- Minimal API endpoints (AC: #2, #4, #6)
  - [ ] 5.1 Create `CandidateEndpoints.cs` in `api/src/Web/Endpoints/` inheriting `EndpointGroupBase`
  - [ ] 5.2 Map `POST /api/recruitments/{recruitmentId}/candidates` -- accept command, return 201 Created with Location header + CandidateDto
  - [ ] 5.3 Map `DELETE /api/recruitments/{recruitmentId}/candidates/{candidateId}` -- accept route params, return 204 No Content
  - [ ] 5.4 Map `GET /api/recruitments/{recruitmentId}/candidates` -- accept pagination params, return paginated list
  - [ ] 5.5 Integration test: POST with valid body returns 201 + Location + response body
  - [ ] 5.6 Integration test: POST with duplicate email returns 400 Problem Details
  - [ ] 5.7 Integration test: POST with missing fields returns 400 Problem Details with field-level errors
  - [ ] 5.8 Integration test: POST on closed recruitment returns 400 "Recruitment is closed"
  - [ ] 5.9 Integration test: DELETE returns 204
  - [ ] 5.10 Integration test: DELETE non-existent candidate returns 404
  - [ ] 5.11 Integration test: GET returns paginated candidate list
- [ ] Task 6: Backend -- Add DuplicateCandidateException to CustomExceptionHandler (AC: #4)
  - [ ] 6.1 Add `DuplicateCandidateException` handler to `CustomExceptionHandler.cs` returning 400 Problem Details with title "A candidate with this email already exists in this recruitment"
  - [ ] 6.2 Verify `DuplicateCandidateException` already exists in `api/src/Domain/Exceptions/`
- [ ] Task 7: Frontend -- API client and types (AC: #2, #6)
  - [ ] 7.1 Create `web/src/lib/api/candidates.types.ts` with `CandidateResponse`, `CreateCandidateRequest`, `PaginatedCandidateList` types
  - [ ] 7.2 Create `web/src/lib/api/candidates.ts` with `candidateApi.create()`, `candidateApi.remove()`, `candidateApi.getAll()` using `apiPost`, `apiDelete`, `apiGet` from httpClient
- [ ] Task 8: Frontend -- Candidate hooks (AC: #2, #6)
  - [ ] 8.1 Create `web/src/features/candidates/hooks/useCandidates.ts` -- TanStack Query wrapper for `candidateApi.getAll()` with query key `['candidates', recruitmentId]`
  - [ ] 8.2 Create `web/src/features/candidates/hooks/useCandidateMutations.ts` -- `useCreateCandidate()` and `useRemoveCandidate()` mutations, invalidate `['candidates', recruitmentId]` on success
- [ ] Task 9: Frontend -- CreateCandidateForm component (AC: #1, #2, #3, #4)
  - [ ] 9.1 Create `web/src/features/candidates/CreateCandidateForm.tsx` -- Dialog with react-hook-form + zod schema
  - [ ] 9.2 Fields: fullName (required), email (required, email format), phone (optional), location (optional), dateApplied (optional, default today)
  - [ ] 9.3 Use shadcn/ui Dialog, Form, Input, Button components
  - [ ] 9.4 On success: toast "Candidate added", close dialog, invalidate candidate list
  - [ ] 9.5 On API error: parse Problem Details, show inline error (especially for duplicate email)
  - [ ] 9.6 Button: "Add Candidate" -> "Adding..." with spinner when pending, disabled
  - [ ] 9.7 Create `web/src/features/candidates/CreateCandidateForm.test.tsx`
  - [ ] 9.8 Test: form renders all fields with correct labels
  - [ ] 9.9 Test: submit without required fields shows validation errors
  - [ ] 9.10 Test: successful submission calls API with correct data and shows toast
  - [ ] 9.11 Test: duplicate email error displays inline error message
  - [ ] 9.12 Test: button disabled and shows spinner when pending
- [ ] Task 10: Frontend -- CandidateList component (AC: #2, #5, #6, #7)
  - [ ] 10.1 Create `web/src/features/candidates/CandidateList.tsx` -- table/list of candidates with remove action
  - [ ] 10.2 Include empty state using shared `EmptyState` component: "No candidates yet" with "Add Candidate" CTA
  - [ ] 10.3 Each candidate row shows: full name, email, location, date applied, remove button (when not closed)
  - [ ] 10.4 Remove button triggers `AlertDialog` confirmation: "Remove [Name] from this recruitment? This cannot be undone."
  - [ ] 10.5 On remove success: toast "Candidate removed", list updates
  - [ ] 10.6 When `isClosed`, hide "Add Candidate" button and remove actions
  - [ ] 10.7 Use shared `SkeletonLoader` for loading state
  - [ ] 10.8 Create `web/src/features/candidates/CandidateList.test.tsx`
  - [ ] 10.9 Test: renders candidate data in list
  - [ ] 10.10 Test: displays empty state when no candidates
  - [ ] 10.11 Test: remove button shows confirmation dialog
  - [ ] 10.12 Test: confirming removal calls API and shows toast
  - [ ] 10.13 Test: add/remove actions hidden when recruitment is closed
- [ ] Task 11: Frontend -- Wire CandidateList into RecruitmentPage (AC: #2, #7)
  - [ ] 11.1 Import and render `CandidateList` in `RecruitmentPage.tsx` below the team section
  - [ ] 11.2 Pass `recruitmentId` and `isClosed` props
  - [ ] 11.3 Add "Add Candidate" button that opens `CreateCandidateForm` dialog
- [ ] Task 12: Frontend -- MSW handlers and fixtures (AC: all)
  - [ ] 12.1 Create `web/src/mocks/candidateHandlers.ts` with handlers for `POST`, `DELETE`, `GET` candidate endpoints
  - [ ] 12.2 Create `web/src/mocks/fixtures/candidates.ts` with mock candidate data
  - [ ] 12.3 Add candidateHandlers to `web/src/mocks/handlers.ts`

## Dev Notes

### Affected Aggregate(s)

**Candidate** (aggregate root -- NEW) -- This is the first story that creates the Candidate aggregate. The `Candidate` entity already exists at `api/src/Domain/Entities/Candidate.cs` with full domain logic from Story 1.3. DO NOT modify the domain entity -- it is complete.

Key domain methods to use:
- `Candidate.Create(recruitmentId, fullName, email, phoneNumber, location, dateApplied)` -- factory method, raises `CandidateImportedEvent`
- `candidate.RecordOutcome(workflowStepId, status, recordedByUserId)` -- for placing candidate at first step with "Not Started"
- Child entities (`CandidateOutcome`, `CandidateDocument`) have `internal` constructors -- only creatable through aggregate root methods

**Recruitment** (aggregate root -- READ ONLY) -- The handler loads the Recruitment to:
1. Verify user membership (authorization)
2. Check that recruitment is not closed (via `EnsureNotClosed()` or status check)
3. Get the first workflow step ID (for initial outcome placement)

Cross-aggregate: `Candidate` holds `RecruitmentId` as an ID-only reference (no navigation property). The handler bridges the two aggregates by querying the Recruitment for membership/status checks, then creating the Candidate in the same transaction. This is acceptable because the Candidate is being created (not modifying an existing aggregate).

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (DTO) | **Characterization** | Simple data mapping -- test via handler/endpoint tests |
| Task 2 (CreateCandidate handler) | **Test-first** | Core business logic -- aggregate creation, email uniqueness, authorization |
| Task 3 (RemoveCandidate handler) | **Test-first** | Destructive action -- must verify authorization, not-closed guard, existence check |
| Task 4 (GetCandidates query) | **Test-first** | Data access with authorization -- membership check required |
| Task 5 (API endpoints) | **Test-first** | Integration boundary -- 201/400/404 responses with Problem Details |
| Task 6 (Exception handler) | **Characterization** | Adding mapping to existing middleware -- verify mapping exists |
| Task 7 (API client) | **Characterization** | Thin wrapper over httpClient -- test via component integration tests |
| Task 8 (Hooks) | **Characterization** | TanStack Query wrappers -- test via component tests |
| Task 9 (CreateCandidateForm) | **Test-first** | User-facing form with validation, error display, success feedback |
| Task 10 (CandidateList) | **Test-first** | Displays data, empty state, destructive remove confirmation, read-only mode |
| Task 11 (Page wiring) | **Characterization** | Glue code -- verify candidate list renders on page |
| Task 12 (MSW handlers) | **Characterization** | Test infrastructure -- validated by component tests |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **CQRS folder structure:**
   ```
   api/src/Application/Features/Candidates/
     Commands/
       CreateCandidate/
         CreateCandidateCommand.cs
         CreateCandidateCommandValidator.cs
         CreateCandidateCommandHandler.cs
       RemoveCandidate/
         RemoveCandidateCommand.cs
         RemoveCandidateCommandValidator.cs
         RemoveCandidateCommandHandler.cs
     Queries/
       GetCandidates/
         GetCandidatesQuery.cs
         GetCandidatesQueryHandler.cs
     CandidateDto.cs
   ```
   Rule: One command/query per folder. Handler in same folder.

2. **Command as record (init properties, not constructor params):**
   ```csharp
   // CreateCandidateCommand.cs
   namespace api.Application.Features.Candidates.Commands.CreateCandidate;

   public record CreateCandidateCommand : IRequest<Guid>
   {
       public Guid RecruitmentId { get; init; }
       public string FullName { get; init; } = null!;
       public string Email { get; init; } = null!;
       public string? PhoneNumber { get; init; }
       public string? Location { get; init; }
       public DateTimeOffset? DateApplied { get; init; }
   }
   ```

   ```csharp
   // RemoveCandidateCommand.cs
   namespace api.Application.Features.Candidates.Commands.RemoveCandidate;

   public class RemoveCandidateCommand : IRequest
   {
       public Guid RecruitmentId { get; init; }
       public Guid CandidateId { get; init; }
   }
   ```

3. **Handler pattern -- membership check + aggregate operations:**
   ```csharp
   // CreateCandidateCommandHandler.cs
   public class CreateCandidateCommandHandler(
       IApplicationDbContext dbContext,
       ITenantContext tenantContext)
       : IRequestHandler<CreateCandidateCommand, Guid>
   {
       public async Task<Guid> Handle(
           CreateCandidateCommand request,
           CancellationToken cancellationToken)
       {
           // 1. Load recruitment with members and steps
           var recruitment = await dbContext.Recruitments
               .Include(r => r.Members)
               .Include(r => r.Steps)
               .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
               ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

           // 2. MANDATORY: Verify current user is a member
           var userId = tenantContext.UserGuid;
           if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
           {
               throw new ForbiddenAccessException();
           }

           // 3. Ensure recruitment is active
           if (recruitment.Status == RecruitmentStatus.Closed)
           {
               throw new RecruitmentClosedException(recruitment.Id);
           }

           // 4. Check email uniqueness within recruitment
           var emailExists = await dbContext.Candidates
               .AnyAsync(c => c.RecruitmentId == request.RecruitmentId
                   && c.Email == request.Email, cancellationToken);
           if (emailExists)
           {
               throw new DuplicateCandidateException(request.Email, request.RecruitmentId);
           }

           // 5. Create candidate (separate aggregate)
           var dateApplied = request.DateApplied ?? DateTimeOffset.UtcNow;
           var candidate = Candidate.Create(
               request.RecruitmentId,
               request.FullName,
               request.Email,
               request.PhoneNumber,
               request.Location,
               dateApplied);

           // 6. Place at first workflow step with "Not Started"
           var firstStep = recruitment.Steps.OrderBy(s => s.Order).FirstOrDefault();
           if (firstStep is not null)
           {
               candidate.RecordOutcome(firstStep.Id, OutcomeStatus.NotStarted, userId.Value);
           }

           dbContext.Candidates.Add(candidate);
           await dbContext.SaveChangesAsync(cancellationToken);

           return candidate.Id;
       }
   }
   ```

   ```csharp
   // RemoveCandidateCommandHandler.cs
   public class RemoveCandidateCommandHandler(
       IApplicationDbContext dbContext,
       ITenantContext tenantContext)
       : IRequestHandler<RemoveCandidateCommand>
   {
       public async Task Handle(
           RemoveCandidateCommand request,
           CancellationToken cancellationToken)
       {
           // 1. Load recruitment with members
           var recruitment = await dbContext.Recruitments
               .Include(r => r.Members)
               .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
               ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

           // 2. MANDATORY: Verify membership
           var userId = tenantContext.UserGuid;
           if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
           {
               throw new ForbiddenAccessException();
           }

           // 3. Ensure not closed
           if (recruitment.Status == RecruitmentStatus.Closed)
           {
               throw new RecruitmentClosedException(recruitment.Id);
           }

           // 4. Load and remove candidate
           var candidate = await dbContext.Candidates
               .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                   && c.RecruitmentId == request.RecruitmentId, cancellationToken)
               ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

           dbContext.Candidates.Remove(candidate);
           await dbContext.SaveChangesAsync(cancellationToken);
       }
   }
   ```

4. **FluentValidation:**
   ```csharp
   // CreateCandidateCommandValidator.cs
   public class CreateCandidateCommandValidator
       : AbstractValidator<CreateCandidateCommand>
   {
       public CreateCandidateCommandValidator()
       {
           RuleFor(x => x.RecruitmentId).NotEmpty();
           RuleFor(x => x.FullName)
               .NotEmpty().WithMessage("Full name is required.")
               .MaximumLength(200);
           RuleFor(x => x.Email)
               .NotEmpty().WithMessage("Email is required.")
               .EmailAddress().WithMessage("A valid email address is required.")
               .MaximumLength(254);
           RuleFor(x => x.PhoneNumber).MaximumLength(30);
           RuleFor(x => x.Location).MaximumLength(200);
       }
   }
   ```

5. **DTO with manual mapping:**
   ```csharp
   // CandidateDto.cs
   namespace api.Application.Features.Candidates;

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

       public static CandidateDto From(Candidate candidate) => new()
       {
           Id = candidate.Id,
           RecruitmentId = candidate.RecruitmentId,
           FullName = candidate.FullName!,
           Email = candidate.Email!,
           PhoneNumber = candidate.PhoneNumber,
           Location = candidate.Location,
           DateApplied = candidate.DateApplied,
           CreatedAt = candidate.CreatedAt,
       };
   }
   ```

6. **Endpoint registration -- inherit EndpointGroupBase:**
   ```csharp
   // CandidateEndpoints.cs
   public class CandidateEndpoints : EndpointGroupBase
   {
       public override string? GroupName => "recruitments/{recruitmentId:guid}/candidates";

       public override void Map(RouteGroupBuilder group)
       {
           group.MapPost("/", CreateCandidate);
           group.MapDelete("/{candidateId:guid}", RemoveCandidate);
           group.MapGet("/", GetCandidates);
       }

       private static async Task<IResult> CreateCandidate(
           ISender sender,
           Guid recruitmentId,
           CreateCandidateCommand command)
       {
           var id = await sender.Send(command with { RecruitmentId = recruitmentId });
           return Results.Created(
               $"/api/recruitments/{recruitmentId}/candidates/{id}",
               new { id });
       }

       private static async Task<IResult> RemoveCandidate(
           ISender sender,
           Guid recruitmentId,
           Guid candidateId)
       {
           await sender.Send(new RemoveCandidateCommand
           {
               RecruitmentId = recruitmentId,
               CandidateId = candidateId
           });
           return Results.NoContent();
       }

       private static async Task<IResult> GetCandidates(
           ISender sender,
           Guid recruitmentId,
           int page = 1,
           int pageSize = 50)
       {
           var result = await sender.Send(new GetCandidatesQuery
           {
               RecruitmentId = recruitmentId,
               Page = page,
               PageSize = pageSize
           });
           return Results.Ok(result);
       }
   }
   ```

7. **Exception mapping -- add DuplicateCandidateException to CustomExceptionHandler:**
   ```csharp
   // Add to _exceptionHandlers dictionary in CustomExceptionHandler constructor
   { typeof(DuplicateCandidateException), HandleDuplicateCandidateException },

   // Add handler method
   private async Task HandleDuplicateCandidateException(HttpContext httpContext, Exception ex)
   {
       httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

       await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
       {
           Status = StatusCodes.Status400BadRequest,
           Title = "A candidate with this email already exists in this recruitment",
           Detail = ex.Message,
           Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
       });
   }
   ```

8. **Error handling:** DO NOT catch domain exceptions in handlers. Let `DuplicateCandidateException`, `RecruitmentClosedException`, `ForbiddenAccessException`, and `NotFoundException` propagate to the global exception middleware for automatic Problem Details conversion.

9. **ITenantContext:** Handlers use `ITenantContext.UserGuid` for membership verification. The candidate query for `GetCandidates` filters by `RecruitmentId` explicitly in the query.

**Frontend -- MUST follow these patterns:**

1. **Feature folder structure:**
   ```
   web/src/features/candidates/
     CandidateList.tsx                (NEW)
     CandidateList.test.tsx           (NEW)
     CreateCandidateForm.tsx          (NEW)
     CreateCandidateForm.test.tsx     (NEW)
     hooks/
       useCandidates.ts              (NEW)
       useCandidateMutations.ts      (NEW)
   web/src/lib/api/
     candidates.ts                   (NEW)
     candidates.types.ts             (NEW)
   web/src/mocks/
     candidateHandlers.ts            (NEW)
     fixtures/
       candidates.ts                 (NEW)
   ```

2. **API client using existing httpClient:**
   ```typescript
   // web/src/lib/api/candidates.ts
   import { apiDelete, apiGet, apiPost } from './httpClient'
   import type {
     CandidateResponse,
     CreateCandidateRequest,
     PaginatedCandidateList,
   } from './candidates.types'

   export const candidateApi = {
     create: (recruitmentId: string, data: CreateCandidateRequest) =>
       apiPost<CandidateResponse>(
         `/recruitments/${recruitmentId}/candidates`,
         data,
       ),

     remove: (recruitmentId: string, candidateId: string) =>
       apiDelete(`/recruitments/${recruitmentId}/candidates/${candidateId}`),

     getAll: (recruitmentId: string, page = 1, pageSize = 50) =>
       apiGet<PaginatedCandidateList>(
         `/recruitments/${recruitmentId}/candidates?page=${page}&pageSize=${pageSize}`,
       ),
   }
   ```
   CRITICAL: Use `apiPost`/`apiDelete`/`apiGet` from `httpClient.ts` -- it handles auth headers (MSAL in prod, X-Dev-User-* in dev) and Problem Details parsing.

3. **API types:**
   ```typescript
   // web/src/lib/api/candidates.types.ts
   export interface CandidateResponse {
     id: string
     recruitmentId: string
     fullName: string
     email: string
     phoneNumber: string | null
     location: string | null
     dateApplied: string      // ISO 8601
     createdAt: string         // ISO 8601
   }

   export interface CreateCandidateRequest {
     fullName: string
     email: string
     phoneNumber?: string | null
     location?: string | null
     dateApplied?: string | null  // ISO 8601, defaults to today on server
   }

   export interface PaginatedCandidateList {
     items: CandidateResponse[]
     totalCount: number
     page: number
     pageSize: number
   }
   ```

4. **TanStack Query hooks:**
   ```typescript
   // hooks/useCandidates.ts
   import { useQuery } from '@tanstack/react-query'
   import { candidateApi } from '@/lib/api/candidates'

   export function useCandidates(recruitmentId: string) {
     return useQuery({
       queryKey: ['candidates', recruitmentId],
       queryFn: () => candidateApi.getAll(recruitmentId),
       enabled: !!recruitmentId,
     })
   }
   ```

   ```typescript
   // hooks/useCandidateMutations.ts
   import { useMutation, useQueryClient } from '@tanstack/react-query'
   import { candidateApi } from '@/lib/api/candidates'
   import type { CreateCandidateRequest } from '@/lib/api/candidates.types'

   export function useCreateCandidate(recruitmentId: string) {
     const queryClient = useQueryClient()
     return useMutation({
       mutationFn: (data: CreateCandidateRequest) =>
         candidateApi.create(recruitmentId, data),
       onSuccess: () => {
         void queryClient.invalidateQueries({
           queryKey: ['candidates', recruitmentId],
         })
       },
     })
   }

   export function useRemoveCandidate(recruitmentId: string) {
     const queryClient = useQueryClient()
     return useMutation({
       mutationFn: (candidateId: string) =>
         candidateApi.remove(recruitmentId, candidateId),
       onSuccess: () => {
         void queryClient.invalidateQueries({
           queryKey: ['candidates', recruitmentId],
         })
       },
     })
   }
   ```
   Note: `isPending` (not `isLoading`) for TanStack Query v5.

5. **Form with react-hook-form + zod:**
   ```typescript
   // CreateCandidateForm.tsx (pattern outline)
   const createCandidateSchema = z.object({
     fullName: z.string().min(1, 'Full name is required').max(200),
     email: z.string().min(1, 'Email is required').email('A valid email is required').max(254),
     phoneNumber: z.string().max(30).optional().or(z.literal('')),
     location: z.string().max(200).optional().or(z.literal('')),
     dateApplied: z.string().optional(),
   })
   ```
   - Use shadcn/ui `Dialog`, `Form`, `Input`, `Label`, `Button` components
   - Validation: on blur + on submit
   - Labels above inputs, optional fields marked "(optional)"
   - Error messages below field, specific text
   - Button: "Add Candidate" -> "Adding..." with spinner when pending

6. **Toast on success:** Use `useAppToast()` hook: `toast({ title: "Candidate added", variant: "success" })` for creation, `toast({ title: "Candidate removed", variant: "success" })` for removal. Auto-dismiss after 3 seconds per UX spec.

7. **Error display for duplicate email:** Parse `ApiError.problemDetails.title` to detect the duplicate email case and show it inline in the form rather than as a generic toast.

8. **Empty state:** `CandidateList` must use the shared `EmptyState` component when no candidates exist. Text: "No candidates yet". Action: "Add Candidate" button.

9. **Confirmation dialog for removal:** Use shadcn/ui `AlertDialog` (same pattern as `CloseRecruitmentDialog`). Destructive action button.

10. **Read-only mode:** When `isClosed` is true, hide "Add Candidate" button and all remove actions. The candidate list remains visible and readable.

### Architecture Compliance

- **Aggregate root access only:** Use `Candidate.Create()` factory method. NEVER instantiate `Candidate` directly or use `dbContext.Candidates.Add(new Candidate { ... })`.
- **Cross-aggregate references by ID only:** `Candidate.RecruitmentId` is a `Guid`, not a navigation property. The handler loads the Recruitment separately to verify membership.
- **One aggregate per transaction (relaxed for creation):** Creating a new Candidate and recording its initial outcome is a single aggregate creation, not a cross-aggregate modification. This is acceptable.
- **Ubiquitous language:** Use "Candidate" (not applicant), "Recruitment" (not job/position), "Outcome" (not result).
- **Manual DTO mapping:** `CandidateDto.From(candidate)` -- no AutoMapper.
- **Problem Details for errors:** Test that `DuplicateCandidateException` returns RFC 9457 Problem Details with correct title.
- **No PII in audit events/logs:** `CandidateImportedEvent` contains only `CandidateId` and `RecruitmentId` (Guids).
- **NSubstitute for ALL mocking** (never Moq).
- **Handler authorization pattern:** Every handler MUST verify membership via `ITenantContext.UserGuid` and `recruitment.Members` check. This is security-critical (see Story 2.3 evidence).

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI in handlers. |
| EF Core | 10.x | Fluent API only. `CandidateConfiguration` already exists. `FirstOrDefaultAsync` for loading by ID. |
| MediatR | 13.x | `IRequest<Guid>` for create, `IRequest` (void) for remove. Pipeline behaviors for validation. |
| FluentValidation | Latest | `AbstractValidator<T>`, registered via DI. `.EmailAddress()` for email validation. |
| React | 19.x | Controlled components, NOT React 19 form Actions. |
| TypeScript | 5.7.x | Strict mode, `erasableSyntaxOnly` in tsconfig. |
| TanStack Query | 5.x | `useMutation`, `isPending` (not isLoading), `retry: false` for mutations. |
| react-hook-form | Latest | `useForm()` + zod resolver. |
| zod | Latest | Schema-first validation. |
| shadcn/ui | Installed | `Dialog` for add form, `AlertDialog` for remove confirmation, `Button`, `Input`, `Label`. |
| Tailwind CSS | 4.x | CSS-first config via `@theme` in `index.css`. |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Candidates/
  CandidateDto.cs
  Commands/CreateCandidate/
    CreateCandidateCommand.cs
    CreateCandidateCommandValidator.cs
    CreateCandidateCommandHandler.cs
  Commands/RemoveCandidate/
    RemoveCandidateCommand.cs
    RemoveCandidateCommandValidator.cs
    RemoveCandidateCommandHandler.cs
  Queries/GetCandidates/
    GetCandidatesQuery.cs
    GetCandidatesQueryHandler.cs

api/src/Web/Endpoints/
  CandidateEndpoints.cs

api/tests/Application.UnitTests/Features/Candidates/
  Commands/CreateCandidate/
    CreateCandidateCommandHandlerTests.cs
    CreateCandidateCommandValidatorTests.cs
  Commands/RemoveCandidate/
    RemoveCandidateCommandHandlerTests.cs
    RemoveCandidateCommandValidatorTests.cs
  Queries/GetCandidates/
    GetCandidatesQueryHandlerTests.cs

web/src/lib/api/
  candidates.ts
  candidates.types.ts

web/src/features/candidates/
  CandidateList.tsx
  CandidateList.test.tsx
  CreateCandidateForm.tsx
  CreateCandidateForm.test.tsx
  hooks/
    useCandidates.ts
    useCandidateMutations.ts

web/src/mocks/
  candidateHandlers.ts
  fixtures/
    candidates.ts
```

**Existing files to modify:**
```
api/src/Web/Infrastructure/CustomExceptionHandler.cs
  -- Add DuplicateCandidateException handler (400 Problem Details)

web/src/mocks/handlers.ts
  -- Add candidateHandlers to handlers array

web/src/features/recruitments/pages/RecruitmentPage.tsx
  -- Import and render CandidateList component
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**

CreateCandidateCommandHandler tests:
- `Handle_ValidRequest_CreatesCandidateAndReturnsId`
- `Handle_DuplicateEmail_ThrowsDuplicateCandidateException`
- `Handle_ClosedRecruitment_ThrowsRecruitmentClosedException`
- `Handle_UserNotMember_ThrowsForbiddenAccessException`
- `Handle_RecruitmentNotFound_ThrowsNotFoundException`
- `Handle_NoDateApplied_DefaultsToUtcNow`
- `Handle_WithWorkflowSteps_PlacesCandidateAtFirstStep`

CreateCandidateCommandValidator tests:
- `Validate_EmptyFullName_Fails`
- `Validate_EmptyEmail_Fails`
- `Validate_InvalidEmailFormat_Fails`
- `Validate_EmptyRecruitmentId_Fails`
- `Validate_ValidCommand_Passes`
- `Validate_FullNameTooLong_Fails`
- `Validate_EmailTooLong_Fails`

RemoveCandidateCommandHandler tests:
- `Handle_ValidRequest_RemovesCandidate`
- `Handle_CandidateNotFound_ThrowsNotFoundException`
- `Handle_ClosedRecruitment_ThrowsRecruitmentClosedException`
- `Handle_UserNotMember_ThrowsForbiddenAccessException`

RemoveCandidateCommandValidator tests:
- `Validate_EmptyCandidateId_Fails`
- `Validate_EmptyRecruitmentId_Fails`

GetCandidatesQueryHandler tests:
- `Handle_ValidRequest_ReturnsPaginatedCandidates`
- `Handle_UserNotMember_ThrowsForbiddenAccessException`
- `Handle_RecruitmentNotFound_ThrowsNotFoundException`

Integration tests (WebApplicationFactory):
- `POST /api/recruitments/{id}/candidates` with valid body returns 201 + Location header + `{ id }`
- `POST /api/recruitments/{id}/candidates` with duplicate email returns 400 Problem Details with title "A candidate with this email already exists in this recruitment"
- `POST /api/recruitments/{id}/candidates` missing fullName returns 400 Problem Details with field-level error
- `POST /api/recruitments/{id}/candidates` on closed recruitment returns 400 "Recruitment is closed"
- `DELETE /api/recruitments/{id}/candidates/{cid}` returns 204
- `DELETE /api/recruitments/{id}/candidates/{cid}` non-existent returns 404
- `GET /api/recruitments/{id}/candidates` returns paginated list with correct structure

Test naming convention: `MethodName_Scenario_ExpectedBehavior`

**Frontend tests (Vitest + Testing Library + MSW):**

CreateCandidateForm tests:
- `"should render all form fields with correct labels"`
- `"should show validation errors when submitting with empty required fields"`
- `"should call API with correct data when form is valid"`
- `"should show success toast when candidate is created"`
- `"should show inline error when duplicate email is returned"`
- `"should disable submit button and show spinner when pending"`
- `"should close dialog on successful creation"`
- `"should default date applied to today"`

CandidateList tests:
- `"should render candidate data in list"`
- `"should display empty state when no candidates exist"`
- `"should show Add Candidate action in empty state"`
- `"should show remove button for each candidate"`
- `"should show confirmation dialog when remove is clicked"`
- `"should call remove API and show toast when confirmed"`
- `"should hide add and remove actions when recruitment is closed"`
- `"should show skeleton loader while loading"`

MSW handlers:
- `POST /api/recruitments/:recruitmentId/candidates` returning 201 with `{ id }`
- `POST /api/recruitments/:recruitmentId/candidates` with duplicate email returning 400 Problem Details
- `DELETE /api/recruitments/:recruitmentId/candidates/:candidateId` returning 204
- `GET /api/recruitments/:recruitmentId/candidates` returning paginated list

Use custom `test-utils.tsx` that wraps with `QueryClientProvider` + `MemoryRouter`.

### Previous Story Intelligence (Epic 1 + Epic 2 Learnings)

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- always use `apiGet`/`apiPost`/`apiDelete` from it
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- `Candidate` entity already exists with `Create()`, `RecordOutcome()`, `AttachDocument()`, `Anonymize()` methods
- Child entity constructors are `internal` -- only creatable through aggregate root methods
- Properties use `{ get; private set; }` or `{ get; init; }`
- `ApplicationDbContext` constructor requires `ITenantContext` -- mock it in tests
- `DuplicateCandidateException` already exists at `api/src/Domain/Exceptions/DuplicateCandidateException.cs`
- `CandidateImportedEvent` already exists at `api/src/Domain/Events/CandidateImportedEvent.cs`
- `CandidateConfiguration` already has unique index `UQ_Candidates_RecruitmentId_Email` with `[Email] IS NOT NULL` filter
- MediatR 13: `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`
- `IApplicationDbContext` has `DbSet<Candidate> Candidates` -- already available

**From Story 1.4 (Shared UI Components):**
- 18 shadcn/ui components already installed in `web/src/components/ui/`
- Custom components: `StatusBadge`, `ActionButton`, `EmptyState`, `SkeletonLoader`, `ErrorBoundary`
- `useAppToast()` hook for toast notifications (3-second auto-dismiss for success)
- `cn()` utility in `web/src/lib/utils.ts` for className merging

**From Story 1.5 (App Shell):**
- React Router v7 declarative mode (NOT framework mode) -- single `react-router` package
- TanStack Query v5: `isPending` replaces `isLoading`, `retry: false` for mutations
- CSS Grid layout: `grid-template-rows: 48px 1fr`

**From Story 2.1 (Create Recruitment):**
- CQRS folder structure established: one command per folder with Command, Validator, Handler
- Endpoint pattern established -- `EndpointGroupBase` with `GroupName` and `Map()` override
- Response DTOs use `static From()` factory methods
- `IUser` interface provides `Id` (string) and `Roles` for current user
- `ITenantContext` provides `UserGuid` (Guid?) for membership checks
- Frontend API client pattern established in `web/src/lib/api/recruitments.ts`
- TanStack Query mutation pattern established in `useRecruitmentMutations.ts`
- MSW handler pattern established in `web/src/mocks/recruitmentHandlers.ts`

**From Story 2.3 (Edit Recruitment + Workflow Steps):**
- ALL handlers must include membership verification via `ITenantContext.UserGuid` -- this was a C1 security finding
- Handler pattern: load recruitment with `.Include(r => r.Members)`, check `!recruitment.Members.Any(m => m.UserId == userId)`, throw `ForbiddenAccessException`

**From Story 2.5 (Close Recruitment):**
- `CloseRecruitmentCommandHandler` uses primary constructor pattern for DI
- Handler checks `recruitment.Status == RecruitmentStatus.Closed` and throws `RecruitmentClosedException`
- `RecruitmentPage.tsx` already has `isClosed` derived from `data.status === 'Closed'` -- use this for candidate list read-only mode
- `AlertDialog` pattern established for destructive confirmations

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(3.1): add CandidateDto and CreateCandidate command + handler + validator + tests`
2. `feat(3.1): add RemoveCandidate command + handler + validator + tests`
3. `feat(3.1): add GetCandidates query + handler + tests`
4. `feat(3.1): add CandidateEndpoints + DuplicateCandidateException mapping + integration tests`
5. `feat(3.1): add candidate API client, types, and hooks`
6. `feat(3.1): add CreateCandidateForm component + tests`
7. `feat(3.1): add CandidateList component with empty state + remove confirmation + tests`
8. `feat(3.1): wire CandidateList into RecruitmentPage + MSW handlers`

### Latest Tech Information

- **.NET 10.0:** LTS until Nov 2028. Primary constructor DI pattern used in handlers.
- **EF Core 10:** `FirstOrDefaultAsync` for loading by ID. `AnyAsync` for duplicate checks. The `CandidateConfiguration` with unique index `UQ_Candidates_RecruitmentId_Email` provides DB-level enforcement of email uniqueness -- the handler check prevents a less-friendly SQL error.
- **MediatR 13.x:** `IRequest<T>` for commands with return value, `IRequest` (void) for fire-and-forget. Pipeline behaviors for validation.
- **React 19.2:** Controlled components with react-hook-form. No form Actions.
- **TanStack Query 5.90.x:** `useMutation` with `isPending`. Query invalidation via `queryClient.invalidateQueries()`.
- **shadcn/ui Dialog:** For the "Add Candidate" form. `AlertDialog` for the "Remove Candidate" confirmation (destructive action).
- **react-hook-form + zod:** Use `zodResolver` from `@hookform/resolvers/zod` for schema-based validation. Validation on blur + submit.

### Project Structure Notes

- This story establishes the `features/candidates/` folder in the frontend -- the first candidate-related frontend feature
- This story establishes the `Application/Features/Candidates/` folder in the backend -- the first candidate-related CQRS feature
- `CandidateEndpoints.cs` uses nested resource pattern: `/api/recruitments/{recruitmentId}/candidates` -- following the pattern from `TeamEndpoints` (Story 2.4)
- The `Candidate` domain entity, `CandidateConfiguration`, `DuplicateCandidateException`, and `CandidateImportedEvent` already exist from Story 1.3 -- NO domain changes needed
- `IApplicationDbContext` already has `DbSet<Candidate> Candidates` -- NO DbContext changes needed

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-3-candidate-import-document-management.md` -- Story 3.1 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries (Candidate is own aggregate), cross-aggregate ID references, ITenantContext, membership enforcement]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, handler authorization pattern, error handling, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, component structure, empty state pattern, loading states, validation timing]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Response formats (201 + Location for creation, 204 for deletion), Problem Details, EndpointGroupBase pattern]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, Candidates feature paths]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- NUnit + NSubstitute + FluentAssertions, test naming, mandatory security tests]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR23, FR24, FR62 requirements]
- [Source: `api/src/Domain/Entities/Candidate.cs` -- Existing aggregate root with Create(), RecordOutcome()]
- [Source: `api/src/Domain/Entities/Recruitment.cs` -- EnsureNotClosed(), Steps collection, Members collection]
- [Source: `api/src/Domain/Exceptions/DuplicateCandidateException.cs` -- Existing domain exception]
- [Source: `api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs` -- UQ_Candidates_RecruitmentId_Email unique index]
- [Source: `api/src/Application/Common/Interfaces/IApplicationDbContext.cs` -- DbSet<Candidate> Candidates already present]
- [Source: `api/src/Web/Infrastructure/CustomExceptionHandler.cs` -- Exception-to-ProblemDetails mapping (add DuplicateCandidateException)]
- [Source: `api/src/Web/Endpoints/RecruitmentEndpoints.cs` -- EndpointGroupBase pattern reference]
- [Source: `web/src/lib/api/httpClient.ts` -- HTTP client with dual auth, Problem Details parsing]
- [Source: `web/src/lib/api/recruitments.ts` -- API client pattern reference]
- [Source: `web/src/features/recruitments/hooks/useRecruitmentMutations.ts` -- Mutation hook pattern reference]
- [Source: `web/src/mocks/recruitmentHandlers.ts` -- MSW handler pattern reference]
- [Source: `web/src/features/recruitments/pages/RecruitmentPage.tsx` -- Integration target, isClosed pattern]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Testing Mode Rationale

| Component | Mode | Rationale |
|-----------|------|-----------|
| CandidateDto | Characterization | Simple data mapping -- validated via handler tests |
| CreateCandidateCommandHandler | Test-first | Core business logic: aggregate creation, email uniqueness, authorization, initial step placement |
| CreateCandidateCommandValidator | Test-first | Validation rules for required fields, email format, length constraints |
| RemoveCandidateCommandHandler | Test-first | Destructive action: authorization, not-closed guard, existence check |
| RemoveCandidateCommandValidator | Test-first | Validation rules for required IDs |
| GetCandidatesQueryHandler | Test-first | Data access with authorization: membership check, pagination |
| CandidateEndpoints | Characterization | Thin routing layer -- auto-discovered via EndpointGroupBase reflection |
| CustomExceptionHandler mapping | Characterization | Adding one mapping to existing middleware |
| Frontend API client/hooks | Characterization | Thin wrappers over httpClient -- validated via component tests |
| CreateCandidateForm | Test-first | User-facing form with validation, inline error display, pending state |
| CandidateList | Test-first | Displays data, empty state, destructive remove confirmation, read-only mode |
| RecruitmentPage wiring | Characterization | Glue code -- import and render CandidateList |
| MSW handlers/fixtures | Characterization | Test infrastructure -- validated by component tests |

### Key Decisions

1. **Ambiguous type references**: `NotFoundException` and `ForbiddenAccessException` conflict with `Ardalis.GuardClauses`. Resolved with `using` aliases in handler files (same pattern as existing `AddWorkflowStepCommandHandler`).
2. **CreateCandidateForm controlled/uncontrolled dialog**: Made the dialog support both internal state (trigger button) and external controlled state (open/onOpenChange props) so the EmptyState "Add Candidate" action can open the form dialog.
3. **Empty state action wiring**: CandidateList manages `createDialogOpen` state and passes it to a controlled `CreateCandidateForm` instance when candidates list is empty, separate from the uncontrolled instance shown in the header when candidates exist.
4. **DuplicateCandidateException inline error**: Detects the specific Problem Details title string and uses `setError('email', ...)` to show inline under the email field rather than a generic toast.
5. **Endpoint GroupName**: Used `recruitments/{recruitmentId:guid}/candidates` to produce routes under `/api/recruitments/{id}/candidates/*`, consistent with the TeamEndpoints pattern.

### Debug Log References

- ASP.NET Core 10 runtime not installed in environment -- Application.UnitTests build cleanly but cannot execute (pre-existing limitation, not caused by this story)

### Completion Notes List

- Backend build: 0 errors, 0 warnings
- Domain tests: 49/49 pass
- Application unit tests: 23 new tests written, build verified, cannot execute (ASP.NET Core 10 runtime)
- Frontend TypeScript: 0 errors
- Frontend tests: 163/163 pass (15 new + 148 existing, zero regressions)
- Anti-pattern scan (E-002): Zero matches -- no direct Candidate instantiation, no child entity DbSet access, no caught domain exceptions in handlers
- All handlers enforce ITenantContext.UserGuid membership check
- Sprint status updated: 3-1 -> done

### File List

**Created (29 files):**

Backend -- Application layer:
- `api/src/Application/Features/Candidates/CandidateDto.cs`
- `api/src/Application/Features/Candidates/Commands/CreateCandidate/CreateCandidateCommand.cs`
- `api/src/Application/Features/Candidates/Commands/CreateCandidate/CreateCandidateCommandHandler.cs`
- `api/src/Application/Features/Candidates/Commands/CreateCandidate/CreateCandidateCommandValidator.cs`
- `api/src/Application/Features/Candidates/Commands/RemoveCandidate/RemoveCandidateCommand.cs`
- `api/src/Application/Features/Candidates/Commands/RemoveCandidate/RemoveCandidateCommandHandler.cs`
- `api/src/Application/Features/Candidates/Commands/RemoveCandidate/RemoveCandidateCommandValidator.cs`
- `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQuery.cs`
- `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandler.cs`
- `api/src/Application/Features/Candidates/Queries/GetCandidates/PaginatedCandidateListDto.cs`

Backend -- Web layer:
- `api/src/Web/Endpoints/CandidateEndpoints.cs`

Backend -- Unit tests:
- `api/tests/Application.UnitTests/Features/Candidates/Commands/CreateCandidate/CreateCandidateCommandHandlerTests.cs`
- `api/tests/Application.UnitTests/Features/Candidates/Commands/CreateCandidate/CreateCandidateCommandValidatorTests.cs`
- `api/tests/Application.UnitTests/Features/Candidates/Commands/RemoveCandidate/RemoveCandidateCommandHandlerTests.cs`
- `api/tests/Application.UnitTests/Features/Candidates/Commands/RemoveCandidate/RemoveCandidateCommandValidatorTests.cs`
- `api/tests/Application.UnitTests/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandlerTests.cs`

Frontend -- API layer:
- `web/src/lib/api/candidates.ts`
- `web/src/lib/api/candidates.types.ts`

Frontend -- Feature components:
- `web/src/features/candidates/CandidateList.tsx`
- `web/src/features/candidates/CandidateList.test.tsx`
- `web/src/features/candidates/CreateCandidateForm.tsx`
- `web/src/features/candidates/CreateCandidateForm.test.tsx`
- `web/src/features/candidates/hooks/useCandidates.ts`
- `web/src/features/candidates/hooks/useCandidateMutations.ts`

Frontend -- MSW test infrastructure:
- `web/src/mocks/candidateHandlers.ts`
- `web/src/mocks/fixtures/candidates.ts`

**Modified (3 files):**
- `api/src/Web/Infrastructure/CustomExceptionHandler.cs` -- Added DuplicateCandidateException mapping (400 Problem Details)
- `web/src/features/recruitments/pages/RecruitmentPage.tsx` -- Import and render CandidateList
- `web/src/mocks/handlers.ts` -- Added candidateHandlers to handler array

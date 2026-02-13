# Story 2.5: Close Recruitment & Read-Only View

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **recruiting leader (Erik)**,
I want to **close a completed recruitment so that it is locked from further changes and the GDPR retention timer begins**,
so that **the recruitment data is preserved for reference during the retention period and then properly cleaned up**.

## Acceptance Criteria

### AC1: Close confirmation dialog
**Given** an active recruitment exists
**When** the user clicks "Close Recruitment"
**Then** a confirmation dialog is shown explaining: the recruitment will be locked from edits, and data will be retained for the configured retention period before anonymization

### AC2: Successful close
**Given** the user confirms the close action
**When** the system processes the close
**Then** the recruitment status changes to "Closed"
**And** a `ClosedAt` timestamp is recorded (starts the GDPR retention timer)
**And** a `RecruitmentClosedEvent` is raised and recorded in the audit trail
**And** a success toast confirms the closure

### AC3: Read-only mode enforcement
**Given** a recruitment is closed
**When** any user views it
**Then** the recruitment is displayed in read-only mode
**And** all edit controls are disabled or hidden (no editing title, steps, outcomes, candidates)
**And** the recruitment is visually marked as "Closed" in the recruitment list

### AC4: Closed recruitment data visibility
**Given** a recruitment is closed
**When** the user views the candidate list and details
**Then** all candidate data, documents, and outcome history remain visible
**And** no modifications can be made

### AC5: API mutation rejection on closed recruitment
**Given** a recruitment is closed
**When** the user attempts to import candidates or upload documents via API
**Then** the API returns 400 Bad Request with Problem Details: "Recruitment is closed"

### AC6: Visual distinction in recruitment list
**Given** a closed recruitment exists
**When** the recruitment list is displayed
**Then** the closed recruitment appears with a "Closed" status indicator
**And** closed recruitments are visually distinct from active ones

### FRs Fulfilled
- **FR7:** Close a recruitment, locking it from further edits and starting GDPR retention timer
- **FR8:** View closed recruitments in read-only mode during the retention period

## Tasks / Subtasks

- [ ] Task 1: Backend -- CloseRecruitment command, validator, handler (AC: #2, #5)
  - [ ] 1.1 Create `CloseRecruitmentCommand` record in `api/src/Application/Features/Recruitments/Commands/CloseRecruitment/`
  - [ ] 1.2 Create `CloseRecruitmentCommandValidator` with FluentValidation (recruitmentId required)
  - [ ] 1.3 Create `CloseRecruitmentCommandHandler` -- loads Recruitment aggregate, calls `recruitment.Close()`, saves
  - [ ] 1.4 Unit test handler: valid close sets status + ClosedAt, domain event raised
  - [ ] 1.5 Unit test handler: closing already-closed recruitment throws `RecruitmentClosedException`
  - [ ] 1.6 Unit test validator: empty recruitmentId fails
- [ ] Task 2: Backend -- Minimal API endpoint `POST /api/recruitments/{id}/close` (AC: #2, #5)
  - [ ] 2.1 Add `POST /api/recruitments/{id}/close` to `RecruitmentEndpoints.cs`
  - [ ] 2.2 Return `200 OK` with updated recruitment response on success
  - [ ] 2.3 Integration test: successful close returns 200 + status "Closed" + ClosedAt populated
  - [ ] 2.4 Integration test: closing already-closed recruitment returns 400 Problem Details
  - [ ] 2.5 Integration test: closing non-existent recruitment returns 404
- [ ] Task 3: Backend -- Read-only enforcement via domain (AC: #5)
  - [ ] 3.1 Verify existing `EnsureNotClosed()` guard is called on ALL mutation methods in `Recruitment` aggregate (AddStep, RemoveStep, AddMember, RemoveMember) -- already implemented in domain, just verify via tests
  - [ ] 3.2 Add domain unit tests: calling AddStep/RemoveStep/AddMember/RemoveMember on a closed recruitment throws `RecruitmentClosedException`
  - [ ] 3.3 Verify global exception middleware maps `RecruitmentClosedException` to 400 Problem Details with title "Recruitment is closed"
- [ ] Task 4: Frontend -- API client additions (AC: #2)
  - [ ] 4.1 Add `close` method to `recruitmentApi` in `web/src/lib/api/recruitments.ts`
  - [ ] 4.2 Add `RecruitmentStatus` type and `ClosedAt` field to response types in `web/src/lib/api/recruitments.types.ts`
- [ ] Task 5: Frontend -- CloseRecruitmentDialog component (AC: #1, #2)
  - [ ] 5.1 Create `web/src/features/recruitments/CloseRecruitmentDialog.tsx` -- confirmation dialog with explanation text
  - [ ] 5.2 Use shadcn/ui `AlertDialog` (destructive confirmation pattern) with "Close Recruitment" as destructive action button
  - [ ] 5.3 Create `useCloseRecruitment` mutation in `web/src/features/recruitments/hooks/useRecruitmentMutations.ts`
  - [ ] 5.4 On success: toast "Recruitment closed", invalidate recruitment queries
  - [ ] 5.5 Unit tests: dialog renders explanation text, confirm triggers API call, cancel dismisses, success shows toast
  - [ ] 5.6 MSW handler for `POST /api/recruitments/:id/close`
- [ ] Task 6: Frontend -- Read-only mode enforcement (AC: #3, #4)
  - [ ] 6.1 Create `useIsRecruitmentClosed` hook (or inline check) that derives read-only state from recruitment status
  - [ ] 6.2 Conditionally hide/disable edit controls throughout recruitment views when status is "Closed": hide "Edit" buttons, hide "Close Recruitment" button, hide "Add Step"/"Remove Step", hide "Add Member"/"Remove Member"
  - [ ] 6.3 Add visual "Closed" banner or indicator at the top of the recruitment detail view
  - [ ] 6.4 Unit tests: when recruitment status is "Closed", edit controls are not rendered; "Closed" indicator is visible
- [ ] Task 7: Frontend -- Recruitment list status indicator (AC: #6)
  - [ ] 7.1 Add `StatusBadge` for recruitment status in `RecruitmentList.tsx` (or wherever the list is rendered)
  - [ ] 7.2 "Closed" recruitments show a distinct badge (e.g., gray `StatusBadge` or a "Closed" label)
  - [ ] 7.3 Unit test: closed recruitment renders with "Closed" badge, active recruitment renders with "Active" badge

## Dev Notes

### Affected Aggregate(s)

**Recruitment** (aggregate root) -- this is the only aggregate touched by this story. The domain model in `api/src/Domain/Entities/Recruitment.cs` already has the `Close()` method, `ClosedAt` property, `EnsureNotClosed()` guard, and `RecruitmentClosedEvent`. DO NOT modify the domain entities -- they are complete from Story 1.3.

Key domain methods already implemented:
- `Recruitment.Close()` -- sets status to `Closed`, records `ClosedAt` timestamp, raises `RecruitmentClosedEvent`
- `EnsureNotClosed()` -- private guard called by `AddStep()`, `RemoveStep()`, `AddMember()`, `RemoveMember()` that throws `RecruitmentClosedException`
- `RecruitmentClosedException` already exists at `api/src/Domain/Exceptions/RecruitmentClosedException.cs`
- `RecruitmentClosedEvent` already exists at `api/src/Domain/Events/RecruitmentClosedEvent.cs`
- `RecruitmentStatus.Closed` already exists in `api/src/Domain/Enums/RecruitmentStatus.cs`

Cross-aggregate: None. This story only transitions a Recruitment from Active to Closed. No Candidate or ImportSession aggregates are directly touched (their mutations are blocked indirectly via the `EnsureNotClosed()` guard when performed through the Recruitment aggregate).

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Command handler) | **Test-first** | Core business logic -- command orchestrates aggregate method call |
| Task 2 (API endpoint) | **Test-first** | Integration boundary -- must verify 200 on success, 400 Problem Details on closed, 404 on missing |
| Task 3 (Domain guards) | **Test-first** | Security-critical invariants -- must prove all mutation paths reject after close |
| Task 4 (API client) | **Characterization** | Thin wrapper over httpClient -- test via component integration tests |
| Task 5 (CloseRecruitmentDialog) | **Test-first** | User-facing destructive action dialog -- confirm/cancel flow, toast |
| Task 6 (Read-only enforcement) | **Test-first** | UI correctness -- edit controls must be hidden when closed |
| Task 7 (List status badge) | **Characterization** | Simple visual display -- verify badge renders |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **CQRS folder structure:**
   ```
   api/src/Application/Features/Recruitments/
     Commands/
       CloseRecruitment/
         CloseRecruitmentCommand.cs
         CloseRecruitmentCommandValidator.cs
         CloseRecruitmentCommandHandler.cs
   ```
   Rule: One command per folder. Handler in same folder.

2. **Command as record:**
   ```csharp
   public record CloseRecruitmentCommand(Guid Id) : IRequest;
   ```
   Note: Returns `Unit` (no return value needed -- caller already has the recruitment ID). Alternatively can return the updated status.

3. **Handler pattern -- use `IApplicationDbContext` directly:**
   ```csharp
   public class CloseRecruitmentCommandHandler(
       IApplicationDbContext dbContext)
       : IRequestHandler<CloseRecruitmentCommand>
   {
       public async Task Handle(
           CloseRecruitmentCommand request,
           CancellationToken cancellationToken)
       {
           var recruitment = await dbContext.Recruitments
               .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
               ?? throw new NotFoundException(nameof(Recruitment), request.Id);

           recruitment.Close();

           await dbContext.SaveChangesAsync(cancellationToken);
       }
   }
   ```
   CRITICAL: The `Close()` method already handles all invariants (throws `RecruitmentClosedException` if already closed, sets status, records timestamp, raises event). Do NOT duplicate domain logic in the handler.

4. **FluentValidation:**
   ```csharp
   public class CloseRecruitmentCommandValidator
       : AbstractValidator<CloseRecruitmentCommand>
   {
       public CloseRecruitmentCommandValidator()
       {
           RuleFor(x => x.Id).NotEmpty();
       }
   }
   ```

5. **Minimal API endpoint -- add to existing `RecruitmentEndpoints.cs`:**
   ```csharp
   app.MapPost("/api/recruitments/{id:guid}/close", async (
       Guid id,
       ISender sender) =>
   {
       await sender.Send(new CloseRecruitmentCommand(id));
       return Results.Ok();
   })
   .WithName("CloseRecruitment")
   .Produces(StatusCodes.Status200OK)
   .ProducesProblem(StatusCodes.Status400BadRequest)
   .ProducesProblem(StatusCodes.Status404NotFound);
   ```
   Note: No request body needed -- the recruitment ID comes from the route.

6. **Exception mapping:** `RecruitmentClosedException` must map to 400 Bad Request Problem Details with title "Recruitment is closed". Verify that `ExceptionHandlingMiddleware` (or the global exception handler) already handles this. If not, add a mapping. The architecture specifies that domain exceptions are converted to Problem Details by global exception middleware -- check `api/src/Web/Middleware/ExceptionHandlingMiddleware.cs` for the mapping.

7. **Error handling:** DO NOT catch domain exceptions in the handler. Let `RecruitmentClosedException` propagate to the global exception middleware for automatic Problem Details conversion.

8. **ITenantContext:** The handler loads the recruitment by ID. Ensure the query goes through `IApplicationDbContext` which applies tenant filtering automatically. The user must be a member of the recruitment to access it.

**Frontend -- MUST follow these patterns:**

1. **Feature folder structure (additions to existing):**
   ```
   web/src/features/recruitments/
     CloseRecruitmentDialog.tsx          (NEW)
     CloseRecruitmentDialog.test.tsx     (NEW)
     hooks/
       useRecruitmentMutations.ts        (MODIFY -- add useCloseRecruitment)
   web/src/lib/api/
     recruitments.ts                     (MODIFY -- add close method)
     recruitments.types.ts               (MODIFY -- add status/closedAt fields)
   ```

2. **API client using existing httpClient:**
   ```typescript
   // Add to web/src/lib/api/recruitments.ts
   export const recruitmentApi = {
     // ... existing methods ...
     close: (id: string) =>
       apiPost<void>(`/recruitments/${id}/close`),
   };
   ```
   CRITICAL: Use `apiPost` from `httpClient.ts` -- it handles auth headers and Problem Details parsing.

3. **Response type additions:**
   ```typescript
   // Add to web/src/lib/api/recruitments.types.ts
   export interface RecruitmentResponse {
     id: string;
     title: string;
     description: string | null;
     jobRequisitionId: string | null;
     status: 'Active' | 'Closed';
     closedAt: string | null;    // ISO 8601 DateTimeOffset
     steps: WorkflowStepResponse[];
   }
   ```

4. **Confirmation dialog uses shadcn/ui `AlertDialog`:**
   ```typescript
   // CloseRecruitmentDialog.tsx
   // Use AlertDialog (not Dialog) for destructive confirmation
   // - AlertDialogAction is the destructive "Close Recruitment" button (red/destructive variant)
   // - AlertDialogCancel is "Cancel"
   // - Body text explains: "This will lock the recruitment from further changes.
   //   Data will be retained for the configured retention period before anonymization."
   ```
   Per UX patterns: destructive actions use red text or outlined red (ActionButton patterns from `patterns-frontend.md`).

5. **TanStack Query mutation:**
   ```typescript
   // Add to hooks/useRecruitmentMutations.ts
   export function useCloseRecruitment() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (id: string) => recruitmentApi.close(id),
       onSuccess: () => {
         queryClient.invalidateQueries({ queryKey: ['recruitments'] });
       },
     });
   }
   ```
   Note: `isPending` (not `isLoading`) for TanStack Query v5.

6. **Toast on success:** Use `useAppToast()` hook: `toast({ title: "Recruitment closed", variant: "success" })`. Auto-dismiss after 3 seconds per UX spec.

7. **Read-only mode enforcement approach:**
   - Derive `isClosed` from the recruitment's `status` field (`status === 'Closed'`)
   - Pass `isClosed` as prop or use context to conditionally render/disable edit controls
   - DO NOT create a separate "read-only provider" -- keep it simple with a boolean check
   - Components that need to respect read-only: `EditRecruitmentForm` (Story 2.3), `WorkflowStepEditor` (Story 2.3), `MemberList` add/remove actions (Story 2.4), `CloseRecruitmentDialog` trigger button (this story)
   - If those stories are not yet implemented, add a `TODO` comment noting where read-only checks should go

8. **StatusBadge for recruitment status:**
   - Use the shared `StatusBadge` component from `web/src/components/StatusBadge.tsx`
   - Active = Blue indicator, Closed = Gray indicator
   - This extends the existing status badge system (currently used for outcome statuses)

### Architecture Compliance

- **Aggregate root access only:** Call `recruitment.Close()`. NEVER directly set `Status` or `ClosedAt` properties.
- **Ubiquitous language:** Use "Recruitment" (not job/position), "Closed" (not archived/completed/deactivated).
- **Manual DTO mapping:** If returning recruitment data after close, use existing `RecruitmentResponse.From()` factory method.
- **Problem Details for errors:** Test that `RecruitmentClosedException` returns RFC 9457 Problem Details with title, status, and type fields.
- **No PII in audit events/logs:** `RecruitmentClosedEvent` contains only `RecruitmentId` (Guid), never user names or emails.
- **NSubstitute for ALL mocking** (never Moq).
- **MediatR v13+:** `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. Configurations exist. `FirstOrDefaultAsync` for loading by ID. |
| MediatR | 13.x | `IRequest` (no return) or `IRequest<T>`. Pipeline behaviors for validation. |
| FluentValidation | Latest | `AbstractValidator<T>`, registered via DI. |
| React | 19.x | Controlled components, NOT React 19 form Actions. |
| TypeScript | 5.7.x | Strict mode, `erasableSyntaxOnly` in tsconfig. |
| TanStack Query | 5.x | `useMutation`, `isPending` (not isLoading), `retry: false` for mutations. |
| shadcn/ui | Installed | `AlertDialog` for destructive confirmation, `Button` with destructive variant. |
| Tailwind CSS | 4.x | CSS-first config via `@theme` in `index.css`. |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Recruitments/
  Commands/CloseRecruitment/
    CloseRecruitmentCommand.cs
    CloseRecruitmentCommandValidator.cs
    CloseRecruitmentCommandHandler.cs

api/tests/Application.UnitTests/Features/Recruitments/
  Commands/CloseRecruitment/
    CloseRecruitmentCommandHandlerTests.cs
    CloseRecruitmentCommandValidatorTests.cs

api/tests/Domain.UnitTests/Entities/
  RecruitmentCloseTests.cs   (or add to existing RecruitmentTests.cs)

web/src/features/recruitments/
  CloseRecruitmentDialog.tsx
  CloseRecruitmentDialog.test.tsx
```

**Existing files to modify:**
```
api/src/Web/Endpoints/RecruitmentEndpoints.cs   -- Add POST /api/recruitments/{id}/close
api/src/Web/Middleware/ExceptionHandlingMiddleware.cs  -- Verify RecruitmentClosedException -> 400 mapping exists
api/tests/Application.FunctionalTests/Endpoints/RecruitmentEndpointTests.cs  -- Add close endpoint tests

web/src/lib/api/recruitments.ts         -- Add close() method
web/src/lib/api/recruitments.types.ts   -- Add status/closedAt to response types
web/src/features/recruitments/hooks/useRecruitmentMutations.ts  -- Add useCloseRecruitment
web/src/mocks/handlers.ts              -- Add MSW handler for close endpoint
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**
- Handler tests: mock `IApplicationDbContext`, verify `Close()` called on loaded recruitment, `SaveChangesAsync` called
- Handler tests: recruitment not found -> throws `NotFoundException`
- Handler tests: recruitment already closed -> throws `RecruitmentClosedException` (propagated from domain)
- Validator tests: empty Id -> fails
- Domain tests: `recruitment.Close()` sets `Status` to `Closed`, sets `ClosedAt` to current time, raises `RecruitmentClosedEvent`
- Domain tests: calling `Close()` on already-closed recruitment throws `RecruitmentClosedException`
- Domain tests: calling `AddStep()` on closed recruitment throws `RecruitmentClosedException`
- Domain tests: calling `RemoveStep()` on closed recruitment throws `RecruitmentClosedException`
- Domain tests: calling `AddMember()` on closed recruitment throws `RecruitmentClosedException`
- Domain tests: calling `RemoveMember()` on closed recruitment throws `RecruitmentClosedException`
- Integration tests: `POST /api/recruitments/{id}/close` on active recruitment -> 200 OK
- Integration tests: `POST /api/recruitments/{id}/close` on already-closed recruitment -> 400 Problem Details
- Integration tests: `POST /api/recruitments/{id}/close` on non-existent recruitment -> 404 Problem Details
- Test naming: `MethodName_Scenario_ExpectedBehavior`

**Frontend tests (Vitest + Testing Library + MSW):**
- CloseRecruitmentDialog: renders explanation text about locking and retention
- CloseRecruitmentDialog: confirm button triggers close API call with correct recruitment ID
- CloseRecruitmentDialog: cancel button dismisses dialog without API call
- CloseRecruitmentDialog: success shows toast "Recruitment closed"
- CloseRecruitmentDialog: button disabled + shows spinner when mutation is pending
- Read-only mode: when recruitment status is "Closed", edit/close controls not rendered
- Read-only mode: "Closed" visual indicator is present
- Recruitment list: closed recruitment shows "Closed" badge
- Recruitment list: active recruitment shows "Active" badge
- MSW handler: mock `POST /api/recruitments/:id/close` returning 200
- Use custom `test-utils.tsx` that wraps with QueryClientProvider + MemoryRouter

### Previous Story Intelligence (Epic 1 + Stories 2.1-2.4 Learnings)

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- always use `apiGet`/`apiPost`/`apiPut`/`apiDelete` from it
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- Domain entities already exist with all methods for close: `Recruitment.Close()`, `EnsureNotClosed()`, `RecruitmentClosedException`, `RecruitmentClosedEvent`
- Child entity constructors are `internal` -- only creatable through aggregate root methods
- `ApplicationDbContext` constructor requires `ITenantContext` -- mock it in tests
- MediatR 13: `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`

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
- Endpoint pattern established in `RecruitmentEndpoints.cs` -- add the close endpoint here
- Response DTOs use `static From()` factory methods
- Frontend API client pattern established in `web/src/lib/api/recruitments.ts`
- TanStack Query mutation pattern established in `useRecruitmentMutations.ts`

**From Stories 2.2-2.4 (concurrent development -- may or may not exist yet):**
- Story 2.2 adds `RecruitmentList.tsx` and `GetRecruitments` query -- this is where the "Closed" badge goes
- Story 2.3 adds `EditRecruitmentForm.tsx` and `WorkflowStepEditor.tsx` -- these need read-only mode when closed
- Story 2.4 adds `MemberList.tsx` and member management -- add/remove actions need read-only mode when closed
- If these stories are not yet implemented, create `CloseRecruitmentDialog` as standalone and add TODO comments for read-only integration points

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(2.5): add CloseRecruitment command, validator, handler + tests`
2. `feat(2.5): add close endpoint to RecruitmentEndpoints + integration tests`
3. `feat(2.5): add domain close guard tests (mutation rejection after close)`
4. `feat(2.5): add close API client method + response type updates`
5. `feat(2.5): add CloseRecruitmentDialog component + tests`
6. `feat(2.5): add read-only mode enforcement + recruitment list status badge`

### Latest Tech Information

- **.NET 10.0:** LTS until Nov 2028. Global exception middleware pattern stable. No breaking changes.
- **EF Core 10:** `FirstOrDefaultAsync` for loading by ID. Ensure `.Include()` is used if aggregate children need to be loaded (for close, only the root is needed -- no includes required).
- **MediatR 13.x:** `IRequest` (void) pattern for commands with no return value. `IRequestHandler<T>` for void return.
- **React 19.2:** `AlertDialog` from shadcn/ui works with React 19. No special considerations.
- **TanStack Query 5.90.x:** `useMutation` with `isPending`. Query invalidation via `queryClient.invalidateQueries()`.
- **shadcn/ui AlertDialog:** Preferred over `Dialog` for destructive confirmation patterns. Has built-in escape/overlay dismiss. `AlertDialogAction` for confirm, `AlertDialogCancel` for cancel.

### Project Structure Notes

- Alignment with unified project structure: all paths follow Clean Architecture (`api/`) + Vite React (`web/`) split
- The `CloseRecruitment/` command folder follows the same CQRS pattern established in Story 2.1
- `CloseRecruitmentDialog.tsx` lives in `features/recruitments/` alongside other recruitment feature components
- API endpoint added to existing `RecruitmentEndpoints.cs` -- not a new endpoint file
- Domain entities are NOT modified -- this story only adds Application + Web + Frontend layers

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-recruitment-team-setup.md` -- Story 2.5 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries, Recruitment invariants, EnsureNotClosed guard]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, error handling, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, component structure, destructive action button pattern, StatusBadge]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Response formats, Problem Details, Minimal API endpoints]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, CloseRecruitment command path]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR7, FR8, FR9, FR63 GDPR retention requirements]
- [Source: `api/src/Domain/Entities/Recruitment.cs` -- Existing Close(), EnsureNotClosed() implementation]
- [Source: `api/src/Domain/Exceptions/RecruitmentClosedException.cs` -- Existing domain exception]
- [Source: `api/src/Domain/Events/RecruitmentClosedEvent.cs` -- Existing domain event]
- [Source: `api/src/Domain/Enums/RecruitmentStatus.cs` -- Active, Closed enum values]
- [Source: `web/src/lib/api/httpClient.ts` -- HTTP client with dual auth path]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

# Story 2.1: Create Recruitment with Workflow Steps

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **recruiting leader (Erik)**,
I want to **create a new recruitment with a title, description, and configurable workflow steps**,
so that **I can set up a structured hiring process for my team to follow**.

## Acceptance Criteria

### AC1: Dialog display
**Given** an authenticated user is on the home screen
**When** they click "Create Recruitment"
**Then** a dialog is displayed with fields for title (required), description (optional), and job requisition reference (optional)
**And** a default workflow template is shown with 7 steps: Screening, Technical Test, Technical Interview, Leader Interview, Personality Test, Offer/Contract, Negotiation

### AC2: Workflow step customization
**Given** the recruitment creation dialog is open
**When** the user views the default workflow steps
**Then** they can rename any step, add new steps, remove steps, and reorder steps freely before saving

### AC3: Successful creation
**Given** the user has entered a title and optionally customized the workflow
**When** they submit the creation form
**Then** a new recruitment is created with status "Active"
**And** the API returns `201 Created` with a Location header
**And** the user who created it is automatically added as a permanent member (cannot be removed)
**And** a `RecruitmentCreatedEvent` is raised and recorded in the audit trail

### AC4: New candidate default placement
**Given** a recruitment is created with workflow steps
**When** candidates are later added (imported or manually created)
**Then** new candidates are placed at the first workflow step with outcome status "Not Started"

### AC5: Title validation
**Given** the user submits the form with a missing title
**When** validation runs
**Then** a field-level validation error is shown ("Title is required")
**And** the form is not submitted

### AC6: Step name uniqueness and order
**Given** the user submits the form with valid data
**When** the API processes the request
**Then** workflow step names must be unique within the recruitment (case-insensitive)
**And** step order is contiguous (no gaps in sequence)

### FRs Fulfilled
- **FR4:** Create recruitment with title, description, job requisition reference
- **FR5:** Configure workflow steps (freeform names, set sequence, default template)
- **FR59:** Creator auto-added as permanent member
- **FR62:** New candidates placed at first workflow step as "Not Started"

## Tasks / Subtasks

- [ ] Task 1: Backend — CreateRecruitment command, validator, handler (AC: #3, #5, #6)
  - [ ] 1.1 Create `CreateRecruitmentCommand` record in `api/src/Application/Features/Recruitments/Commands/CreateRecruitment/`
  - [ ] 1.2 Create `CreateRecruitmentCommandValidator` with FluentValidation (title required, step names non-empty, unique, order contiguous)
  - [ ] 1.3 Create `CreateRecruitmentCommandHandler` — calls `Recruitment.Create()`, adds steps via `AddStep()`, saves via `IApplicationDbContext`
  - [ ] 1.4 Unit test handler: valid creation, returns Guid, domain events raised
  - [ ] 1.5 Unit test validator: missing title fails, duplicate step names fail, empty step name fails
- [ ] Task 2: Backend — Minimal API endpoint `POST /api/recruitments` (AC: #3)
  - [ ] 2.1 Create `RecruitmentEndpoints.cs` in `api/src/Web/Endpoints/`
  - [ ] 2.2 Map `POST /api/recruitments` — accept command body, send via MediatR, return `201 Created` with Location header
  - [ ] 2.3 Create response DTO with `static From()` factory (manual mapping, no AutoMapper)
  - [ ] 2.4 Integration test: successful creation returns 201 + Location header + response body
  - [ ] 2.5 Integration test: missing title returns 400 Problem Details with field-level error
- [ ] Task 3: Frontend — API client and types (AC: #3)
  - [ ] 3.1 Create `web/src/lib/api/recruitments.types.ts` — `CreateRecruitmentRequest`, `RecruitmentResponse` types
  - [ ] 3.2 Create `web/src/lib/api/recruitments.ts` — `recruitmentApi.create()` using `apiPost` from httpClient
- [ ] Task 4: Frontend — CreateRecruitmentForm dialog (AC: #1, #2, #5)
  - [ ] 4.1 Create `web/src/features/recruitments/CreateRecruitmentForm.tsx` — dialog with title, description (optional), job requisition ref (optional) fields
  - [ ] 4.2 Implement zod schema + react-hook-form integration for client-side validation
  - [ ] 4.3 Create `web/src/features/recruitments/WorkflowStepEditor.tsx` — inline list of steps with rename, add, remove, reorder controls
  - [ ] 4.4 Pre-populate default 7-step workflow template
  - [ ] 4.5 Create `web/src/features/recruitments/hooks/useRecruitmentMutations.ts` — TanStack Query `useMutation` wrapper
  - [ ] 4.6 Wire form submission: disable button + "Creating..." spinner, success toast ("Recruitment created"), error display
  - [ ] 4.7 Unit tests: form renders fields, validation errors shown on blur+submit, step editor add/remove/rename/reorder
  - [ ] 4.8 MSW handler for `POST /api/recruitments` in test setup
- [ ] Task 5: Frontend — Wire HomePage CTA to dialog (AC: #1)
  - [ ] 5.1 Update `HomePage.tsx` empty state CTA to open CreateRecruitmentForm dialog
  - [ ] 5.2 After successful creation: navigate to the new recruitment view (or stay on home with updated state)
  - [ ] 5.3 Test: clicking "Create Recruitment" opens dialog

## Dev Notes

### Affected Aggregate(s)

**Recruitment** (aggregate root) — this is the only aggregate touched by this story. The domain model already exists in `api/src/Domain/Entities/Recruitment.cs` with full invariant enforcement. DO NOT modify the domain entities — they are complete from Story 1.3.

Key domain methods to use:
- `Recruitment.Create(title, description, createdByUserId)` — factory method, auto-adds creator as permanent "Recruiting Leader" member, raises `RecruitmentCreatedEvent`
- `recruitment.AddStep(name, order)` — enforces unique step names (case-insensitive), throws `DuplicateStepNameException`
- Child entities (`WorkflowStep`, `RecruitmentMember`) have `internal` constructors — only creatable through aggregate root methods

Cross-aggregate: None. This story only creates a Recruitment. Candidates (separate aggregate) are not involved.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Command handler) | **Test-first** | Core business logic — command validation and aggregate orchestration |
| Task 2 (API endpoint) | **Test-first** | Integration boundary — must verify 201+Location, Problem Details on error |
| Task 3 (API client) | **Characterization** | Thin wrapper over httpClient — test via component integration tests |
| Task 4 (Form + StepEditor) | **Test-first** | User-facing form with complex validation and step management |
| Task 5 (HomePage wiring) | **Characterization** | Glue code — test dialog opens on CTA click |

### Technical Requirements

**Backend — MUST follow these patterns:**

1. **CQRS folder structure:**
   ```
   api/src/Application/Features/Recruitments/
     Commands/
       CreateRecruitment/
         CreateRecruitmentCommand.cs
         CreateRecruitmentCommandValidator.cs
         CreateRecruitmentCommandHandler.cs
   ```
   Rule: One command per folder. Handler in same folder.

2. **Command as record:**
   ```csharp
   public record CreateRecruitmentCommand(
       string Title,
       string? Description,
       string? JobRequisitionId,
       List<WorkflowStepDto> Steps) : IRequest<Guid>;

   public record WorkflowStepDto(string Name, int Order);
   ```

3. **Handler pattern — use `IApplicationDbContext` directly:**
   ```csharp
   public class CreateRecruitmentCommandHandler(
       IApplicationDbContext dbContext,
       ICurrentUserService currentUser)
       : IRequestHandler<CreateRecruitmentCommand, Guid>
   {
       public async Task<Guid> Handle(
           CreateRecruitmentCommand request,
           CancellationToken cancellationToken)
       {
           var userId = Guid.Parse(currentUser.UserId!);
           var recruitment = Recruitment.Create(
               request.Title, request.Description, userId);

           foreach (var step in request.Steps)
               recruitment.AddStep(step.Name, step.Order);

           dbContext.Recruitments.Add(recruitment);
           await dbContext.SaveChangesAsync(cancellationToken);
           return recruitment.Id;
       }
   }
   ```
   Note: NO repository abstraction — use `IApplicationDbContext` per existing codebase pattern.

4. **FluentValidation:**
   ```csharp
   public class CreateRecruitmentCommandValidator
       : AbstractValidator<CreateRecruitmentCommand>
   {
       public CreateRecruitmentCommandValidator()
       {
           RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
           RuleFor(x => x.Description).MaximumLength(2000);
           RuleFor(x => x.JobRequisitionId).MaximumLength(100);
           RuleFor(x => x.Steps).NotEmpty()
               .Must(steps => steps.Select(s => s.Name.ToLowerInvariant()).Distinct().Count() == steps.Count)
               .WithMessage("Step names must be unique");
       }
   }
   ```

5. **Minimal API endpoint:**
   ```csharp
   // api/src/Web/Endpoints/RecruitmentEndpoints.cs
   public static class RecruitmentEndpoints
   {
       public static void MapRecruitmentEndpoints(this IEndpointRouteBuilder app)
       {
           app.MapPost("/api/recruitments", async (
               CreateRecruitmentCommand command,
               ISender sender) =>
           {
               var id = await sender.Send(command);
               return Results.Created($"/api/recruitments/{id}", new { id });
           })
           .WithName("CreateRecruitment")
           .Produces<object>(StatusCodes.Status201Created)
           .ProducesValidationProblem();
       }
   }
   ```

6. **Response DTO with manual mapping:**
   ```csharp
   public record RecruitmentResponse(
       Guid Id, string Title, string? Description,
       string? JobRequisitionId, string Status,
       List<WorkflowStepResponse> Steps)
   {
       public static RecruitmentResponse From(Recruitment r) => new(
           r.Id, r.Title, r.Description, r.JobRequisitionId,
           r.Status.ToString(),
           r.Steps.Select(WorkflowStepResponse.From).OrderBy(s => s.Order).ToList());
   }

   public record WorkflowStepResponse(Guid Id, string Name, int Order)
   {
       public static WorkflowStepResponse From(WorkflowStep s) => new(s.Id, s.Name, s.Order);
   }
   ```

7. **Error handling:** Domain exceptions are converted to Problem Details (RFC 9457) by global exception middleware already in place. DO NOT catch domain exceptions in the handler.

8. **ITenantContext:** Not yet needed for creation — the creator is establishing the recruitment. Tenant scoping applies to queries (Story 2.2+). However, `ICurrentUserService.UserId` is used to identify the creator.

**Frontend — MUST follow these patterns:**

1. **Feature folder structure:**
   ```
   web/src/features/recruitments/
     CreateRecruitmentForm.tsx
     CreateRecruitmentForm.test.tsx
     WorkflowStepEditor.tsx
     WorkflowStepEditor.test.tsx
     hooks/
       useRecruitmentMutations.ts
       useRecruitmentMutations.test.ts
     pages/
       HomePage.tsx  (already exists)
   web/src/lib/api/
     recruitments.ts
     recruitments.types.ts
   ```

2. **API client using existing httpClient:**
   ```typescript
   // web/src/lib/api/recruitments.ts
   import { apiPost } from './httpClient';
   import type { CreateRecruitmentRequest, RecruitmentResponse } from './recruitments.types';

   export const recruitmentApi = {
     create: (data: CreateRecruitmentRequest) =>
       apiPost<RecruitmentResponse>('/recruitments', data),
   };
   ```
   CRITICAL: Use `apiPost` from `httpClient.ts` — it handles auth headers (MSAL in prod, X-Dev-User-* in dev).

3. **Form with react-hook-form + zod:**
   - Use shadcn/ui `Dialog`, `Form`, `Input`, `Textarea`, `Button` components (already installed in Story 1.4)
   - Validation: on blur + on submit (NOT on keystroke)
   - Labels above inputs, optional fields marked "(optional)"
   - Error messages below field in red, specific text
   - Button: "Create Recruitment" → "Creating..." with spinner when pending, disabled

4. **TanStack Query mutation:**
   ```typescript
   // hooks/useRecruitmentMutations.ts
   import { useMutation, useQueryClient } from '@tanstack/react-query';
   import { recruitmentApi } from '@/lib/api/recruitments';

   export function useCreateRecruitment() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: recruitmentApi.create,
       onSuccess: () => {
         queryClient.invalidateQueries({ queryKey: ['recruitments'] });
       },
     });
   }
   ```
   Note: `isPending` (not `isLoading`) for TanStack Query v5.

5. **Toast on success:** Use `useAppToast()` hook (from Story 1.4): `toast({ title: "Recruitment created", variant: "success" })`. Auto-dismiss after 3 seconds per UX spec.

6. **Dialog is the ONLY modal in the app** — per UX design, recruitment creation is the one place where a Dialog is appropriate. All other decisions are inline.

### Architecture Compliance

- **Aggregate root access only:** Call `Recruitment.Create()` and `recruitment.AddStep()`. NEVER directly instantiate `WorkflowStep` or `RecruitmentMember`.
- **Ubiquitous language:** Use "Recruitment" (not job/position), "Workflow Step" (not stage/phase), "Recruitment Member" (not participant), "Recruiting Leader" (not manager/owner).
- **Manual DTO mapping:** `static From()` factory on response DTOs. NO AutoMapper.
- **Problem Details for errors:** Test that validation failures return RFC 9457 Problem Details with `errors` object, not just status codes.
- **No PII in audit events/logs:** `RecruitmentCreatedEvent` contains only `RecruitmentId` (Guid), never user names or emails.
- **EF Core Fluent API only:** No data annotations. Configurations already exist in `api/src/Infrastructure/Data/Configurations/`.
- **NSubstitute for ALL mocking** (never Moq).
- **MediatR v13+:** `RequestHandlerDelegate` takes `CancellationToken` — lambda uses `(_)` not `()`.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. Configurations exist. |
| MediatR | 13.x (project current) | `IRequest<T>`, `IRequestHandler<,>`, pipeline behaviors for validation |
| FluentValidation | Latest | `AbstractValidator<T>`, registered via DI |
| React | 19.x | Controlled components, NOT React 19 form Actions (SPA, not SSR) |
| TypeScript | 5.7.x | Strict mode, `erasableSyntaxOnly` in tsconfig |
| TanStack Query | 5.x | `useMutation`, `isPending` (not isLoading), `retry: false` for mutations |
| react-hook-form | Latest | `useForm()` + zod resolver |
| zod | Latest | Schema-first validation |
| shadcn/ui | Installed | Dialog, Form, Input, Textarea, Button components in `web/src/components/ui/` |
| Tailwind CSS | 4.x | CSS-first config via `@theme` in `index.css`, `@tailwindcss/vite` plugin |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Recruitments/
  Commands/CreateRecruitment/
    CreateRecruitmentCommand.cs
    CreateRecruitmentCommandValidator.cs
    CreateRecruitmentCommandHandler.cs
  DTOs/
    RecruitmentResponse.cs
    WorkflowStepResponse.cs

api/src/Web/Endpoints/
  RecruitmentEndpoints.cs

api/tests/Application.UnitTests/Features/Recruitments/
  Commands/CreateRecruitment/
    CreateRecruitmentCommandHandlerTests.cs
    CreateRecruitmentCommandValidatorTests.cs

api/tests/Web.IntegrationTests/Endpoints/
  RecruitmentEndpointsTests.cs

web/src/features/recruitments/
  CreateRecruitmentForm.tsx
  CreateRecruitmentForm.test.tsx
  WorkflowStepEditor.tsx
  WorkflowStepEditor.test.tsx
  hooks/
    useRecruitmentMutations.ts
    useRecruitmentMutations.test.ts

web/src/lib/api/
  recruitments.ts
  recruitments.types.ts
```

**Existing files to modify:**
```
web/src/features/recruitments/pages/HomePage.tsx  — Wire CTA to open dialog
web/src/routes/index.tsx  — Possibly add recruitment/:id route (prep for Story 2.2)
api/src/Web/Program.cs (or similar)  — Register endpoint mapping: app.MapRecruitmentEndpoints()
api/src/Application/DependencyInjection.cs  — Ensure MediatR + FluentValidation registered (may already exist)
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**
- Handler tests: mock `IApplicationDbContext` and `ICurrentUserService`, verify `Recruitments.Add()` called, `SaveChangesAsync` called, correct Guid returned
- Validator tests: title empty → fails, title too long → fails, duplicate step names → fails, valid input → passes
- Integration tests: `POST /api/recruitments` with valid body → 201 + Location header + response; missing title → 400 Problem Details with `errors.Title`
- Test naming: `MethodName_Scenario_ExpectedBehavior`

**Frontend tests (Vitest + Testing Library + MSW):**
- Form renders: all fields present, default 7 steps shown
- Validation: submit without title shows error, error clears on input
- Step editor: add step, remove step, rename step, reorder step
- Submission: button disabled + shows "Creating..." when pending, toast on success
- MSW handler: mock `POST /api/recruitments` returning `201` with `{ id: "..." }`
- Use custom `test-utils.tsx` that wraps with QueryClientProvider + MemoryRouter
- Co-located test files: `Component.test.tsx` next to `Component.tsx`

### Previous Story Intelligence (Epic 1 Learnings)

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point — always use `apiGet`/`apiPost`/`apiPut`/`apiDelete` from it
- MSAL v5 removed `storeAuthStateInCookie` — don't reference it
- `erasableSyntaxOnly` in tsconfig means constructor parameters must be field declarations (no `public` shorthand in TS)
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- Cross-aggregate references use IDs only (Guid)
- Child entity constructors are `internal` — only creatable through aggregate root methods
- Properties use `{ get; private set; }` or `{ get; init; }`
- EF Core: Fluent API ONLY, no data annotations
- MediatR 13: `RequestHandlerDelegate` takes `CancellationToken` — lambda uses `(_)` not `()`
- Review fix applied: creator-cannot-be-removed invariant exists
- `ApplicationDbContext` constructor requires `ITenantContext` — mock it in tests

**From Story 1.4 (Shared UI Components):**
- 18 shadcn/ui components already installed in `web/src/components/ui/`
- Custom components: `StatusBadge`, `ActionButton`, `EmptyState`, `SkeletonLoader`, `ErrorBoundary`
- `useAppToast()` hook for toast notifications (3-second auto-dismiss for success)
- `cn()` utility in `web/src/lib/utils.ts` for className merging
- Design tokens in `@theme` block in `web/src/index.css`
- Axe accessibility tests use bare `rtlRender` to isolate a11y checks

**From Story 1.5 (App Shell):**
- React Router v7 declarative mode (NOT framework mode) — single `react-router` package
- `createBrowserRouter()` + `<RouterProvider />`
- Route config exported separately for test use with `createMemoryRouter`
- TanStack Query v5: `isPending` replaces `isLoading`, `retry: 3` for queries, `retry: false` for mutations
- `ProtectedRoute` calls `login()` instead of navigating to `/login`
- CSS Grid layout: `grid-template-rows: 48px 1fr`
- `HomePage.tsx` exists with empty state CTA — this is what we wire to

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Recent patterns (Story 1.5):**
- 10+ incremental commits showing per-component building
- Files organized by feature folder (`features/recruitments/pages/`, `features/auth/`)
- `test-utils.tsx` updated with `QueryClientProvider` + `MemoryRouter` wrappers
- Each component got its own commit with tests included

**Current state:** Branch `main`, clean working tree, only `docs/experiments/` untracked.

### Latest Tech Information

- **.NET 10.0:** LTS until Nov 2028. OpenAPI 3.1 built-in. No breaking changes for typical CRUD.
- **EF Core 10:** ComplexType can now be nullable. Use existing `HasMany` configuration for WorkflowStep (already configured). DO NOT switch to owned entities — current separate-table approach with `HasMany` + cascade delete is correct.
- **MediatR:** Project uses v13.x. Dual license from v13+ (free under $5M revenue). Pattern unchanged from Story 1.3.
- **React 19.2:** Form Actions exist but are NOT recommended for SPA + TanStack Query. Use controlled components + `useMutation`.
- **TypeScript 5.7.x:** Stable. TypeScript 6.0 beta available but not needed.
- **Vite 7.3:** Requires Node 20.19+. Uses `@tailwindcss/vite` plugin (no PostCSS).
- **Tailwind CSS 4.x:** CSS-first configuration via `@theme` in `index.css`. No `tailwind.config.js`. Buttons default to `cursor: default` — add `cursor-pointer` explicitly.
- **TanStack Query 5.90.x:** `isPending` not `isLoading`. `mutationOptions()` helper available for reusable mutation configs.

### Project Structure Notes

- Alignment with unified project structure: all paths follow Clean Architecture (`api/`) + Vite React (`web/`) split established in Story 1.1
- Feature folders in `web/src/features/recruitments/` match existing pattern from `features/auth/`
- API types in `web/src/lib/api/` follow httpClient pattern from Story 1.2
- Backend CQRS structure in `api/src/Application/Features/` is NEW — this story establishes the pattern for all future commands/queries
- Endpoint registration in `api/src/Web/` follows Minimal API pattern — this story establishes the pattern

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2.md` — Story 2.1 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — Aggregate boundaries, Recruitment invariants, ITenantContext]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` — C# naming, CQRS structure, DTO mapping, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` — React/TS naming, component structure, state management]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` — Response formats, Problem Details, Minimal API endpoints]
- [Source: `_bmad-output/planning-artifacts/ux-design.md` — J0 journey, dialog design, form patterns, button hierarchy, toast behavior]
- [Source: `api/src/Domain/Entities/Recruitment.cs` — Existing aggregate root implementation]
- [Source: `api/src/Infrastructure/Data/Configurations/RecruitmentConfiguration.cs` — EF Core configuration]
- [Source: `web/src/lib/api/httpClient.ts` — HTTP client with dual auth path]
- [Source: `web/src/features/recruitments/pages/HomePage.tsx` — Existing empty state CTA to wire]
- [Source: `docs/testing-pragmatic-tdd.md` — Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

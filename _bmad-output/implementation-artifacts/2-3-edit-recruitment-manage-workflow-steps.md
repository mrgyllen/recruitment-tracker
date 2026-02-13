# Story 2.3: Edit Recruitment & Manage Workflow Steps

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **recruiting leader (Erik)**,
I want to **edit a recruitment's details and modify workflow steps on an active recruitment**,
so that **I can adapt the hiring process as requirements evolve mid-recruitment**.

## Acceptance Criteria

### AC1: Edit recruitment details
**Given** an active recruitment exists
**When** the user edits the recruitment title or description
**Then** the changes are saved and a success toast is shown
**And** the updated values are reflected immediately in the UI

### AC2: Add a new workflow step
**Given** an active recruitment exists
**When** the user adds a new workflow step
**Then** the step is added at the specified position in the sequence
**And** the step order is adjusted to remain contiguous
**And** existing candidates see the new step as "Not Started"

### AC3: Remove a step with no recorded outcomes
**Given** an active recruitment has a workflow step with no recorded outcomes
**When** the user removes that step
**Then** the step is removed from the workflow
**And** any candidates currently at that step are moved to the next step with status "Not Started"
**And** step order is recompacted

### AC4: Block removal of step with recorded outcomes
**Given** an active recruitment has a workflow step with recorded outcomes
**When** the user attempts to remove that step
**Then** the removal is blocked with a clear message: "Cannot remove -- outcomes recorded at this step"
**And** the step remains unchanged

### AC5: Reorder workflow steps
**Given** an active recruitment has workflow steps
**When** the user reorders steps
**Then** the sequence is updated
**And** no candidate data is lost or corrupted

### AC6: Closed recruitment is read-only
**Given** a closed recruitment exists
**When** the user attempts to edit the title, description, or workflow steps
**Then** all edit controls are disabled or hidden
**And** the recruitment is displayed in read-only mode

### FRs Fulfilled
- **FR11:** Edit recruitment details (title, description) on active recruitment
- **FR12:** Modify workflow steps (add, remove with outcome protection, reorder) on active recruitment

## Tasks / Subtasks

- [ ] Task 1: Domain -- Add `UpdateDetails()` and `ReorderSteps()` methods to Recruitment aggregate (AC: #1, #5)
  - [ ] 1.1 Add `Recruitment.UpdateDetails(string title, string? description, string? jobRequisitionId)` method with `EnsureNotClosed()` guard
  - [ ] 1.2 Add `Recruitment.ReorderSteps(List<(Guid StepId, int NewOrder)> reordering)` method with contiguous-order validation
  - [ ] 1.3 Add `WorkflowStep.UpdateOrder(int newOrder)` internal method for reordering
  - [ ] 1.4 Unit test `UpdateDetails`: valid update changes properties, closed recruitment throws `RecruitmentClosedException`
  - [ ] 1.5 Unit test `ReorderSteps`: valid reorder updates order, invalid step ID throws, closed recruitment throws
- [ ] Task 2: Backend -- UpdateRecruitment command + handler + validator (AC: #1)
  - [ ] 2.1 Create `UpdateRecruitmentCommand` record in `api/src/Application/Features/Recruitments/Commands/UpdateRecruitment/`
  - [ ] 2.2 Create `UpdateRecruitmentCommandValidator` with FluentValidation (title required, max lengths)
  - [ ] 2.3 Create `UpdateRecruitmentCommandHandler` -- loads aggregate, calls `UpdateDetails()`, saves
  - [ ] 2.4 Unit test handler: valid update saves, recruitment not found returns appropriate error
  - [ ] 2.5 Unit test validator: missing title fails, title too long fails, valid input passes
- [ ] Task 3: Backend -- AddWorkflowStep command + handler + validator (AC: #2)
  - [ ] 3.1 Create `AddWorkflowStepCommand` record in `api/src/Application/Features/Recruitments/Commands/AddWorkflowStep/`
  - [ ] 3.2 Create `AddWorkflowStepCommandValidator` (step name required, non-empty)
  - [ ] 3.3 Create `AddWorkflowStepCommandHandler` -- loads aggregate with steps, calls `AddStep()`, saves
  - [ ] 3.4 Unit test handler: step added successfully, duplicate name returns domain exception
  - [ ] 3.5 Unit test validator: empty name fails, valid name passes
- [ ] Task 4: Backend -- RemoveWorkflowStep command + handler (AC: #3, #4)
  - [ ] 4.1 Create `RemoveWorkflowStepCommand` record in `api/src/Application/Features/Recruitments/Commands/RemoveWorkflowStep/`
  - [ ] 4.2 Create `RemoveWorkflowStepCommandHandler` -- loads aggregate, checks outcomes via query, calls `MarkStepHasOutcomes()` if needed, calls `RemoveStep()`, saves
  - [ ] 4.3 Unit test handler: step removed when no outcomes, `StepHasOutcomesException` when outcomes exist, step not found throws
- [ ] Task 5: Backend -- ReorderWorkflowSteps command + handler (AC: #5)
  - [ ] 5.1 Create `ReorderWorkflowStepsCommand` record in `api/src/Application/Features/Recruitments/Commands/ReorderWorkflowSteps/`
  - [ ] 5.2 Create `ReorderWorkflowStepsCommandValidator` (non-empty list, contiguous order values)
  - [ ] 5.3 Create `ReorderWorkflowStepsCommandHandler` -- loads aggregate with steps, calls `ReorderSteps()`, saves
  - [ ] 5.4 Unit test handler: valid reorder updates steps, invalid step IDs throw
- [ ] Task 6: Backend -- Minimal API endpoints (AC: #1, #2, #3, #4, #5)
  - [ ] 6.1 Add `PUT /api/recruitments/{id}` to `RecruitmentEndpoints.cs` for updating details
  - [ ] 6.2 Add `POST /api/recruitments/{id}/steps` for adding a step
  - [ ] 6.3 Add `DELETE /api/recruitments/{id}/steps/{stepId}` for removing a step
  - [ ] 6.4 Add `PUT /api/recruitments/{id}/steps/reorder` for reordering steps
  - [ ] 6.5 Integration test: PUT recruitment with valid data returns 200
  - [ ] 6.6 Integration test: PUT recruitment with missing title returns 400 Problem Details
  - [ ] 6.7 Integration test: POST step returns 200 with new step data
  - [ ] 6.8 Integration test: DELETE step with outcomes returns 409 Problem Details
  - [ ] 6.9 Integration test: DELETE step without outcomes returns 204
  - [ ] 6.10 Integration test: PUT reorder returns 200 with updated step list
  - [ ] 6.11 Integration test: All endpoints on closed recruitment return 400 Problem Details
- [ ] Task 7: Backend -- GetRecruitmentById query (AC: #1, #6)
  - [ ] 7.1 Create `GetRecruitmentByIdQuery` in `api/src/Application/Features/Recruitments/Queries/GetRecruitmentById/` if not already created by Story 2.2
  - [ ] 7.2 Create `GetRecruitmentByIdQueryHandler` returning `RecruitmentDetailResponse` with steps
  - [ ] 7.3 Unit test: returns recruitment with steps, not found returns null/throws
- [ ] Task 8: Frontend -- API client and types (AC: #1, #2, #3, #5)
  - [ ] 8.1 Add types to `web/src/lib/api/recruitments.types.ts`: `UpdateRecruitmentRequest`, `AddWorkflowStepRequest`, `ReorderStepsRequest`, `RecruitmentDetailResponse`
  - [ ] 8.2 Add methods to `web/src/lib/api/recruitments.ts`: `recruitmentApi.update()`, `recruitmentApi.getById()`, `recruitmentApi.addStep()`, `recruitmentApi.removeStep()`, `recruitmentApi.reorderSteps()`
- [ ] Task 9: Frontend -- EditRecruitmentForm component (AC: #1, #6)
  - [ ] 9.1 Create `web/src/features/recruitments/EditRecruitmentForm.tsx` -- inline form (NOT dialog) for editing title, description, job requisition ref
  - [ ] 9.2 Implement zod schema + react-hook-form integration for client-side validation
  - [ ] 9.3 Disable all fields when recruitment status is "Closed" (read-only mode)
  - [ ] 9.4 Create `web/src/features/recruitments/hooks/useRecruitmentQueries.ts` -- TanStack Query `useQuery` wrapper for `getRecruitmentById`
  - [ ] 9.5 Add update mutation to `web/src/features/recruitments/hooks/useRecruitmentMutations.ts`
  - [ ] 9.6 Success toast: "Recruitment updated" via `useAppToast()`
  - [ ] 9.7 Unit tests: form renders fields with existing values, validation errors shown, disabled in closed state
- [ ] Task 10: Frontend -- WorkflowStepEditor for edit mode (AC: #2, #3, #4, #5, #6)
  - [ ] 10.1 Extend or refactor `WorkflowStepEditor.tsx` to support edit mode (persisted steps with IDs) in addition to create mode (local-only steps)
  - [ ] 10.2 Add step: inline input + add button, calls `addStep` mutation on save
  - [ ] 10.3 Remove step: remove button per step, calls `removeStep` mutation. Show error message from API if step has outcomes (409 response)
  - [ ] 10.4 Reorder steps: drag-and-drop or up/down buttons, calls `reorderSteps` mutation on change
  - [ ] 10.5 Disable all controls when recruitment status is "Closed"
  - [ ] 10.6 Add step mutations to `useRecruitmentMutations.ts`: `useAddWorkflowStep()`, `useRemoveWorkflowStep()`, `useReorderWorkflowSteps()`
  - [ ] 10.7 Unit tests: add step, remove step, reorder, error display for step-has-outcomes, disabled in closed state
  - [ ] 10.8 MSW handlers for all step mutation endpoints

## Dev Notes

### Affected Aggregate(s)

**Recruitment** (aggregate root) -- this is the only aggregate touched by this story. The domain model already exists in `api/src/Domain/Entities/Recruitment.cs` with `AddStep()` and `RemoveStep()` methods. This story requires adding:

1. `Recruitment.UpdateDetails(title, description, jobRequisitionId)` -- new method to update recruitment properties
2. `Recruitment.ReorderSteps(reordering)` -- new method to update step ordering
3. `WorkflowStep.UpdateOrder(newOrder)` -- new internal method to allow the aggregate root to update step order

Key existing domain methods:
- `Recruitment.AddStep(name, order)` -- already exists, enforces unique step names (case-insensitive), throws `DuplicateStepNameException`
- `Recruitment.RemoveStep(stepId)` -- already exists, checks `_stepsWithOutcomes` set, throws `StepHasOutcomesException`
- `Recruitment.MarkStepHasOutcomes(stepId)` -- already exists, used by application layer to mark steps with outcomes before calling `RemoveStep()`
- `EnsureNotClosed()` -- private guard, already used by all mutation methods, throws `RecruitmentClosedException`

Cross-aggregate: The `RemoveStep` handler needs to query `CandidateOutcome` data to determine if outcomes exist for a step. This is a **read** across aggregates (allowed) -- the handler queries outcomes, then calls `MarkStepHasOutcomes()` on the Recruitment aggregate before attempting `RemoveStep()`. No cross-aggregate writes.

**Important domain note:** The `_stepsWithOutcomes` HashSet is in-memory only (not persisted by EF Core). The application layer MUST query for outcome existence and call `MarkStepHasOutcomes()` before calling `RemoveStep()`. This is by design -- the domain stays pure while the application layer provides the integration.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Domain methods) | **Test-first** | Core business logic -- aggregate invariants, closed recruitment guard |
| Task 2 (UpdateRecruitment handler) | **Test-first** | Business logic orchestration with validation |
| Task 3 (AddWorkflowStep handler) | **Test-first** | Business logic -- delegates to existing domain method |
| Task 4 (RemoveWorkflowStep handler) | **Test-first** | Complex logic -- outcome checking + domain exception propagation |
| Task 5 (ReorderWorkflowSteps handler) | **Test-first** | Business logic -- step reordering with validation |
| Task 6 (API endpoints) | **Test-first** | Integration boundary -- verify status codes, Problem Details, response shapes |
| Task 7 (GetRecruitmentById query) | **Test-first** | Query with DTO mapping -- verify correct shape returned |
| Task 8 (API client) | **Characterization** | Thin wrapper over httpClient -- test via component integration tests |
| Task 9 (EditRecruitmentForm) | **Test-first** | User-facing form with validation and read-only mode |
| Task 10 (WorkflowStepEditor edit mode) | **Test-first** | Complex UI with multiple mutation states and error handling |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **CQRS folder structure -- one command per folder:**
   ```
   api/src/Application/Features/Recruitments/
     Commands/
       UpdateRecruitment/
         UpdateRecruitmentCommand.cs
         UpdateRecruitmentCommandValidator.cs
         UpdateRecruitmentCommandHandler.cs
       AddWorkflowStep/
         AddWorkflowStepCommand.cs
         AddWorkflowStepCommandValidator.cs
         AddWorkflowStepCommandHandler.cs
       RemoveWorkflowStep/
         RemoveWorkflowStepCommand.cs
         RemoveWorkflowStepCommandHandler.cs
       ReorderWorkflowSteps/
         ReorderWorkflowStepsCommand.cs
         ReorderWorkflowStepsCommandValidator.cs
         ReorderWorkflowStepsCommandHandler.cs
     Queries/
       GetRecruitmentById/
         GetRecruitmentByIdQuery.cs
         GetRecruitmentByIdQueryHandler.cs
   ```

2. **Command records:**
   ```csharp
   public record UpdateRecruitmentCommand(
       Guid Id,
       string Title,
       string? Description,
       string? JobRequisitionId) : IRequest;

   public record AddWorkflowStepCommand(
       Guid RecruitmentId,
       string Name,
       int Order) : IRequest<WorkflowStepResponse>;

   public record RemoveWorkflowStepCommand(
       Guid RecruitmentId,
       Guid StepId) : IRequest;

   public record ReorderWorkflowStepsCommand(
       Guid RecruitmentId,
       List<StepOrderDto> Steps) : IRequest;

   public record StepOrderDto(Guid StepId, int Order);
   ```

3. **Handler pattern -- use `IApplicationDbContext` directly:**
   ```csharp
   public class UpdateRecruitmentCommandHandler(
       IApplicationDbContext dbContext)
       : IRequestHandler<UpdateRecruitmentCommand>
   {
       public async Task Handle(
           UpdateRecruitmentCommand request,
           CancellationToken cancellationToken)
       {
           var recruitment = await dbContext.Recruitments
               .Include(r => r.Steps)
               .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
               ?? throw new NotFoundException(nameof(Recruitment), request.Id);

           recruitment.UpdateDetails(
               request.Title, request.Description, request.JobRequisitionId);

           await dbContext.SaveChangesAsync(cancellationToken);
       }
   }
   ```
   Note: NO repository abstraction -- use `IApplicationDbContext` per existing codebase pattern. Load aggregate with `.Include()` for child collections.

4. **RemoveWorkflowStep handler -- outcome checking pattern:**
   ```csharp
   public class RemoveWorkflowStepCommandHandler(
       IApplicationDbContext dbContext)
       : IRequestHandler<RemoveWorkflowStepCommand>
   {
       public async Task Handle(
           RemoveWorkflowStepCommand request,
           CancellationToken cancellationToken)
       {
           var recruitment = await dbContext.Recruitments
               .Include(r => r.Steps)
               .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
               ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

           // Check if step has outcomes (cross-aggregate read)
           var hasOutcomes = await dbContext.CandidateOutcomes
               .AnyAsync(o => o.WorkflowStepId == request.StepId, cancellationToken);

           if (hasOutcomes)
               recruitment.MarkStepHasOutcomes(request.StepId);

           recruitment.RemoveStep(request.StepId);
           await dbContext.SaveChangesAsync(cancellationToken);
       }
   }
   ```

5. **FluentValidation for UpdateRecruitment:**
   ```csharp
   public class UpdateRecruitmentCommandValidator
       : AbstractValidator<UpdateRecruitmentCommand>
   {
       public UpdateRecruitmentCommandValidator()
       {
           RuleFor(x => x.Id).NotEmpty();
           RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
           RuleFor(x => x.Description).MaximumLength(2000);
           RuleFor(x => x.JobRequisitionId).MaximumLength(100);
       }
   }
   ```

6. **Minimal API endpoints:**
   ```csharp
   // Add to RecruitmentEndpoints.cs (extends existing file from Story 2.1)
   app.MapPut("/api/recruitments/{id:guid}", async (
       Guid id,
       UpdateRecruitmentCommand command,
       ISender sender) =>
   {
       // Ensure route ID matches command ID
       await sender.Send(command with { Id = id });
       return Results.NoContent();
   })
   .WithName("UpdateRecruitment")
   .Produces(StatusCodes.Status204NoContent)
   .ProducesValidationProblem();

   app.MapPost("/api/recruitments/{id:guid}/steps", async (
       Guid id,
       AddWorkflowStepCommand command,
       ISender sender) =>
   {
       var result = await sender.Send(command with { RecruitmentId = id });
       return Results.Ok(result);
   })
   .WithName("AddWorkflowStep")
   .Produces<WorkflowStepResponse>(StatusCodes.Status200OK)
   .ProducesValidationProblem();

   app.MapDelete("/api/recruitments/{id:guid}/steps/{stepId:guid}", async (
       Guid id, Guid stepId,
       ISender sender) =>
   {
       await sender.Send(new RemoveWorkflowStepCommand(id, stepId));
       return Results.NoContent();
   })
   .WithName("RemoveWorkflowStep")
   .Produces(StatusCodes.Status204NoContent)
   .ProducesProblem(StatusCodes.Status409Conflict);

   app.MapPut("/api/recruitments/{id:guid}/steps/reorder", async (
       Guid id,
       ReorderWorkflowStepsCommand command,
       ISender sender) =>
   {
       await sender.Send(command with { RecruitmentId = id });
       return Results.NoContent();
   })
   .WithName("ReorderWorkflowSteps")
   .Produces(StatusCodes.Status204NoContent)
   .ProducesValidationProblem();
   ```

7. **Error handling:** Domain exceptions are converted to Problem Details (RFC 9457) by global exception middleware already in place. `StepHasOutcomesException` should map to 409 Conflict. `RecruitmentClosedException` should map to 400 Bad Request. DO NOT catch domain exceptions in the handler.

8. **Response DTO for step operations:** Reuse `WorkflowStepResponse` from Story 2.1. The `AddWorkflowStep` endpoint returns the new step's details.

**Frontend -- MUST follow these patterns:**

1. **Feature folder structure (extends existing from Story 2.1):**
   ```
   web/src/features/recruitments/
     CreateRecruitmentForm.tsx        (Story 2.1)
     EditRecruitmentForm.tsx          (NEW)
     EditRecruitmentForm.test.tsx     (NEW)
     WorkflowStepEditor.tsx           (EXTEND -- add edit mode)
     WorkflowStepEditor.test.tsx      (EXTEND)
     hooks/
       useRecruitmentMutations.ts     (EXTEND -- add update, step mutations)
       useRecruitmentQueries.ts       (NEW -- getById query)
       useRecruitmentQueries.test.ts  (NEW)
   web/src/lib/api/
     recruitments.ts                  (EXTEND -- add update, step API methods)
     recruitments.types.ts            (EXTEND -- add request/response types)
   ```

2. **API client extending existing module:**
   ```typescript
   // Add to web/src/lib/api/recruitments.ts
   export const recruitmentApi = {
     create: (data: CreateRecruitmentRequest) =>
       apiPost<RecruitmentResponse>('/recruitments', data),
     getById: (id: string) =>
       apiGet<RecruitmentDetailResponse>(`/recruitments/${id}`),
     update: (id: string, data: UpdateRecruitmentRequest) =>
       apiPut<void>(`/recruitments/${id}`, data),
     addStep: (recruitmentId: string, data: AddWorkflowStepRequest) =>
       apiPost<WorkflowStepResponse>(`/recruitments/${recruitmentId}/steps`, data),
     removeStep: (recruitmentId: string, stepId: string) =>
       apiDelete(`/recruitments/${recruitmentId}/steps/${stepId}`),
     reorderSteps: (recruitmentId: string, data: ReorderStepsRequest) =>
       apiPut<void>(`/recruitments/${recruitmentId}/steps/reorder`, data),
   };
   ```
   CRITICAL: Use `apiGet`/`apiPost`/`apiPut`/`apiDelete` from `httpClient.ts` -- it handles auth headers (MSAL in prod, X-Dev-User-* in dev).

3. **Edit form with react-hook-form + zod:**
   - Use shadcn/ui `Form`, `Input`, `Textarea`, `Button` components (already installed in Story 1.4)
   - Pre-populate form with existing values from `getRecruitmentById` query
   - Validation: on blur + on submit (NOT on keystroke)
   - Labels above inputs, optional fields marked "(optional)"
   - Error messages below field in red, specific text
   - Button: "Save Changes" -> "Saving..." with spinner when pending, disabled
   - All fields disabled when recruitment status is "Closed"

4. **TanStack Query patterns:**
   ```typescript
   // hooks/useRecruitmentQueries.ts
   import { useQuery } from '@tanstack/react-query';
   import { recruitmentApi } from '@/lib/api/recruitments';

   export function useRecruitmentById(id: string) {
     return useQuery({
       queryKey: ['recruitments', id],
       queryFn: () => recruitmentApi.getById(id),
     });
   }
   ```

   ```typescript
   // hooks/useRecruitmentMutations.ts (additions)
   export function useUpdateRecruitment(id: string) {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (data: UpdateRecruitmentRequest) =>
         recruitmentApi.update(id, data),
       onSuccess: () => {
         queryClient.invalidateQueries({ queryKey: ['recruitments', id] });
         queryClient.invalidateQueries({ queryKey: ['recruitments'] });
       },
     });
   }

   export function useAddWorkflowStep(recruitmentId: string) {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (data: AddWorkflowStepRequest) =>
         recruitmentApi.addStep(recruitmentId, data),
       onSuccess: () => {
         queryClient.invalidateQueries({ queryKey: ['recruitments', recruitmentId] });
       },
     });
   }

   export function useRemoveWorkflowStep(recruitmentId: string) {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (stepId: string) =>
         recruitmentApi.removeStep(recruitmentId, stepId),
       onSuccess: () => {
         queryClient.invalidateQueries({ queryKey: ['recruitments', recruitmentId] });
       },
     });
   }

   export function useReorderWorkflowSteps(recruitmentId: string) {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (data: ReorderStepsRequest) =>
         recruitmentApi.reorderSteps(recruitmentId, data),
       onSuccess: () => {
         queryClient.invalidateQueries({ queryKey: ['recruitments', recruitmentId] });
       },
     });
   }
   ```
   Note: `isPending` (not `isLoading`) for TanStack Query v5. `retry: false` for mutations (default).

5. **Toast on success:** Use `useAppToast()` hook (from Story 1.4): `toast({ title: "Recruitment updated", variant: "success" })`. Auto-dismiss after 3 seconds per UX spec.

6. **Error handling for step removal:** When `removeStep` mutation returns 409 Conflict (step has outcomes), parse the Problem Details and display the error message inline near the step -- NOT as a toast. This is a validation-style error, not a transient error. Use `ApiError.problemDetails.title` for the message text.

7. **Inline editing, NOT dialog:** Per UX design (J2 journey), editing recruitment settings happens inline on the recruitment view page. The only dialog in the app is recruitment creation (Story 2.1). All other decisions are inline.

8. **WorkflowStepEditor modes:** The `WorkflowStepEditor` component from Story 2.1 handles create mode (local-only steps, no persistence). This story extends it with edit mode (persisted steps with IDs, each action triggers an API call). Consider a `mode: 'create' | 'edit'` prop or separate components if the logic diverges significantly. The edit mode should:
   - Show existing steps from the API response
   - Add: inline input, API call on confirm, optimistic update
   - Remove: confirm via inline UI, API call, handle 409 error
   - Reorder: up/down buttons (drag-and-drop optional), API call on change

### Architecture Compliance

- **Aggregate root access only:** Call `Recruitment.UpdateDetails()`, `Recruitment.AddStep()`, `Recruitment.RemoveStep()`, `Recruitment.ReorderSteps()`. NEVER directly modify `WorkflowStep` properties from the handler.
- **Ubiquitous language:** Use "Recruitment" (not job/position), "Workflow Step" (not stage/phase), "Recruitment Member" (not participant), "Recruiting Leader" (not manager/owner).
- **Manual DTO mapping:** `static From()` factory on response DTOs. NO AutoMapper.
- **Problem Details for errors:** Test that `StepHasOutcomesException` returns 409 Conflict with Problem Details. Test that `RecruitmentClosedException` returns 400 Bad Request with Problem Details.
- **No PII in audit events/logs:** Any audit events raised contain only entity IDs (Guid), never user names or emails.
- **EF Core Fluent API only:** No data annotations. Configurations already exist in `api/src/Infrastructure/Data/Configurations/`.
- **NSubstitute for ALL mocking** (never Moq).
- **MediatR v13+:** `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`.
- **Domain exceptions not caught in handlers:** Let the global exception middleware convert `StepHasOutcomesException`, `RecruitmentClosedException`, and `DuplicateStepNameException` to Problem Details responses.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. `.Include()` for loading steps with aggregate. |
| MediatR | 13.x (project current) | `IRequest<T>`, `IRequestHandler<,>`, pipeline behaviors for validation |
| FluentValidation | Latest | `AbstractValidator<T>`, registered via DI |
| React | 19.x | Controlled components, NOT React 19 form Actions (SPA, not SSR) |
| TypeScript | 5.7.x | Strict mode, `erasableSyntaxOnly` in tsconfig |
| TanStack Query | 5.x | `useMutation`, `useQuery`, `isPending` (not isLoading), `retry: false` for mutations |
| react-hook-form | Latest | `useForm()` + zod resolver, `defaultValues` from API data |
| zod | Latest | Schema-first validation |
| shadcn/ui | Installed | Form, Input, Textarea, Button components in `web/src/components/ui/` |
| Tailwind CSS | 4.x | CSS-first config via `@theme` in `index.css`, `@tailwindcss/vite` plugin |

### File Structure Requirements

**New files to create:**
```
api/src/Domain/Entities/Recruitment.cs            (MODIFY -- add UpdateDetails, ReorderSteps)
api/src/Domain/Entities/WorkflowStep.cs            (MODIFY -- add UpdateOrder internal method)

api/src/Application/Features/Recruitments/
  Commands/UpdateRecruitment/
    UpdateRecruitmentCommand.cs
    UpdateRecruitmentCommandValidator.cs
    UpdateRecruitmentCommandHandler.cs
  Commands/AddWorkflowStep/
    AddWorkflowStepCommand.cs
    AddWorkflowStepCommandValidator.cs
    AddWorkflowStepCommandHandler.cs
  Commands/RemoveWorkflowStep/
    RemoveWorkflowStepCommand.cs
    RemoveWorkflowStepCommandHandler.cs
  Commands/ReorderWorkflowSteps/
    ReorderWorkflowStepsCommand.cs
    ReorderWorkflowStepsCommandValidator.cs
    ReorderWorkflowStepsCommandHandler.cs
  Queries/GetRecruitmentById/
    GetRecruitmentByIdQuery.cs
    GetRecruitmentByIdQueryHandler.cs

api/tests/Domain.UnitTests/Entities/
  RecruitmentTests.cs                              (EXTEND -- add UpdateDetails, ReorderSteps tests)

api/tests/Application.UnitTests/Features/Recruitments/
  Commands/UpdateRecruitment/
    UpdateRecruitmentCommandHandlerTests.cs
    UpdateRecruitmentCommandValidatorTests.cs
  Commands/AddWorkflowStep/
    AddWorkflowStepCommandHandlerTests.cs
    AddWorkflowStepCommandValidatorTests.cs
  Commands/RemoveWorkflowStep/
    RemoveWorkflowStepCommandHandlerTests.cs
  Commands/ReorderWorkflowSteps/
    ReorderWorkflowStepsCommandHandlerTests.cs
    ReorderWorkflowStepsCommandValidatorTests.cs

api/tests/Application.FunctionalTests/Endpoints/
  RecruitmentEndpointTests.cs                      (EXTEND -- add edit, step management tests)

web/src/features/recruitments/
  EditRecruitmentForm.tsx
  EditRecruitmentForm.test.tsx
  WorkflowStepEditor.tsx                           (EXTEND -- add edit mode)
  WorkflowStepEditor.test.tsx                      (EXTEND)
  hooks/
    useRecruitmentQueries.ts
    useRecruitmentQueries.test.ts
    useRecruitmentMutations.ts                     (EXTEND)

web/src/lib/api/
  recruitments.ts                                  (EXTEND)
  recruitments.types.ts                            (EXTEND)
```

**Existing files to modify:**
```
api/src/Web/Endpoints/RecruitmentEndpoints.cs      -- Add PUT, POST step, DELETE step, PUT reorder endpoints
api/src/Domain/Entities/Recruitment.cs             -- Add UpdateDetails(), ReorderSteps() methods
api/src/Domain/Entities/WorkflowStep.cs            -- Add UpdateOrder() internal method
web/src/lib/api/recruitments.ts                    -- Add update, step API methods
web/src/lib/api/recruitments.types.ts              -- Add request/response types
web/src/features/recruitments/hooks/useRecruitmentMutations.ts -- Add update, step mutation hooks
web/src/features/recruitments/WorkflowStepEditor.tsx -- Add edit mode support
web/src/mocks/handlers.ts                          -- Add MSW handlers for new endpoints
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**
- Domain tests: `UpdateDetails` with valid data changes properties, `UpdateDetails` on closed recruitment throws `RecruitmentClosedException`, `ReorderSteps` with valid data updates order, `ReorderSteps` with unknown step ID throws
- Handler tests: mock `IApplicationDbContext`, verify aggregate methods called, `SaveChangesAsync` called
- Validator tests: empty title fails, title too long fails, valid input passes; empty step name fails; empty reorder list fails
- Integration tests (functional): `PUT /api/recruitments/{id}` with valid body returns 204; missing title returns 400 Problem Details; `POST /api/recruitments/{id}/steps` returns 200; `DELETE /api/recruitments/{id}/steps/{stepId}` with outcomes returns 409; without outcomes returns 204; `PUT /api/recruitments/{id}/steps/reorder` returns 204; all endpoints on closed recruitment return 400
- Test naming: `MethodName_Scenario_ExpectedBehavior`

**Frontend tests (Vitest + Testing Library + MSW):**
- EditRecruitmentForm: form renders with existing values, validation errors on empty title, submit calls API, disabled fields in closed state
- WorkflowStepEditor (edit mode): add step calls API, remove step calls API, remove step with outcomes shows error, reorder calls API, disabled in closed state
- MSW handlers: mock all 4 new endpoints (`PUT /api/recruitments/:id`, `POST /api/recruitments/:id/steps`, `DELETE /api/recruitments/:id/steps/:stepId`, `PUT /api/recruitments/:id/steps/reorder`)
- Use custom `test-utils.tsx` that wraps with QueryClientProvider + MemoryRouter
- Co-located test files: `Component.test.tsx` next to `Component.tsx`

### Previous Story Intelligence

**From Story 2.1 (Create Recruitment with Workflow Steps) -- direct predecessor:**
- `WorkflowStepEditor.tsx` component created for create mode with add/remove/rename/reorder for LOCAL steps. This story extends it for PERSISTED steps (each action = API call).
- `RecruitmentEndpoints.cs` created with `POST /api/recruitments`. This story adds PUT + step management endpoints to the same file.
- `useRecruitmentMutations.ts` created with `useCreateRecruitment()`. This story adds `useUpdateRecruitment()`, `useAddWorkflowStep()`, `useRemoveWorkflowStep()`, `useReorderWorkflowSteps()`.
- `recruitmentApi` in `recruitments.ts` created with `create` method. This story adds `getById`, `update`, `addStep`, `removeStep`, `reorderSteps`.
- `RecruitmentResponse` and `WorkflowStepResponse` DTOs created. Reuse `WorkflowStepResponse` for step operation responses.
- `CreateRecruitmentCommandHandler` pattern established -- use `IApplicationDbContext` directly, no repository abstraction.

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- Child entity constructors are `internal` -- only creatable through aggregate root methods
- Properties use `{ get; private set; }` or `{ get; init; }`
- EF Core: Fluent API ONLY, no data annotations
- `ApplicationDbContext` constructor requires `ITenantContext` -- mock it in tests
- `_stepsWithOutcomes` HashSet is in-memory only -- application layer must query outcomes before `RemoveStep()`

**From Story 1.4 (Shared UI Components):**
- `useAppToast()` hook for toast notifications (3-second auto-dismiss for success)
- `cn()` utility in `web/src/lib/utils.ts` for className merging
- `ActionButton` component for Primary/Secondary/Destructive variants
- `ErrorBoundary` component at feature level

**From Story 1.5 (App Shell):**
- React Router v7 declarative mode
- TanStack Query v5: `isPending` replaces `isLoading`, `retry: false` for mutations
- Route config: `recruitmentId` param expected in URL (prep from Story 2.2)

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Recent patterns (Stories 1.3-1.5):**
- Per-component incremental commits with tests included
- Files organized by feature folder
- Domain entity changes committed separately from application layer changes

**Current state:** Branch `main`, Story 2.1 at "ready-for-dev", Story 2.2 at "backlog".

### Latest Tech Information

- **.NET 10.0:** LTS until Nov 2028. `IRequest` (no return type) for void commands, `IRequest<T>` for commands returning data.
- **EF Core 10:** `.Include()` for eager loading child collections. Tracked entities auto-detect changes on `SaveChangesAsync()`. No need to call `Update()` on tracked entities.
- **MediatR:** `with` expression on records for route-param override pattern (e.g., `command with { Id = id }`).
- **React 19.2:** Use controlled components + `useMutation` for forms. NOT React 19 form Actions.
- **TanStack Query 5.x:** `invalidateQueries` accepts partial query key for cache invalidation. `retry: false` default for mutations.
- **react-hook-form:** `defaultValues` supports async loading -- use `reset()` method to set values after data loads from API.

### Project Structure Notes

- Alignment with unified project structure: all paths follow Clean Architecture (`api/`) + Vite React (`web/`) split
- CQRS structure in `api/src/Application/Features/Recruitments/Commands/` extends pattern established by Story 2.1
- Endpoint registration in `api/src/Web/Endpoints/RecruitmentEndpoints.cs` extends existing file
- `GetRecruitmentById` query may already exist from Story 2.2 -- check before creating. If it exists, reuse. If not, create it.
- Frontend edit components are inline (not dialog) per UX design

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-recruitment-team-setup.md` -- Story 2.3 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries, Recruitment invariants, ITenantContext]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, component structure, state management]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Response formats, Problem Details, Minimal API endpoints]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, CQRS folder layout]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Test frameworks, naming, pragmatic TDD modes]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md` -- J2 journey: import & workflow modification]
- [Source: `api/src/Domain/Entities/Recruitment.cs` -- Existing aggregate root with AddStep, RemoveStep, MarkStepHasOutcomes]
- [Source: `api/src/Domain/Entities/WorkflowStep.cs` -- Existing entity with internal Create method]
- [Source: `api/src/Domain/Exceptions/StepHasOutcomesException.cs` -- Existing domain exception for step removal protection]
- [Source: `api/src/Domain/Exceptions/RecruitmentClosedException.cs` -- Existing domain exception for closed recruitment guard]
- [Source: `web/src/lib/api/httpClient.ts` -- HTTP client with apiGet, apiPost, apiPut, apiDelete]
- [Source: `_bmad-output/implementation-artifacts/2-1-create-recruitment-with-workflow-steps.md` -- Story 2.1 patterns to follow]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

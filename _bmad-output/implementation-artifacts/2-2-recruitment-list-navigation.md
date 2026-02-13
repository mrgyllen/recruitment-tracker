# Story 2.2: Recruitment List & Navigation

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **user**,
I want to **see a list of all recruitments I have access to and navigate between them**,
so that **I can quickly find and switch to the recruitment I need to work on**.

## Acceptance Criteria

### AC1: Recruitment list display
**Given** an authenticated user has access to one or more recruitments
**When** the home screen loads
**Then** a list of recruitments is displayed showing each recruitment's title and current status (Active/Closed)
**And** only recruitments where the user is a member are shown

### AC2: Empty state (no recruitments)
**Given** the user has no recruitments (not a member of any)
**When** the home screen loads
**Then** the empty state from Story 1.5 is displayed with the "Create Recruitment" CTA

### AC3: Click to navigate
**Given** the recruitment list is displayed
**When** the user clicks a recruitment
**Then** the user navigates to that recruitment's main view
**And** the URL updates to reflect the selected recruitment (e.g., `/recruitments/{id}`)
**And** navigation completes in under 300ms (client-side routing)

### AC4: Breadcrumb display
**Given** the user is viewing a recruitment
**When** they look at the app header
**Then** the recruitment name appears as a breadcrumb
**And** if the user has access to multiple recruitments, the breadcrumb includes a dropdown to switch between recruitments

### AC5: Recruitment selector dropdown
**Given** the user has access to multiple recruitments
**When** they click the recruitment selector dropdown in the header
**Then** all accessible recruitments are listed with their status
**And** selecting one navigates to that recruitment without a full page reload

### AC6: Access control (403)
**Given** the user is a member of Recruitment A but not Recruitment B
**When** they attempt to access Recruitment B's URL directly
**Then** a 403 Forbidden response is returned
**And** the user sees an appropriate error message

### FRs Fulfilled
- **FR6:** View list of all recruitments user has access to
- **FR13:** Navigation between recruitments
- **FR60:** Membership-based access enforcement on queries

## Tasks / Subtasks

- [ ] Task 1: Backend -- GetRecruitments query (AC: #1, #2)
  - [ ] 1.1 Create `GetRecruitmentsQuery.cs` record in `api/src/Application/Features/Recruitments/Queries/GetRecruitments/`
  - [ ] 1.2 Create `GetRecruitmentsQueryHandler.cs` -- query `IApplicationDbContext.Recruitments` filtered by membership via `ITenantContext.UserId`
  - [ ] 1.3 Create `RecruitmentListItemDto.cs` -- Id, Title, Status, CreatedAt
  - [ ] 1.4 Unit test handler: returns only recruitments where user is a member
  - [ ] 1.5 Unit test handler: returns empty list when user has no memberships
- [ ] Task 2: Backend -- GetRecruitmentById query (AC: #3, #6)
  - [ ] 2.1 Create `GetRecruitmentByIdQuery.cs` record in `api/src/Application/Features/Recruitments/Queries/GetRecruitmentById/`
  - [ ] 2.2 Create `GetRecruitmentByIdQueryHandler.cs` -- load recruitment by Id, verify membership via `ITenantContext.UserId`, return 403 if not a member
  - [ ] 2.3 Create `RecruitmentDetailDto.cs` -- Id, Title, Description, JobRequisitionId, Status, CreatedAt, ClosedAt, Steps list
  - [ ] 2.4 Unit test handler: valid member gets recruitment details
  - [ ] 2.5 Unit test handler: non-member gets `ForbiddenAccessException`
  - [ ] 2.6 Unit test handler: non-existent recruitment throws `NotFoundException`
- [ ] Task 3: Backend -- Minimal API endpoints (AC: #1, #3, #6)
  - [ ] 3.1 Add `GET /api/recruitments` to `RecruitmentEndpoints.cs` -- returns list via MediatR
  - [ ] 3.2 Add `GET /api/recruitments/{id}` to `RecruitmentEndpoints.cs` -- returns single recruitment via MediatR
  - [ ] 3.3 Integration test: GET list returns only user's recruitments
  - [ ] 3.4 Integration test: GET by id returns 200 for member
  - [ ] 3.5 Integration test: GET by id returns 403 for non-member
  - [ ] 3.6 Integration test: GET by id returns 404 for non-existent
- [ ] Task 4: Frontend -- API client and types (AC: #1, #3)
  - [ ] 4.1 Create `web/src/lib/api/recruitments.types.ts` -- `RecruitmentListItem`, `RecruitmentDetail` types
  - [ ] 4.2 Create `web/src/lib/api/recruitments.ts` -- `recruitmentApi.list()`, `recruitmentApi.getById()` using `apiGet`
- [ ] Task 5: Frontend -- TanStack Query hooks (AC: #1, #3)
  - [ ] 5.1 Create `web/src/features/recruitments/hooks/useRecruitments.ts` -- `useRecruitments()` query hook
  - [ ] 5.2 Create `web/src/features/recruitments/hooks/useRecruitmentById.ts` -- `useRecruitmentById(id)` query hook
- [ ] Task 6: Frontend -- RecruitmentList component (AC: #1, #2, #3)
  - [ ] 6.1 Create `web/src/features/recruitments/RecruitmentList.tsx` -- list of recruitment cards/rows with title and status badge
  - [ ] 6.2 Show `SkeletonLoader` during initial load
  - [ ] 6.3 Show existing `EmptyState` component when list is empty (same CTA as Story 1.5)
  - [ ] 6.4 Each recruitment item is clickable, navigates to `/recruitments/{id}`
  - [ ] 6.5 Show `StatusBadge` for Active/Closed status on each item
  - [ ] 6.6 Unit test: renders list of recruitments with titles and status
  - [ ] 6.7 Unit test: renders empty state when no recruitments
  - [ ] 6.8 Unit test: renders skeleton during loading
  - [ ] 6.9 Unit test: clicking recruitment navigates to correct URL
- [ ] Task 7: Frontend -- RecruitmentDetail page placeholder (AC: #3)
  - [ ] 7.1 Create `web/src/features/recruitments/pages/RecruitmentPage.tsx` -- reads `recruitmentId` from URL params, fetches via `useRecruitmentById`, shows title + status
  - [ ] 7.2 Show `SkeletonLoader` while loading
  - [ ] 7.3 Show error message on 403 (forbidden access)
  - [ ] 7.4 Show error message on 404 (not found)
  - [ ] 7.5 Unit test: renders recruitment title when loaded
  - [ ] 7.6 Unit test: shows error on forbidden access
- [ ] Task 8: Frontend -- Route configuration (AC: #3)
  - [ ] 8.1 Update `web/src/routes/index.tsx` -- add `/recruitments/:recruitmentId` route pointing to `RecruitmentPage`
  - [ ] 8.2 Update `HomePage` to render `RecruitmentList` instead of only the empty state
- [ ] Task 9: Frontend -- RecruitmentSelector breadcrumb (AC: #4, #5)
  - [ ] 9.1 Create `web/src/features/recruitments/RecruitmentSelector.tsx` -- breadcrumb dropdown in header showing current recruitment name + switcher
  - [ ] 9.2 Show recruitment name as plain text when only one recruitment exists
  - [ ] 9.3 Show recruitment name with dropdown chevron when multiple recruitments exist
  - [ ] 9.4 Dropdown lists all accessible recruitments with status badge
  - [ ] 9.5 Selecting a recruitment navigates to `/recruitments/{id}` without full page reload
  - [ ] 9.6 Update `AppHeader.tsx` to render `RecruitmentSelector` when on a recruitment route
  - [ ] 9.7 Unit test: renders recruitment name in breadcrumb
  - [ ] 9.8 Unit test: dropdown shows all recruitments
  - [ ] 9.9 Unit test: selecting from dropdown navigates correctly
- [ ] Task 10: Frontend -- MSW handlers and fixtures
  - [ ] 10.1 Create `web/src/mocks/fixtures/recruitments.ts` -- mock recruitment data
  - [ ] 10.2 Add MSW handlers for `GET /api/recruitments` and `GET /api/recruitments/:id` to `web/src/mocks/handlers.ts`

## Dev Notes

### Affected Aggregate(s)

**Recruitment** (aggregate root) -- this story only READS from the Recruitment aggregate. No mutations. The domain model already exists in `api/src/Domain/Entities/Recruitment.cs` with full invariant enforcement. DO NOT modify the domain entities.

Cross-aggregate: None. This story queries Recruitment data only. Candidates are not involved.

### Key Domain Context

- `Recruitment` entity has: `Title`, `Description`, `JobRequisitionId`, `Status` (enum: `Active`/`Closed`), `CreatedAt`, `ClosedAt`, `CreatedByUserId`
- `RecruitmentMember` child entity tracks membership: `UserId`, `Role`, `RecruitmentId`
- Access control is membership-based: only users who are members of a recruitment can see it
- The `ITenantContext.UserId` is populated by web middleware from the authenticated JWT (or dev headers in development)

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (GetRecruitments query) | **Test-first** | Security-critical: must verify membership filtering |
| Task 2 (GetRecruitmentById query) | **Test-first** | Security-critical: must verify 403 for non-members |
| Task 3 (API endpoints) | **Test-first** | Integration boundary: verify status codes + response shapes |
| Task 4 (API client) | **Characterization** | Thin wrapper over httpClient -- test via component integration tests |
| Task 5 (TanStack Query hooks) | **Characterization** | Thin wrapper over API client -- test via component tests |
| Task 6 (RecruitmentList) | **Test-first** | User-facing component with conditional rendering (list/empty/loading) |
| Task 7 (RecruitmentPage) | **Test-first** | User-facing component with error states (403/404) |
| Task 8 (Routes) | **Characterization** | Config glue -- test via navigation tests in Task 6 |
| Task 9 (RecruitmentSelector) | **Test-first** | Interactive dropdown with navigation -- complex user interaction |
| Task 10 (MSW fixtures) | N/A | Test infrastructure only |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **CQRS folder structure:**
   ```
   api/src/Application/Features/Recruitments/
     Queries/
       GetRecruitments/
         GetRecruitmentsQuery.cs
         GetRecruitmentsQueryHandler.cs
         RecruitmentListItemDto.cs
       GetRecruitmentById/
         GetRecruitmentByIdQuery.cs
         GetRecruitmentByIdQueryHandler.cs
         RecruitmentDetailDto.cs
   ```
   Rule: One query per folder. Handler in same folder.

2. **GetRecruitmentsQuery -- membership filtering via ITenantContext:**
   ```csharp
   public record GetRecruitmentsQuery : IRequest<List<RecruitmentListItemDto>>;

   public class GetRecruitmentsQueryHandler(
       IApplicationDbContext dbContext,
       ITenantContext tenantContext)
       : IRequestHandler<GetRecruitmentsQuery, List<RecruitmentListItemDto>>
   {
       public async Task<List<RecruitmentListItemDto>> Handle(
           GetRecruitmentsQuery request,
           CancellationToken cancellationToken)
       {
           var userId = Guid.Parse(tenantContext.UserId!);

           return await dbContext.Recruitments
               .Where(r => r.Members.Any(m => m.UserId == userId))
               .OrderByDescending(r => r.CreatedAt)
               .Select(r => RecruitmentListItemDto.From(r))
               .ToListAsync(cancellationToken);
       }
   }
   ```
   CRITICAL: Filter by `Members.Any(m => m.UserId == userId)` -- this is the security boundary. Do NOT rely solely on EF global query filters for this; the Recruitment entity itself is not tenant-scoped, membership is the access control mechanism.

3. **GetRecruitmentByIdQuery -- membership check + NotFoundException:**
   ```csharp
   public record GetRecruitmentByIdQuery(Guid Id) : IRequest<RecruitmentDetailDto>;
   ```
   Handler must:
   - Load recruitment by Id including Steps (`.Include(r => r.Steps)`)
   - Verify the current user is a member: `r.Members.Any(m => m.UserId == userId)`
   - If recruitment not found: throw `NotFoundException` (returns 404)
   - If user not a member: throw `ForbiddenAccessException` (returns 403)
   - Both exception types exist in the Clean Architecture template

4. **DTOs with manual mapping:**
   ```csharp
   public record RecruitmentListItemDto(
       Guid Id, string Title, string Status, DateTimeOffset CreatedAt)
   {
       public static RecruitmentListItemDto From(Recruitment r) => new(
           r.Id, r.Title, r.Status.ToString(), r.CreatedAt);
   }

   public record RecruitmentDetailDto(
       Guid Id, string Title, string? Description,
       string? JobRequisitionId, string Status,
       DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt,
       List<WorkflowStepDto> Steps)
   {
       public static RecruitmentDetailDto From(Recruitment r) => new(
           r.Id, r.Title, r.Description, r.JobRequisitionId,
           r.Status.ToString(), r.CreatedAt, r.ClosedAt,
           r.Steps.Select(s => new WorkflowStepDto(s.Id, s.Name, s.Order))
               .OrderBy(s => s.Order).ToList());
   }

   public record WorkflowStepDto(Guid Id, string Name, int Order);
   ```

5. **Minimal API endpoints -- add to existing `RecruitmentEndpoints.cs`:**
   ```csharp
   // Story 2.1 already creates this file with POST /api/recruitments
   // Add GET endpoints:
   app.MapGet("/api/recruitments", async (ISender sender) =>
   {
       var result = await sender.Send(new GetRecruitmentsQuery());
       return Results.Ok(result);
   })
   .WithName("GetRecruitments")
   .Produces<List<RecruitmentListItemDto>>();

   app.MapGet("/api/recruitments/{id:guid}", async (Guid id, ISender sender) =>
   {
       var result = await sender.Send(new GetRecruitmentByIdQuery(id));
       return Results.Ok(result);
   })
   .WithName("GetRecruitmentById")
   .Produces<RecruitmentDetailDto>()
   .ProducesProblem(StatusCodes.Status403Forbidden)
   .ProducesProblem(StatusCodes.Status404NotFound);
   ```
   IMPORTANT: `RecruitmentEndpoints.cs` may already exist from Story 2.1 (with POST). Add these GET endpoints to the same file -- do NOT create a new file.

6. **Error handling:** `NotFoundException` and `ForbiddenAccessException` are converted to Problem Details (RFC 9457) by global exception middleware. DO NOT catch exceptions in the handler. The template's `ExceptionHandlingMiddleware` maps:
   - `NotFoundException` -> 404 Problem Details
   - `ForbiddenAccessException` -> 403 Problem Details
   - `ValidationException` -> 400 Problem Details

7. **ITenantContext usage:** This is the FIRST story that uses `ITenantContext` for read queries. The web middleware (`TenantContextMiddleware`) populates `UserId` from the JWT claim (production) or `X-Dev-User-Id` header (dev mode). The handler reads `tenantContext.UserId` to determine the current user.

**Frontend -- MUST follow these patterns:**

1. **Feature folder structure:**
   ```
   web/src/features/recruitments/
     RecruitmentList.tsx
     RecruitmentList.test.tsx
     RecruitmentSelector.tsx
     RecruitmentSelector.test.tsx
     pages/
       HomePage.tsx         (already exists -- modify)
       RecruitmentPage.tsx  (new)
       RecruitmentPage.test.tsx
     hooks/
       useRecruitments.ts
       useRecruitmentById.ts
   web/src/lib/api/
     recruitments.ts
     recruitments.types.ts
   ```

2. **API client using existing httpClient:**
   ```typescript
   // web/src/lib/api/recruitments.ts
   import { apiGet } from './httpClient';
   import type { RecruitmentListItem, RecruitmentDetail } from './recruitments.types';

   export const recruitmentApi = {
     list: () => apiGet<RecruitmentListItem[]>('/recruitments'),
     getById: (id: string) => apiGet<RecruitmentDetail>(`/recruitments/${id}`),
   };
   ```
   CRITICAL: Use `apiGet` from `httpClient.ts`. It handles auth headers and Problem Details parsing. Also include `create` method if Story 2.1 has already been implemented (check).

3. **API types:**
   ```typescript
   // web/src/lib/api/recruitments.types.ts
   export interface RecruitmentListItem {
     id: string;
     title: string;
     status: 'Active' | 'Closed';
     createdAt: string; // ISO 8601
   }

   export interface RecruitmentDetail {
     id: string;
     title: string;
     description: string | null;
     jobRequisitionId: string | null;
     status: 'Active' | 'Closed';
     createdAt: string;
     closedAt: string | null;
     steps: WorkflowStepDto[];
   }

   export interface WorkflowStepDto {
     id: string;
     name: string;
     order: number;
   }
   ```
   Rule: Use `| null` for nullable fields, not `| undefined`. Collections are never null (return `[]`).

4. **TanStack Query hooks:**
   ```typescript
   // hooks/useRecruitments.ts
   import { useQuery } from '@tanstack/react-query';
   import { recruitmentApi } from '@/lib/api/recruitments';

   export function useRecruitments() {
     return useQuery({
       queryKey: ['recruitments'],
       queryFn: recruitmentApi.list,
     });
   }
   ```
   ```typescript
   // hooks/useRecruitmentById.ts
   import { useQuery } from '@tanstack/react-query';
   import { recruitmentApi } from '@/lib/api/recruitments';

   export function useRecruitmentById(id: string | undefined) {
     return useQuery({
       queryKey: ['recruitments', id],
       queryFn: () => recruitmentApi.getById(id!),
       enabled: !!id,
     });
   }
   ```
   Note: `isPending` (not `isLoading`) for TanStack Query v5. Use `retry: 3` (default) for queries, not `retry: false`.

5. **RecruitmentList component:**
   - Use `useRecruitments()` hook
   - Loading state: `SkeletonLoader` component (already exists in `web/src/components/SkeletonLoader.tsx`)
   - Empty state: reuse existing `EmptyState` component with same props as current `HomePage.tsx` (heading: "Create your first recruitment", CTA: "Create Recruitment")
   - Each item shows title + `StatusBadge` for Active/Closed
   - Click navigates via React Router `useNavigate()` to `/recruitments/{id}`
   - Use `<Link>` from `react-router` for proper accessible navigation (not `onClick` + `navigate`)

6. **HomePage refactor:**
   - `HomePage.tsx` currently renders only `EmptyState` with a toast placeholder
   - Replace with `RecruitmentList` component which handles both states internally (list + empty)
   - Remove the `useAppToast` import and "Coming in Epic 2" toast
   - Keep the layout wrapper div for centering

7. **Route configuration:**
   ```typescript
   // web/src/routes/index.tsx -- ADD to existing routeConfig
   import { RecruitmentPage } from '@/features/recruitments/pages/RecruitmentPage'

   // Inside the ProtectedRoute children:
   { path: '/', element: <HomePage /> },
   { path: '/recruitments/:recruitmentId', element: <RecruitmentPage /> },
   ```

8. **RecruitmentSelector breadcrumb in AppHeader:**
   - Only renders when on a `/recruitments/:recruitmentId` route (use `useParams` to detect)
   - Shows: "Recruitment Tracker" > "Recruitment Name" in the header
   - If user has multiple recruitments, the recruitment name is a dropdown trigger (use shadcn/ui `DropdownMenu` component -- already installed)
   - If user has one recruitment, just show the name as plain text (no dropdown)
   - Dropdown items show recruitment title + status badge
   - Selecting an item navigates to that recruitment
   - Use `useRecruitments()` to get the list for the dropdown

9. **Error handling for 403/404:**
   - `RecruitmentPage` must handle `ApiError` from the query
   - 403: show "You don't have access to this recruitment" message with navigation back to home
   - 404: show "Recruitment not found" message with navigation back to home
   - Use TanStack Query's `error` state, check `error instanceof ApiError` and inspect `error.status`

10. **Toast on success:** No toasts needed for navigation. Only the "Create Recruitment" action (Story 2.1) uses toasts.

### Architecture Compliance

- **Read-only queries:** This story has NO mutations. All handlers only query data.
- **ITenantContext for access control:** Every query filters by `tenantContext.UserId` to enforce membership-based access. This is the first story establishing the query-side security pattern.
- **Ubiquitous language:** Use "Recruitment" (not job/position), "Workflow Step" (not stage/phase), "Recruitment Member" (not participant).
- **Manual DTO mapping:** `static From()` factory on response DTOs. NO AutoMapper.
- **Problem Details for errors:** 403 and 404 return RFC 9457 Problem Details. Test both status code AND response shape.
- **No PII in logs:** Never log user names or emails. Use user IDs only.
- **Empty state handling:** `RecruitmentList` MUST have an empty state variant (AC2).
- **Shared components:** Use existing `StatusBadge`, `EmptyState`, `SkeletonLoader`, `ActionButton` -- do NOT create feature-local equivalents.
- **Cross-feature rule:** The `RecruitmentSelector` lives in `features/recruitments/` and is imported by `components/AppHeader.tsx`. This is acceptable because AppHeader is a shell component, not a feature.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. Use `.Include()` for eager loading Steps/Members. |
| MediatR | 13.x | `IRequest<T>`, `IRequestHandler<,>`. Queries return DTOs, not entities. |
| React | 19.x | Controlled components. |
| TypeScript | 5.7.x | Strict mode, `erasableSyntaxOnly` in tsconfig. |
| TanStack Query | 5.x | `useQuery`, `isPending` (not `isLoading`). Default `retry: 3` for queries. |
| react-router | 7.x (declarative mode) | `useParams`, `useNavigate`, `<Link>`, `createBrowserRouter`. NOT framework mode. |
| shadcn/ui | Installed | `DropdownMenu` for recruitment selector. All 18 components available in `web/src/components/ui/`. |
| Tailwind CSS | 4.x | CSS-first config via `@theme` in `index.css`. |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Recruitments/
  Queries/
    GetRecruitments/
      GetRecruitmentsQuery.cs
      GetRecruitmentsQueryHandler.cs
      RecruitmentListItemDto.cs
    GetRecruitmentById/
      GetRecruitmentByIdQuery.cs
      GetRecruitmentByIdQueryHandler.cs
      RecruitmentDetailDto.cs

api/tests/Application.UnitTests/Features/Recruitments/
  Queries/
    GetRecruitments/
      GetRecruitmentsQueryHandlerTests.cs
    GetRecruitmentById/
      GetRecruitmentByIdQueryHandlerTests.cs

api/tests/Application.FunctionalTests/Endpoints/
  RecruitmentEndpointTests.cs  (may already exist from Story 2.1 -- ADD tests)

web/src/lib/api/
  recruitments.ts
  recruitments.types.ts

web/src/features/recruitments/
  RecruitmentList.tsx
  RecruitmentList.test.tsx
  RecruitmentSelector.tsx
  RecruitmentSelector.test.tsx
  pages/
    RecruitmentPage.tsx
    RecruitmentPage.test.tsx
  hooks/
    useRecruitments.ts
    useRecruitmentById.ts

web/src/mocks/fixtures/
  recruitments.ts
```

**Existing files to modify:**
```
api/src/Web/Endpoints/RecruitmentEndpoints.cs  -- Add GET endpoints (may exist from Story 2.1)
web/src/features/recruitments/pages/HomePage.tsx  -- Replace empty state placeholder with RecruitmentList
web/src/routes/index.tsx  -- Add /recruitments/:recruitmentId route
web/src/components/AppHeader.tsx  -- Add RecruitmentSelector breadcrumb
web/src/mocks/handlers.ts  -- Add MSW handlers for GET /api/recruitments and GET /api/recruitments/:id
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**
- GetRecruitmentsQueryHandler tests: mock `IApplicationDbContext` with in-memory data, verify membership filtering returns only user's recruitments
- GetRecruitmentsQueryHandler tests: verify empty list returned when user has no memberships
- GetRecruitmentByIdQueryHandler tests: valid member receives recruitment details including steps
- GetRecruitmentByIdQueryHandler tests: non-member throws `ForbiddenAccessException`
- GetRecruitmentByIdQueryHandler tests: non-existent Id throws `NotFoundException`
- Integration tests: `GET /api/recruitments` returns 200 with filtered list
- Integration tests: `GET /api/recruitments/{id}` returns 200 for member, 403 for non-member, 404 for missing
- Test naming: `MethodName_Scenario_ExpectedBehavior`
- Mock `ITenantContext` to set `UserId` for different test scenarios

**Frontend tests (Vitest + Testing Library + MSW):**
- RecruitmentList: renders list of recruitments with titles and status badges
- RecruitmentList: renders empty state when no recruitments returned
- RecruitmentList: renders skeleton during loading
- RecruitmentList: clicking a recruitment navigates to `/recruitments/{id}`
- RecruitmentPage: renders recruitment title and details when loaded
- RecruitmentPage: shows error message on 403 response
- RecruitmentPage: shows error message on 404 response
- RecruitmentSelector: renders current recruitment name in breadcrumb
- RecruitmentSelector: dropdown shows all recruitments when clicked
- RecruitmentSelector: selecting from dropdown navigates to correct URL
- MSW handlers: `GET /api/recruitments` returns mock list, `GET /api/recruitments/:id` returns mock detail
- Use custom `test-utils.tsx` that wraps with QueryClientProvider + MemoryRouter
- Co-located test files: `Component.test.tsx` next to `Component.tsx`

### Previous Story Intelligence (Epic 1 Learnings)

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- always use `apiGet`/`apiPost` from it
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers
- `erasableSyntaxOnly` in tsconfig means no `public` shorthand in TS constructor parameters

**From Story 1.3 (Core Data Model):**
- `IApplicationDbContext` exposes `DbSet<Recruitment> Recruitments` -- use this in query handlers
- `ITenantContext` has `UserId` (string), `RecruitmentId` (Guid?), `IsServiceContext` (bool)
- `ICurrentUserService` has `UserId` (string) -- used for write operations (creator identity)
- Child entity constructors are `internal` -- only creatable through aggregate root methods
- Domain entities have `private set` properties -- handlers read but never directly set
- NSubstitute for ALL mocking (never Moq)
- MediatR 13: `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`

**From Story 1.4 (Shared UI Components):**
- 18 shadcn/ui components already installed including `DropdownMenu` in `web/src/components/ui/`
- Custom components available: `StatusBadge`, `ActionButton`, `EmptyState`, `SkeletonLoader`, `ErrorBoundary`
- `useAppToast()` hook for toast notifications
- `cn()` utility in `web/src/lib/utils.ts` for className merging

**From Story 1.5 (App Shell):**
- React Router v7 declarative mode -- `createBrowserRouter()` + `<RouterProvider />`
- Route config exported separately for test use with `createMemoryRouter`
- TanStack Query v5: `isPending` replaces `isLoading`
- `ProtectedRoute` wraps all authenticated routes
- CSS Grid layout: `grid-template-rows: 48px 1fr` in `RootLayout.tsx`
- `HomePage.tsx` currently shows `EmptyState` with "Create your first recruitment" -- THIS GETS REPLACED
- `AppHeader.tsx` currently shows "Recruitment Tracker" title + user name + Sign out button -- breadcrumb gets added here

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Recent patterns (Story 1.5):**
- Incremental commits per component with tests included
- Feature folder organization (`features/recruitments/pages/`, `features/auth/`)
- `test-utils.tsx` wraps render with `QueryClientProvider` + `MemoryRouter`

**Current state:** Branch `main`, Story 2.1 is `ready-for-dev` (may or may not be implemented when this story starts). If Story 2.1 has been implemented, `RecruitmentEndpoints.cs`, `recruitments.ts`, and `recruitments.types.ts` may already exist -- extend them rather than overwriting.

### Latest Tech Information

- **.NET 10.0:** LTS. EF Core `.Include()` works with global query filters. `ToListAsync()` materializes the filtered results.
- **EF Core 10:** `Where` + `Any` on navigation property translates to SQL `EXISTS` subquery -- efficient for membership checks.
- **React Router v7:** `useParams()` returns `{ recruitmentId: string | undefined }` -- type-safe param access. `useNavigate()` for programmatic navigation.
- **TanStack Query 5.x:** `enabled` option prevents queries from running until conditions are met (useful for `useRecruitmentById` where `id` might be undefined).
- **shadcn/ui DropdownMenu:** Renders in a portal, uses Radix primitives. Keyboard accessible out of the box. Use `DropdownMenu`, `DropdownMenuTrigger`, `DropdownMenuContent`, `DropdownMenuItem` components.

### Project Structure Notes

- Backend query structure in `api/src/Application/Features/Recruitments/Queries/` is NEW if Story 2.1 hasn't been implemented yet. This story establishes the query-side CQRS pattern.
- Frontend `recruitments.ts` API client may already exist from Story 2.1. If so, add `list()` and `getById()` methods to the existing `recruitmentApi` object.
- Frontend `recruitments.types.ts` may already exist from Story 2.1. If so, add `RecruitmentListItem` and `RecruitmentDetail` types.
- `RecruitmentEndpoints.cs` may already exist from Story 2.1. If so, add GET routes to the existing `MapRecruitmentEndpoints()` method.
- MSW handlers and fixtures may already have recruitment-related entries from Story 2.1 tests.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-recruitment-team-setup.md` -- Story 2.2 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries, ITenantContext, membership-based access, enforcement]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, component structure, empty state pattern, loading states]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Response formats, Problem Details, GET endpoint patterns]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- State management, routing, httpClient contract]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, feature folder boundaries]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Test frameworks, naming conventions, Pragmatic TDD]
- [Source: `api/src/Domain/Entities/Recruitment.cs` -- Existing aggregate root with Members collection]
- [Source: `api/src/Application/Common/Interfaces/IApplicationDbContext.cs` -- DbSet<Recruitment> Recruitments]
- [Source: `api/src/Application/Common/Interfaces/ITenantContext.cs` -- UserId, RecruitmentId, IsServiceContext]
- [Source: `web/src/lib/api/httpClient.ts` -- apiGet, apiPost, ApiError, AuthError]
- [Source: `web/src/features/recruitments/pages/HomePage.tsx` -- Current empty state CTA]
- [Source: `web/src/routes/index.tsx` -- Current route config with ProtectedRoute wrapper]
- [Source: `web/src/components/AppHeader.tsx` -- Current header with title + sign out]
- [Source: `web/src/components/EmptyState.tsx` -- Reusable empty state component]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

# Story 2.4: Team Membership Management

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **recruiting leader (Erik)**,
I want to **invite team members to a recruitment and manage who has access**,
so that **the right people can collaborate on candidate screening and assessment**.

## Acceptance Criteria

### AC1: View member list
**Given** a user is viewing a recruitment they are a member of
**When** they click "Manage Team" or equivalent action
**Then** they see the current list of members with their names and roles

### AC2: Directory search
**Given** a user wants to invite a new member
**When** they open the invite dialog and start typing a name or email
**Then** the system searches the organizational directory (Microsoft Entra ID / Graph API)
**And** matching users are displayed as suggestions

### AC3: Add member
**Given** the user selects a person from the directory search results
**When** they confirm the invitation
**Then** the selected person is added as a member of the recruitment
**And** a `MembershipChangedEvent` is raised and recorded in the audit trail
**And** a success toast confirms the addition

### AC4: Creator is permanent
**Given** the user views the member list
**When** they look at the recruitment creator
**Then** the creator is visually marked as permanent (e.g., "Creator" badge)
**And** no remove action is available for the creator

### AC5: Remove non-creator member
**Given** the user wants to remove a non-creator member
**When** they click the remove action for that member
**Then** the member is removed from the recruitment
**And** that member can no longer see or access the recruitment
**And** a `MembershipChangedEvent` is raised and recorded in the audit trail

### AC6: Cannot remove creator
**Given** the user attempts to remove the recruitment creator
**When** the remove action is attempted
**Then** the action is blocked (button disabled or hidden)
**And** the creator remains a permanent member

### AC7: Access revocation visible
**Given** a member is removed from a recruitment
**When** that removed user navigates to the recruitment list
**Then** the recruitment no longer appears in their list
**And** direct URL access returns 403 Forbidden

### FRs Fulfilled
- **FR56:** Invite authenticated users by searching organizational directory (Entra ID)
- **FR57:** View list of members with access to a recruitment
- **FR58:** Remove a member (except the creator who is permanent)
- **FR61:** Record member additions and removals in audit trail

## Tasks / Subtasks

- [ ] Task 1: Backend -- IDirectoryService interface and dev stub (AC: #2)
  - [ ] 1.1 Create `IDirectoryService` interface in `api/src/Application/Common/Interfaces/` with `SearchUsersAsync(string query, CancellationToken)` returning `IReadOnlyList<DirectoryUser>`
  - [ ] 1.2 Create `DirectoryUser` record in `api/src/Application/Common/Models/` with `Guid Id, string DisplayName, string Email`
  - [ ] 1.3 Create `DevDirectoryService` in `api/src/Infrastructure/Identity/` that returns hardcoded personas (User A, User B, Admin) for development -- registered only in Development environment
  - [ ] 1.4 Create placeholder `EntraIdDirectoryService` in `api/src/Infrastructure/Identity/` that wraps Microsoft Graph API -- initially throws `NotImplementedException` until Entra ID is configured
  - [ ] 1.5 Register DI: `DevDirectoryService` in Development, `EntraIdDirectoryService` in Production

- [ ] Task 2: Backend -- SearchDirectory query (AC: #2)
  - [ ] 2.1 Create `SearchDirectoryQuery` record in `api/src/Application/Features/Team/Queries/SearchDirectory/` with `string SearchTerm` implementing `IRequest<IReadOnlyList<DirectoryUserDto>>`
  - [ ] 2.2 Create `SearchDirectoryQueryValidator` -- `SearchTerm` required, min length 2, max length 100
  - [ ] 2.3 Create `SearchDirectoryQueryHandler` -- delegates to `IDirectoryService.SearchUsersAsync()`
  - [ ] 2.4 Create `DirectoryUserDto` with `static From()` factory
  - [ ] 2.5 Unit test handler: valid search returns mapped results
  - [ ] 2.6 Unit test validator: empty term fails, single char fails, valid term passes

- [ ] Task 3: Backend -- GetMembers query (AC: #1, #4)
  - [ ] 3.1 Create `GetMembersQuery` record in `api/src/Application/Features/Team/Queries/GetMembers/` with `Guid RecruitmentId` implementing `IRequest<MembersListDto>`
  - [ ] 3.2 Create `GetMembersQueryHandler` -- queries `Recruitments` DbSet including Members, maps to DTOs, marks creator with `IsCreator` flag
  - [ ] 3.3 Create `MemberDto` with fields: `Guid Id, Guid UserId, string DisplayName, string Role, bool IsCreator, DateTimeOffset InvitedAt`
  - [ ] 3.4 Create `MembersListDto` with `List<MemberDto> Members, int TotalCount`
  - [ ] 3.5 Unit test handler: returns members with correct IsCreator flag, correct member count

- [ ] Task 4: Backend -- AddMember command (AC: #3)
  - [ ] 4.1 Create `AddMemberCommand` record in `api/src/Application/Features/Team/Commands/AddMember/` with `Guid RecruitmentId, Guid UserId, string DisplayName` implementing `IRequest<Guid>`
  - [ ] 4.2 Create `AddMemberCommandValidator` -- RecruitmentId required, UserId required
  - [ ] 4.3 Create `AddMemberCommandHandler` -- loads Recruitment aggregate (including Members), calls `recruitment.AddMember(userId, role)`, saves
  - [ ] 4.4 Unit test handler: member added, domain event raised, SaveChanges called
  - [ ] 4.5 Unit test handler: duplicate member throws (domain already enforces)
  - [ ] 4.6 Unit test validator: missing RecruitmentId fails, missing UserId fails

- [ ] Task 5: Backend -- RemoveMember command (AC: #5, #6)
  - [ ] 5.1 Create `RemoveMemberCommand` record in `api/src/Application/Features/Team/Commands/RemoveMember/` with `Guid RecruitmentId, Guid MemberId` implementing `IRequest`
  - [ ] 5.2 Create `RemoveMemberCommandValidator` -- RecruitmentId required, MemberId required
  - [ ] 5.3 Create `RemoveMemberCommandHandler` -- loads Recruitment aggregate (including Members), calls `recruitment.RemoveMember(memberId)`, saves
  - [ ] 5.4 Unit test handler: member removed, domain event raised, SaveChanges called
  - [ ] 5.5 Unit test handler: removing creator throws
  - [ ] 5.6 Unit test validator: missing fields fail

- [ ] Task 6: Backend -- Team API endpoints (AC: #1, #2, #3, #5, #7)
  - [ ] 6.1 Create `TeamEndpoints.cs` in `api/src/Web/Endpoints/`
  - [ ] 6.2 Map `GET /api/recruitments/{recruitmentId}/members` -- returns member list
  - [ ] 6.3 Map `GET /api/recruitments/{recruitmentId}/directory-search?q={searchTerm}` -- returns directory search results
  - [ ] 6.4 Map `POST /api/recruitments/{recruitmentId}/members` -- adds member, returns 201
  - [ ] 6.5 Map `DELETE /api/recruitments/{recruitmentId}/members/{memberId}` -- removes member, returns 204
  - [ ] 6.6 Register endpoints in Program.cs: `app.MapTeamEndpoints()`
  - [ ] 6.7 Integration test: GET members returns list with creator flagged
  - [ ] 6.8 Integration test: POST member returns 201, member appears in subsequent GET
  - [ ] 6.9 Integration test: DELETE creator returns 400 Problem Details
  - [ ] 6.10 Integration test: DELETE non-creator returns 204
  - [ ] 6.11 Integration test: directory search returns results

- [ ] Task 7: Frontend -- API client and types (AC: #1, #2, #3, #5)
  - [ ] 7.1 Create `web/src/lib/api/team.types.ts` -- `MemberDto`, `MembersListResponse`, `DirectoryUserDto`, `AddMemberRequest`
  - [ ] 7.2 Create `web/src/lib/api/team.ts` -- `teamApi.getMembers()`, `teamApi.searchDirectory()`, `teamApi.addMember()`, `teamApi.removeMember()` using httpClient helpers

- [ ] Task 8: Frontend -- MemberList component (AC: #1, #4, #6)
  - [ ] 8.1 Create `web/src/features/team/MemberList.tsx` -- displays members with name, role, invited date
  - [ ] 8.2 Show "Creator" badge next to the creator (using existing StatusBadge or a simple badge)
  - [ ] 8.3 Show remove button for non-creator members, hidden/disabled for creator
  - [ ] 8.4 Empty state: "No team members yet" with invite CTA (uses shared EmptyState component)
  - [ ] 8.5 Confirmation dialog before removing a member ("Remove {name} from this recruitment?")
  - [ ] 8.6 Create `web/src/features/team/hooks/useTeamMembers.ts` -- TanStack Query `useQuery` wrapper for getMembers, `useMutation` for add/remove
  - [ ] 8.7 Tests: renders member list, creator badge shown, remove button hidden for creator, removal confirmation dialog

- [ ] Task 9: Frontend -- InviteMemberDialog with directory search (AC: #2, #3)
  - [ ] 9.1 Create `web/src/features/team/InviteMemberDialog.tsx` -- dialog with search input, results list, invite action
  - [ ] 9.2 Debounced search input (min 2 chars) using `useDebounce` hook
  - [ ] 9.3 Display search results with name and email, click to select
  - [ ] 9.4 On invite: call addMember mutation, show success toast, close dialog, invalidate members query
  - [ ] 9.5 Handle already-a-member error (API returns 400 Problem Details) -- show inline error
  - [ ] 9.6 Loading state: skeleton/spinner during search
  - [ ] 9.7 Tests: search triggers API call after debounce, selecting user and confirming calls API, success toast shown, error displayed for duplicate
  - [ ] 9.8 MSW handlers for `GET /api/recruitments/:id/directory-search` and `POST /api/recruitments/:id/members`

- [ ] Task 10: Frontend -- Wire team management into recruitment view (AC: #1)
  - [ ] 10.1 Add "Manage Team" button/tab to recruitment detail view (from Story 2.2)
  - [ ] 10.2 Route: team management accessible within recruitment context (`/recruitments/:id/team` or as a panel/tab)
  - [ ] 10.3 Test: "Manage Team" action opens member list view

## Dev Notes

### Affected Aggregate(s)

**Recruitment** (aggregate root) -- this is the only aggregate touched by this story. The domain model already has full member management support:
- `Recruitment.AddMember(userId, role)` -- adds member, raises `MembershipChangedEvent`, throws if duplicate
- `Recruitment.RemoveMember(memberId)` -- removes member, raises `MembershipChangedEvent`, throws if creator or last leader
- `RecruitmentMember` entity has `internal` constructor -- only creatable through aggregate root

Cross-aggregate: None. Member management is entirely within the Recruitment aggregate.

**Domain methods already implemented (Story 1.3):**
- `AddMember()` enforces: no duplicate userId, recruitment not closed
- `RemoveMember()` enforces: cannot remove creator, cannot remove last Recruiting Leader, recruitment not closed
- `MembershipChangedEvent` already defined in `api/src/Domain/Events/MembershipChangedEvent.cs`
- `RecruitmentMemberConfiguration` already exists with unique constraint `UQ_RecruitmentMembers_RecruitmentId_UserId`

DO NOT modify domain entities -- they are complete.

### Directory Service Pattern

The `IDirectoryService` is a new infrastructure service that abstracts organizational directory access:

- **Development:** `DevDirectoryService` returns hardcoded users matching the dev auth personas. This allows full-stack development without an Entra ID tenant.
- **Production:** `EntraIdDirectoryService` calls Microsoft Graph API (`/users?$search=...`). Requires `User.Read.All` or `People.Read.All` application permission.

**Safety:** The dev stub is registered ONLY in `IHostEnvironment.IsDevelopment()` (runtime check, NOT `#if DEBUG`).

**Display name resolution:** When adding a member, the `DisplayName` comes from the directory search result. However, `RecruitmentMember` entity currently only stores `UserId` and `Role` -- it does NOT store the display name. The `GetMembersQuery` handler must resolve display names from the directory service (or accept that in development mode names come from the dev stub). For MVP, consider storing `DisplayName` on `RecruitmentMember` at invite time as a denormalized snapshot (avoids Graph API call on every member list load).

**Architecture decision needed:** The current `RecruitmentMember` entity lacks a `DisplayName` property. The handler options are:
1. **Add `DisplayName` to entity** (preferred for MVP) -- simple denormalization, add via aggregate root method, add EF config. Avoids N+1 Graph API calls.
2. **Resolve at query time** -- call `IDirectoryService` for each member's userId on every list load. More accurate but slower and fragile.

Recommend option 1: add `DisplayName` to `RecruitmentMember` entity. This requires a minor domain change and EF migration.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (IDirectoryService + stubs) | **Spike** | New infrastructure integration, uncertain Graph API shape. Tests added for dev stub. |
| Task 2 (SearchDirectory query) | **Test-first** | Application layer query with validation |
| Task 3 (GetMembers query) | **Test-first** | Application layer query, must verify IsCreator flag logic |
| Task 4 (AddMember command) | **Test-first** | Core business operation through aggregate root |
| Task 5 (RemoveMember command) | **Test-first** | Core business operation with invariant enforcement |
| Task 6 (API endpoints) | **Test-first** | Integration boundary -- 201/204/400 responses and Problem Details |
| Task 7 (API client) | **Characterization** | Thin wrapper over httpClient -- test via component integration |
| Task 8 (MemberList) | **Test-first** | User-facing component with conditional UI (creator badge, remove button) |
| Task 9 (InviteMemberDialog) | **Test-first** | Complex interaction: debounced search, selection, error handling |
| Task 10 (Wiring) | **Characterization** | Glue code -- test navigation opens team view |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **CQRS folder structure:**
   ```
   api/src/Application/Features/Team/
     Commands/
       AddMember/
         AddMemberCommand.cs
         AddMemberCommandValidator.cs
         AddMemberCommandHandler.cs
       RemoveMember/
         RemoveMemberCommand.cs
         RemoveMemberCommandValidator.cs
         RemoveMemberCommandHandler.cs
     Queries/
       GetMembers/
         GetMembersQuery.cs
         GetMembersQueryHandler.cs
         MemberDto.cs
         MembersListDto.cs
       SearchDirectory/
         SearchDirectoryQuery.cs
         SearchDirectoryQueryValidator.cs
         SearchDirectoryQueryHandler.cs
         DirectoryUserDto.cs
   ```
   Rule: One command/query per folder. Handler in same folder.

2. **New interface -- IDirectoryService:**
   ```csharp
   // api/src/Application/Common/Interfaces/IDirectoryService.cs
   public interface IDirectoryService
   {
       Task<IReadOnlyList<DirectoryUser>> SearchUsersAsync(
           string searchTerm, CancellationToken cancellationToken);
   }
   ```

3. **DirectoryUser model:**
   ```csharp
   // api/src/Application/Common/Models/DirectoryUser.cs
   public record DirectoryUser(Guid Id, string DisplayName, string Email);
   ```

4. **AddMember command pattern:**
   ```csharp
   public record AddMemberCommand(
       Guid RecruitmentId, Guid UserId, string DisplayName) : IRequest<Guid>;

   public class AddMemberCommandHandler(
       IApplicationDbContext dbContext)
       : IRequestHandler<AddMemberCommand, Guid>
   {
       public async Task<Guid> Handle(
           AddMemberCommand request, CancellationToken cancellationToken)
       {
           var recruitment = await dbContext.Recruitments
               .Include(r => r.Members)
               .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
               ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

           recruitment.AddMember(request.UserId, "SME/Collaborator");
           await dbContext.SaveChangesAsync(cancellationToken);
           return recruitment.Members.First(m => m.UserId == request.UserId).Id;
       }
   }
   ```
   Note: Role defaults to "SME/Collaborator" in MVP (no role selection UI). Role-based access is Growth scope.

5. **RemoveMember command pattern:**
   ```csharp
   public record RemoveMemberCommand(
       Guid RecruitmentId, Guid MemberId) : IRequest;

   public class RemoveMemberCommandHandler(
       IApplicationDbContext dbContext)
       : IRequestHandler<RemoveMemberCommand>
   {
       public async Task Handle(
           RemoveMemberCommand request, CancellationToken cancellationToken)
       {
           var recruitment = await dbContext.Recruitments
               .Include(r => r.Members)
               .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
               ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

           recruitment.RemoveMember(request.MemberId);
           await dbContext.SaveChangesAsync(cancellationToken);
       }
   }
   ```

6. **Minimal API endpoints:**
   ```csharp
   // api/src/Web/Endpoints/TeamEndpoints.cs
   public static class TeamEndpoints
   {
       public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/recruitments/{recruitmentId}")
               .RequireAuthorization();

           group.MapGet("/members", GetMembers);
           group.MapGet("/directory-search", SearchDirectory);
           group.MapPost("/members", AddMember);
           group.MapDelete("/members/{memberId}", RemoveMember);
       }
   }
   ```

7. **Error handling:** Domain exceptions (`InvalidOperationException` for duplicate/creator removal) are converted to Problem Details by global exception middleware. DO NOT catch domain exceptions in the handler.

8. **ITenantContext:** The `GetMembersQuery` handler must scope queries via `ITenantContext` -- the user must be a member of the recruitment to view its members. The directory search endpoint does NOT need tenant scoping (searching the org directory is not recruitment-specific).

**Frontend -- MUST follow these patterns:**

1. **Feature folder structure:**
   ```
   web/src/features/team/
     MemberList.tsx
     MemberList.test.tsx
     InviteMemberDialog.tsx
     InviteMemberDialog.test.tsx
     hooks/
       useTeamMembers.ts
       useTeamMembers.test.ts
   web/src/lib/api/
     team.ts
     team.types.ts
   ```

2. **API client using existing httpClient:**
   ```typescript
   // web/src/lib/api/team.ts
   import { apiGet, apiPost, apiDelete } from './httpClient';
   import type {
     MembersListResponse,
     DirectoryUserDto,
     AddMemberRequest,
   } from './team.types';

   export const teamApi = {
     getMembers: (recruitmentId: string) =>
       apiGet<MembersListResponse>(`/recruitments/${recruitmentId}/members`),
     searchDirectory: (recruitmentId: string, query: string) =>
       apiGet<DirectoryUserDto[]>(
         `/recruitments/${recruitmentId}/directory-search?q=${encodeURIComponent(query)}`),
     addMember: (recruitmentId: string, data: AddMemberRequest) =>
       apiPost<{ id: string }>(`/recruitments/${recruitmentId}/members`, data),
     removeMember: (recruitmentId: string, memberId: string) =>
       apiDelete(`/recruitments/${recruitmentId}/members/${memberId}`),
   };
   ```
   CRITICAL: Use `apiGet`/`apiPost`/`apiDelete` from `httpClient.ts`.

3. **TanStack Query hooks:**
   ```typescript
   // hooks/useTeamMembers.ts
   export function useTeamMembers(recruitmentId: string) {
     return useQuery({
       queryKey: ['recruitment', recruitmentId, 'members'],
       queryFn: () => teamApi.getMembers(recruitmentId),
     });
   }

   export function useAddMember(recruitmentId: string) {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (data: AddMemberRequest) =>
         teamApi.addMember(recruitmentId, data),
       onSuccess: () => {
         queryClient.invalidateQueries({
           queryKey: ['recruitment', recruitmentId, 'members'],
         });
       },
     });
   }

   export function useRemoveMember(recruitmentId: string) {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (memberId: string) =>
         teamApi.removeMember(recruitmentId, memberId),
       onSuccess: () => {
         queryClient.invalidateQueries({
           queryKey: ['recruitment', recruitmentId, 'members'],
         });
       },
     });
   }
   ```
   Note: `isPending` (not `isLoading`) for TanStack Query v5.

4. **Debounced directory search:**
   ```typescript
   // In InviteMemberDialog.tsx
   const [searchTerm, setSearchTerm] = useState('');
   const debouncedTerm = useDebounce(searchTerm, 300);

   const { data: searchResults, isPending } = useQuery({
     queryKey: ['directory-search', recruitmentId, debouncedTerm],
     queryFn: () => teamApi.searchDirectory(recruitmentId, debouncedTerm),
     enabled: debouncedTerm.length >= 2,
   });
   ```
   Use existing `useDebounce` hook from `web/src/hooks/useDebounce.ts`.

5. **Toast notifications:** Use `useAppToast()` hook:
   - `toast({ title: "Member added", variant: "success" })` -- 3s auto-dismiss
   - `toast({ title: "Member removed", variant: "success" })` -- 3s auto-dismiss

6. **Empty state:** Use shared `EmptyState` component for member list when no non-creator members exist.

7. **shadcn/ui components:** Dialog for invite, Button for actions, use existing components from `web/src/components/ui/`.

### Architecture Compliance

- **Aggregate root access only:** Call `recruitment.AddMember()` and `recruitment.RemoveMember()`. NEVER directly modify `_members` collection or use `dbContext.RecruitmentMembers.Add()`.
- **Ubiquitous language:** Use "Recruitment Member" (not participant/user), "Recruiting Leader" (not manager/owner), "SME/Collaborator" (not reviewer).
- **Manual DTO mapping:** `static From()` factory on response DTOs. NO AutoMapper.
- **Problem Details for errors:** Duplicate member (400), creator removal (400), recruitment not found (404) -- all RFC 9457 Problem Details.
- **No PII in audit events/logs:** `MembershipChangedEvent` contains only `RecruitmentId` and `UserId` (Guids), never names or emails.
- **EF Core Fluent API only:** `RecruitmentMemberConfiguration` already exists. If adding `DisplayName` property, update the configuration.
- **NSubstitute for ALL mocking** (never Moq).
- **MediatR v13+:** `RequestHandlerDelegate` takes `CancellationToken`.
- **ITenantContext required:** GetMembers must verify user is a member of the recruitment. The global query filter handles data isolation.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. Include `Members` navigation for aggregate loading. |
| MediatR | 13.x | `IRequest<T>`, `IRequestHandler<,>`, pipeline behaviors for validation |
| FluentValidation | Latest | `AbstractValidator<T>`, registered via DI |
| Microsoft.Graph | Latest | For `EntraIdDirectoryService` (production). Application permissions: `User.Read.All` |
| React | 19.x | Controlled components |
| TypeScript | 5.7.x | Strict mode, `erasableSyntaxOnly` |
| TanStack Query | 5.x | `useMutation`, `isPending`, `retry: false` for mutations, `enabled` for conditional queries |
| shadcn/ui | Installed | Dialog, Button, Input components |
| Tailwind CSS | 4.x | CSS-first config via `@theme` |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Common/Interfaces/
  IDirectoryService.cs

api/src/Application/Common/Models/
  DirectoryUser.cs

api/src/Application/Features/Team/
  Commands/
    AddMember/
      AddMemberCommand.cs
      AddMemberCommandValidator.cs
      AddMemberCommandHandler.cs
    RemoveMember/
      RemoveMemberCommand.cs
      RemoveMemberCommandValidator.cs
      RemoveMemberCommandHandler.cs
  Queries/
    GetMembers/
      GetMembersQuery.cs
      GetMembersQueryHandler.cs
      MemberDto.cs
      MembersListDto.cs
    SearchDirectory/
      SearchDirectoryQuery.cs
      SearchDirectoryQueryValidator.cs
      SearchDirectoryQueryHandler.cs
      DirectoryUserDto.cs

api/src/Infrastructure/Identity/
  DevDirectoryService.cs
  EntraIdDirectoryService.cs

api/src/Web/Endpoints/
  TeamEndpoints.cs

api/tests/Application.UnitTests/Features/Team/
  Commands/
    AddMember/
      AddMemberCommandHandlerTests.cs
      AddMemberCommandValidatorTests.cs
    RemoveMember/
      RemoveMemberCommandHandlerTests.cs
      RemoveMemberCommandValidatorTests.cs
  Queries/
    GetMembers/
      GetMembersQueryHandlerTests.cs
    SearchDirectory/
      SearchDirectoryQueryHandlerTests.cs
      SearchDirectoryQueryValidatorTests.cs

api/tests/Application.FunctionalTests/Endpoints/
  TeamEndpointTests.cs

web/src/features/team/
  MemberList.tsx
  MemberList.test.tsx
  InviteMemberDialog.tsx
  InviteMemberDialog.test.tsx
  hooks/
    useTeamMembers.ts
    useTeamMembers.test.ts

web/src/lib/api/
  team.ts
  team.types.ts

web/src/mocks/fixtures/
  team.ts
```

**Existing files to modify:**
```
api/src/Web/Program.cs                  -- Register endpoint mapping: app.MapTeamEndpoints()
api/src/Infrastructure/DependencyInjection.cs  -- Register IDirectoryService implementations
api/src/Domain/Entities/RecruitmentMember.cs   -- MAYBE add DisplayName property (see Dev Notes)
api/src/Domain/Entities/Recruitment.cs          -- MAYBE update AddMember() signature for DisplayName
api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs  -- MAYBE add DisplayName config
web/src/mocks/handlers.ts              -- Add MSW handlers for team endpoints
web/src/routes/index.tsx               -- Add team route under recruitment
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**

*Unit tests:*
- `AddMemberCommandHandler` -- mock `IApplicationDbContext`, verify recruitment loaded with Include, `AddMember()` called, `SaveChangesAsync` called, returns member Id
- `AddMemberCommandHandler_DuplicateUser_ThrowsInvalidOperationException` -- domain enforces, handler passes through
- `AddMemberCommandValidator` -- missing RecruitmentId fails, missing UserId fails, valid input passes
- `RemoveMemberCommandHandler` -- mock context, verify `RemoveMember()` called, `SaveChangesAsync` called
- `RemoveMemberCommandHandler_CreatorRemoval_ThrowsInvalidOperationException` -- domain enforces
- `RemoveMemberCommandValidator` -- missing fields fail
- `GetMembersQueryHandler` -- returns members with correct IsCreator flag based on `CreatedByUserId`
- `SearchDirectoryQueryHandler` -- mock `IDirectoryService`, verify delegation, mapping
- `SearchDirectoryQueryValidator` -- empty fails, single char fails, valid passes

*Integration tests:*
- `GET /api/recruitments/{id}/members` -- returns member list with creator flagged
- `POST /api/recruitments/{id}/members` -- valid request returns 201, member in list
- `POST /api/recruitments/{id}/members` -- duplicate returns 400 Problem Details
- `DELETE /api/recruitments/{id}/members/{memberId}` -- non-creator returns 204
- `DELETE /api/recruitments/{id}/members/{creatorMemberId}` -- returns 400 Problem Details
- `GET /api/recruitments/{id}/directory-search?q=test` -- returns search results

Test naming: `MethodName_Scenario_ExpectedBehavior`

**Frontend tests (Vitest + Testing Library + MSW):**
- MemberList: renders member names and roles, creator badge shown, remove button hidden for creator
- MemberList: clicking remove shows confirmation dialog, confirming calls API
- MemberList: empty state shown when no non-creator members
- InviteMemberDialog: search input triggers API after debounce (300ms)
- InviteMemberDialog: selecting user and confirming calls addMember API
- InviteMemberDialog: success toast shown on successful add
- InviteMemberDialog: error message shown for duplicate member
- InviteMemberDialog: loading state during search
- MSW handlers: `GET /api/recruitments/:id/members`, `GET /api/recruitments/:id/directory-search`, `POST /api/recruitments/:id/members`, `DELETE /api/recruitments/:id/members/:memberId`
- Use custom `test-utils.tsx` that wraps with QueryClientProvider + MemoryRouter
- Co-located test files: `Component.test.tsx` next to `Component.tsx`

### Previous Story Intelligence (Epic 1 + Epic 2 Learnings)

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- always use `apiGet`/`apiPost`/`apiDelete`
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers
- `erasableSyntaxOnly` in tsconfig means no `public` shorthand in TS constructor parameters

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- Domain methods (`AddMember`, `RemoveMember`) already implemented and tested
- `RecruitmentMember` constructor is `internal` -- only creatable through `Recruitment.AddMember()`
- Properties use `{ get; private set; }` -- respect this pattern if adding `DisplayName`
- `ApplicationDbContext` constructor requires `ITenantContext` -- mock it in tests
- `MembershipChangedEvent` exists: `(Guid RecruitmentId, Guid UserId, string ChangeType)`

**From Story 1.4 (Shared UI Components):**
- shadcn/ui components available: Dialog, Button, Input, and more in `web/src/components/ui/`
- `useAppToast()` for toast notifications (3s auto-dismiss for success)
- `EmptyState` component for empty lists
- `cn()` utility for className merging

**From Story 1.5 (App Shell):**
- React Router v7 declarative mode -- `createBrowserRouter()` + `<RouterProvider />`
- Route config exported separately for test use
- TanStack Query v5: `isPending` replaces `isLoading`
- `useDebounce` hook exists in `web/src/hooks/useDebounce.ts`

**From Story 2.1 (Create Recruitment -- ready-for-dev):**
- CQRS folder pattern established: `Features/{Area}/Commands/{CommandName}/` and `Features/{Area}/Queries/{QueryName}/`
- Minimal API endpoint pattern: `MapGroup()` with `RequireAuthorization()`
- Response DTO with `static From()` factory method
- MSW handler pattern for frontend tests

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Recent patterns (Story 1.5):**
- Per-component commits with tests included
- Feature folders organized consistently
- `test-utils.tsx` wraps providers for all component tests

### Latest Tech Information

- **.NET 10.0:** LTS. Primary constructors for DI injection in handlers.
- **Microsoft.Graph SDK:** v5.x for .NET. Use `GraphServiceClient` with client credentials flow for app-only permissions (`User.Read.All`). The `$search` query parameter requires `ConsistencyLevel: eventual` header.
- **EF Core 10:** Include navigation properties for aggregate loading. `Include(r => r.Members)` is the standard pattern.
- **React 19.2:** Controlled components. No form actions for SPA.
- **TanStack Query 5.x:** `enabled` parameter for conditional queries (directory search only when search term >= 2 chars). `isPending` for loading states.

### Project Structure Notes

- Alignment with unified project structure: `Features/Team/` follows the pattern from `Features/Recruitments/`
- Frontend `features/team/` matches architecture doc's component map
- API types in `web/src/lib/api/team.ts` follow httpClient pattern
- Infrastructure services in `api/src/Infrastructure/Identity/` follows existing pattern (`CurrentUserService.cs`, `TenantContext.cs`)
- `IDirectoryService` is a NEW infrastructure interface -- follows the same abstraction pattern as `ICurrentUserService`

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-recruitment-team-setup.md` -- Story 2.4 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries (Recruitment owns RecruitmentMember), ITenantContext, Entra ID directory integration]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, component structure, empty state pattern]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Response formats (201 Created, 204 No Content, Problem Details)]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Features/Team/ folder, TeamEndpoints.cs, EntraIdDirectoryService.cs]
- [Source: `_bmad-output/planning-artifacts/architecture/dev-auth-patterns.md` -- Dev auth bypass, personas for development directory service]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- httpClient pattern, TanStack Query hooks, feature folder structure]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- NUnit, NSubstitute, FluentAssertions, MSW, Vitest]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR56-FR61 Team Management requirements]
- [Source: `api/src/Domain/Entities/Recruitment.cs` -- AddMember(), RemoveMember() aggregate methods]
- [Source: `api/src/Domain/Entities/RecruitmentMember.cs` -- Entity with internal constructor]
- [Source: `api/src/Domain/Events/MembershipChangedEvent.cs` -- Domain event definition]
- [Source: `api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs` -- EF Core configuration]
- [Source: `web/src/lib/api/httpClient.ts` -- HTTP client with dual auth path]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

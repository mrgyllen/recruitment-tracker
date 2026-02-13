# Story 1.3: Core Data Model & Tenant Isolation

Status: done

## Story

As a **developer**,
I want the domain entities, database configuration, and per-recruitment data isolation in place with verified security tests,
so that all subsequent feature stories can persist and query data safely within recruitment boundaries.

## Acceptance Criteria

1. **Domain entities exist:** The Domain project contains entities: `Recruitment`, `WorkflowStep`, `RecruitmentMember`, `Candidate`, `CandidateOutcome`, `CandidateDocument`, `ImportSession`, `AuditEntry`. Entities enforce aggregate boundaries (child entities modified only through aggregate root methods). Cross-aggregate references use IDs only (no navigation properties across aggregates).

2. **EF Core configuration:** Each entity has a Fluent API configuration file in `Data/Configurations/`. No data annotations exist on domain entities. `ApplicationDbContext` applies global query filters via `ITenantContext` on all candidate-related entities.

3. **Tenant context middleware:** `TenantContextMiddleware` populates `ITenantContext.UserId` from the authenticated JWT when a web request is processed.

4. **Cross-recruitment isolation (positive):** User A is a member of Recruitment 1 but not Recruitment 2. When User A queries candidates, only candidates from Recruitment 1 are returned. Candidates from Recruitment 2 are never visible.

5. **Import service scoping:** When the import service sets `ITenantContext.RecruitmentId`, operations are scoped to that specific recruitment only.

6. **GDPR service bypass:** When the GDPR service sets `ITenantContext.IsServiceContext = true`, the global query filter is bypassed and all expired recruitments are queryable.

7. **Default empty context safety:** When `ITenantContext` has no user, no recruitment ID, and no service flag, queries return zero results (not an error).

8. **Audit pipeline:** `AuditBehaviour` is registered in the MediatR pipeline. When any command is dispatched, an `AuditEntry` is created capturing who, when, and what changed. No PII is stored in the audit event context (IDs and metadata only).

## Tasks / Subtasks

- [ ] **Task 1: Create domain enums** (AC: 1)
  - [ ] Create `api/src/Domain/Enums/OutcomeStatus.cs` — values: `NotStarted`, `Pass`, `Fail`, `Hold`
  - [ ] Create `api/src/Domain/Enums/ImportMatchConfidence.cs` — values: `High`, `Low`, `None`
  - [ ] Create `api/src/Domain/Enums/RecruitmentStatus.cs` — values: `Active`, `Closed`
  - [ ] Create `api/src/Domain/Enums/ImportSessionStatus.cs` — values: `Processing`, `Completed`, `Failed`
  - [ ] **Testing mode: Test-first** — These are simple value types but define the domain vocabulary. Write tests verifying enum values match expected set to prevent accidental renames/removals.

- [ ] **Task 2: Create value objects** (AC: 1)
  - [ ] Create `api/src/Domain/ValueObjects/CandidateMatch.cs` — immutable, holds `ImportMatchConfidence Confidence` and `string MatchMethod`. Override equality based on value semantics.
  - [ ] Create `api/src/Domain/ValueObjects/AnonymizationResult.cs` — immutable, holds `int CandidatesAnonymized` and `int DocumentsDeleted`.
  - [ ] **Testing mode: Test-first** — Value objects must have value-based equality. Write tests for equality, inequality, and immutability.

- [ ] **Task 3: Create domain events** (AC: 1, 8)
  - [ ] Create `api/src/Domain/Events/CandidateImportedEvent.cs` — implements `MediatR.INotification`
  - [ ] Create `api/src/Domain/Events/OutcomeRecordedEvent.cs`
  - [ ] Create `api/src/Domain/Events/DocumentUploadedEvent.cs`
  - [ ] Create `api/src/Domain/Events/RecruitmentCreatedEvent.cs`
  - [ ] Create `api/src/Domain/Events/RecruitmentClosedEvent.cs`
  - [ ] Create `api/src/Domain/Events/MembershipChangedEvent.cs`
  - [ ] Each event carries only IDs (entity ID, aggregate root ID) — no PII
  - [ ] **Testing mode: Spike** — Events are data records with no logic. Verify they implement `INotification`. No behavior to test-first.

- [ ] **Task 4: Create domain exceptions** (AC: 1)
  - [ ] Create `api/src/Domain/Exceptions/RecruitmentClosedException.cs` — thrown when modifying a closed recruitment
  - [ ] Create `api/src/Domain/Exceptions/DuplicateCandidateException.cs` — thrown when email already exists in recruitment
  - [ ] Create `api/src/Domain/Exceptions/DuplicateStepNameException.cs` — thrown when adding a workflow step with a name that already exists in the recruitment
  - [ ] Create `api/src/Domain/Exceptions/InvalidWorkflowTransitionException.cs` — thrown for invalid step progression
  - [ ] Create `api/src/Domain/Exceptions/StepHasOutcomesException.cs` — thrown when deleting a step with recorded outcomes
  - [ ] **Testing mode: Spike** — Exceptions are simple types. They're tested indirectly via aggregate behavior tests in Task 6.

- [ ] **Task 5: Inspect BaseEntity and template patterns** (AC: 1) — *execute BEFORE Task 6; depends on Tasks 1–4 only*
  - [ ] Inspect the existing `api/src/Domain/Common/BaseEntity.cs` from the Jason Taylor template
  - [ ] Determine if template provides `BaseAuditableEntity` with `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` — if so, entities that need audit timestamps should extend it instead of `BaseEntity`
  - [ ] Confirm domain events collection (`DomainEvents` list) is available on base entity — this is how entities raise events (e.g., `AddDomainEvent(new RecruitmentCreatedEvent(...))`)
  - [ ] Document findings so Task 6 can use the correct base classes and event-raising pattern
  - [ ] **Domain event publishing mechanism:** Entities add events to the `DomainEvents` collection. The template typically provides a `DispatchDomainEventsInterceptor` or similar that publishes events via `MediatR.Publish()` during `SaveChangesAsync()`. Verify this exists; if not, create it in Task 8.
  - [ ] **Testing mode: Spike** — Template exploration. Findings directly inform Task 6 entity design.

- [ ] **Task 6: Create aggregate root entities with invariants** (AC: 1) — *depends on Tasks 1–4 and Task 5*
  - [ ] Create `api/src/Domain/Entities/Recruitment.cs` (aggregate root)
    - [ ] Properties: `Id` (Guid), `Title` (string), `Description` (string?), `JobRequisitionId` (string? — external Workday reference), `Status` (RecruitmentStatus), `CreatedAt` (DateTimeOffset), `ClosedAt` (DateTimeOffset?), `CreatedByUserId` (Guid)
    - [ ] Owns: `IReadOnlyCollection<WorkflowStep> Steps`, `IReadOnlyCollection<RecruitmentMember> Members`
    - [ ] Aggregate methods: `AddStep(name, order)`, `RemoveStep(stepId)`, `AddMember(userId, role)`, `RemoveMember(memberId)`, `Close()`
    - [ ] Invariants: Cannot modify when closed (throws `RecruitmentClosedException`). Step names unique within recruitment (throws `DuplicateStepNameException`). Step order contiguous. Cannot remove step with recorded outcomes (throws `StepHasOutcomesException`). At least one Recruiting Leader must exist. Creator member cannot be removed.
    - [ ] **Note:** "Cannot close with active candidates in progress" is a cross-aggregate invariant — enforced at the command handler level (Epic 2, CloseRecruitment command), not in the domain entity. The entity's `Close()` method only sets status and `ClosedAt`.
    - [ ] **Note:** Default workflow template initialization (PRD FR5 standard steps) is an application-layer concern handled by the CreateRecruitment command handler (Epic 2). The domain entity only provides `AddStep()`.
    - [ ] Raises: `RecruitmentCreatedEvent` on creation, `RecruitmentClosedEvent` on close, `MembershipChangedEvent` on member changes
  - [ ] Create `api/src/Domain/Entities/WorkflowStep.cs` (child of Recruitment)
    - [ ] Properties: `Id` (Guid), `RecruitmentId` (Guid), `Name` (string), `Order` (int), `CreatedAt` (DateTimeOffset)
    - [ ] No public constructor — created via `Recruitment.AddStep()`
  - [ ] Create `api/src/Domain/Entities/RecruitmentMember.cs` (child of Recruitment)
    - [ ] Properties: `Id` (Guid), `RecruitmentId` (Guid), `UserId` (Guid), `Role` (string), `InvitedAt` (DateTimeOffset)
    - [ ] No public constructor — created via `Recruitment.AddMember()`
  - [ ] Create `api/src/Domain/Entities/Candidate.cs` (aggregate root)
    - [ ] Properties: `Id` (Guid), `RecruitmentId` (Guid — ID-only cross-aggregate ref), `FullName` (string), `Email` (string), `PhoneNumber` (string?), `Location` (string?), `DateApplied` (DateTimeOffset), `CreatedAt` (DateTimeOffset)
    - [ ] Owns: `IReadOnlyCollection<CandidateOutcome> Outcomes`, `IReadOnlyCollection<CandidateDocument> Documents`
    - [ ] Aggregate methods: `RecordOutcome(workflowStepId, status)`, `AttachDocument(type, blobUrl)`, `Anonymize()`
    - [ ] Invariants: Cannot delete candidate with recorded outcomes. One primary document per `DocumentType` (throws if duplicate type attached).
    - [ ] **Note:** "Cannot record outcome on non-existent step reference" is a cross-aggregate invariant — the Candidate entity holds only `WorkflowStepId` (Guid) and has no knowledge of valid step IDs. This validation is enforced at the command handler level (Epic 2), which checks the Recruitment aggregate's steps before calling `candidate.RecordOutcome()`. The domain entity method itself does not validate step existence.
    - [ ] `Anonymize()` sets PII fields to null: `FullName`, `Email`, `PhoneNumber`, `Location`. Preserves: `Id`, `RecruitmentId`, `DateApplied`, `CreatedAt`, and all `CandidateOutcome` records (aggregate metrics). This is permanent and irreversible.
    - [ ] **Note:** Initial step placement (PRD FR62: "new candidates placed at first step with Not Started") is application-layer logic in the import/create-candidate command handlers (Epic 3). The domain entity does not track a "current step" — step progression is derived from the `Outcomes` collection.
    - [ ] Raises: `OutcomeRecordedEvent`, `DocumentUploadedEvent`, `CandidateImportedEvent`
  - [ ] Create `api/src/Domain/Entities/CandidateOutcome.cs` (child of Candidate)
    - [ ] Properties: `Id` (Guid), `CandidateId` (Guid), `WorkflowStepId` (Guid — ID-only cross-aggregate ref), `Status` (OutcomeStatus), `RecordedAt` (DateTimeOffset), `RecordedByUserId` (Guid)
    - [ ] No public constructor — created via `Candidate.RecordOutcome()`
  - [ ] Create `api/src/Domain/Entities/CandidateDocument.cs` (child of Candidate)
    - [ ] Properties: `Id` (Guid), `CandidateId` (Guid), `DocumentType` (string), `BlobStorageUrl` (string), `UploadedAt` (DateTimeOffset)
    - [ ] No public constructor — created via `Candidate.AttachDocument()`
  - [ ] Create `api/src/Domain/Entities/ImportSession.cs` (aggregate root)
    - [ ] Properties: `Id` (Guid), `RecruitmentId` (Guid — ID-only cross-aggregate ref), `Status` (ImportSessionStatus), `CreatedAt` (DateTimeOffset), `CompletedAt` (DateTimeOffset?), `TotalRows` (int), `SuccessfulRows` (int), `FailedRows` (int), `CreatedByUserId` (Guid)
    - [ ] Aggregate methods: `MarkCompleted(successCount, failCount)`, `MarkFailed(reason)`
    - [ ] Properties (continued): `FailureReason` (string? — max 2000 chars, no PII)
    - [ ] Invariants: Status transitions: `Processing` -> `Completed` or `Processing` -> `Failed` only. No backward transitions. `MarkCompleted` sets `TotalRows = successCount + failCount`. `MarkFailed(reason)` stores reason in `FailureReason` (max 2000 chars, no PII).
    - [ ] **Note:** Row-level import results (per-candidate match details) are tracked as value objects within ImportSession in story 3.x. This story implements only aggregate counts.
  - [ ] Create `api/src/Domain/Entities/AuditEntry.cs` (standalone, no aggregate root)
    - [ ] Properties: `Id` (Guid), `RecruitmentId` (Guid), `EntityId` (Guid?), `EntityType` (string), `ActionType` (string), `PerformedBy` (Guid), `PerformedAt` (DateTimeOffset), `Context` (string? — JSON, NO PII)
    - [ ] Append-only: no update/delete methods
  - [ ] **Testing mode: Test-first** — Domain logic is the highest-value test target. Write tests for every invariant and aggregate method BEFORE implementing. Tests go in `api/tests/Domain.UnitTests/Entities/`.
    - [ ] `RecruitmentTests.cs`: AddStep (success + duplicate name throws `DuplicateStepNameException`), RemoveStep (success + has outcomes throws `StepHasOutcomesException`), AddMember, RemoveMember (last leader prevented), RemoveMember (creator prevented), Close (success + already closed throws `RecruitmentClosedException`), modify-when-closed throws
    - [ ] `CandidateTests.cs`: RecordOutcome (success), AttachDocument (success + duplicate type rejected), Anonymize (sets FullName/Email/PhoneNumber/Location to null, preserves Id/RecruitmentId/DateApplied/CreatedAt/Outcomes)
    - [ ] `ImportSessionTests.cs`: MarkCompleted (success, sets TotalRows), MarkFailed (success), invalid transitions (Completed -> Processing, Failed -> Processing, Completed -> Failed)

- [ ] **Task 7: Create EF Core configurations** (AC: 2) — *depends on Tasks 5, 6*
  - [ ] Create `api/src/Infrastructure/Data/Configurations/RecruitmentConfiguration.cs` — Fluent API: table name `Recruitments`, required fields, one-to-many relationships for Steps and Members via `HasMany(...).WithOne()` (NOT EF Core `OwnsMany` — Steps and Members have separate tables and independent primary keys)
  - [ ] Create `api/src/Infrastructure/Data/Configurations/WorkflowStepConfiguration.cs` — FK to Recruitment, unique constraint `UQ_WorkflowSteps_RecruitmentId_Name`, index `IX_WorkflowSteps_RecruitmentId`
  - [ ] Create `api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs` — FK to Recruitment, unique constraint `UQ_RecruitmentMembers_RecruitmentId_UserId`, index `IX_RecruitmentMembers_UserId`
  - [ ] Create `api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs` — FK to none (ID-only ref to Recruitment), unique constraint `UQ_Candidates_RecruitmentId_Email`, index `IX_Candidates_RecruitmentId`
  - [ ] Create `api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs` — FK to Candidate, index `IX_CandidateOutcomes_CandidateId_WorkflowStepId`
  - [ ] Create `api/src/Infrastructure/Data/Configurations/CandidateDocumentConfiguration.cs` — FK to Candidate, index `IX_CandidateDocuments_CandidateId`
  - [ ] Create `api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs` — index `IX_ImportSessions_RecruitmentId`, `HasMaxLength(2000)` on `FailureReason` column
  - [ ] Create `api/src/Infrastructure/Data/Configurations/AuditEntryConfiguration.cs` — index `IX_AuditEntries_RecruitmentId_PerformedAt`
  - [ ] **All configurations use Fluent API ONLY. Zero data annotations on domain entities.**
  - [ ] **Testing mode: Spike** — Configuration correctness is verified through migration generation (Task 9) and integration tests (Task 10).

- [ ] **Task 8: Configure ApplicationDbContext with global query filters** (AC: 2, 3, 4, 5, 6, 7) — *depends on Tasks 5, 6, 7*
  - [ ] Update `api/src/Infrastructure/Data/ApplicationDbContext.cs`:
    - [ ] Add `DbSet<T>` properties for aggregate roots and standalone entities: `DbSet<Recruitment>`, `DbSet<Candidate>`, `DbSet<ImportSession>`, `DbSet<AuditEntry>`. Child entities (`WorkflowStep`, `RecruitmentMember`, `CandidateOutcome`, `CandidateDocument`) are accessed only via their parent's navigation properties — do NOT expose them as top-level DbSets.
    - [ ] Update `IApplicationDbContext` interface (provided by Jason Taylor template at `api/src/Application/Common/Interfaces/IApplicationDbContext.cs`) to include the new `DbSet<T>` properties
    - [ ] Inject `ITenantContext` (created in story 1.2) via constructor
    - [ ] Apply configurations via `modelBuilder.ApplyConfigurationsFromAssembly()`
    - [ ] Add global query filters in `OnModelCreating()`:
      - Filter `Candidate` by recruitment membership: candidates visible only if user is a member of the candidate's recruitment, OR `ITenantContext.RecruitmentId` matches (import service), OR `ITenantContext.IsServiceContext == true` (GDPR job)
      - Filter `CandidateOutcome` — cascades through Candidate navigation
      - Filter `CandidateDocument` — cascades through Candidate navigation
      - When no context is set (no UserId, no RecruitmentId, no IsServiceContext) → filter returns zero results
    - [ ] **Do NOT filter** `Recruitment`, `WorkflowStep`, `RecruitmentMember` by global filter — these are filtered at the endpoint authorization level. Global filters apply to candidate-related entities only.
  - [ ] Create `api/src/Web/Middleware/TenantContextMiddleware.cs`:
    - [ ] Populates `ITenantContext.UserId` from authenticated JWT claims (delegates to the `ITenantContext` implementation created in story 1.2)
    - [ ] Runs early in pipeline after authentication but before endpoint handlers
    - [ ] **Note:** Story 1.2 created `ITenantContext` and `TenantContext` with lazy resolution via DI. This middleware explicitly sets `UserId` from claims on every request so it's available before the first DbContext query.
  - [ ] Register `TenantContextMiddleware` in `Program.cs` pipeline after `UseAuthorization()`
  - [ ] **Testing mode: Test-first** — Write integration tests (Task 11) before implementing the filter logic. The query filter IS the security boundary.

- [ ] **Task 9: Create initial EF Core migration** (AC: 2) — *depends on Tasks 7, 8*
  - [ ] Run `dotnet ef migrations add InitialSchema` from the Infrastructure project
  - [ ] Verify migration generates cleanly with no errors
  - [ ] Review generated migration for correct table names, column types, indexes, and constraints
  - [ ] Verify migration can be applied: `dotnet ef database update` (against local SQL instance or in-memory)
  - [ ] **Testing mode: Spike** — Migration generation is a mechanical operation. Review output for correctness.

- [ ] **Task 10: Create AuditBehaviour pipeline** (AC: 8) — *depends on Tasks 6, 8*
  - [ ] Create `api/src/Application/Common/Behaviours/AuditBehaviour.cs`:
    - [ ] Implements `IPipelineBehavior<TRequest, TResponse>` (MediatR)
    - [ ] **Pipeline order:** Runs AFTER `ValidationBehaviour` (only audit successful commands, not validation failures). Registration order in DI: `ValidationBehaviour` first, then `AuditBehaviour`.
    - [ ] For every command (not query), creates an `AuditEntry` with: `RecruitmentId`, `EntityId`, `EntityType`, `ActionType`, `PerformedBy` (from `ITenantContext.UserId`), `PerformedAt` (DateTimeOffset.UtcNow), `Context` (JSON — **NO PII**)
    - [ ] **Audit Context field allowlist:** `Context` JSON may contain: entity IDs, action type, status changes (enum values), counts (e.g., "candidatesImported: 5"). **Forbidden fields:** names, emails, phone numbers, free-text notes, file contents, any direct personal identifiers.
    - [ ] Saves `AuditEntry` to `IApplicationDbContext`
  - [ ] Distinguish commands from queries: Convention-based (class name ends with `Command`) or marker interface approach — follow the pattern the Jason Taylor template already uses
  - [ ] **Testing mode: Test-first** — Write tests verifying: audit entry created for commands, no audit entry for queries, no audit entry for validation failures, PII-free context (assert Context does NOT contain known PII patterns), correct field population. Use NSubstitute for `IApplicationDbContext` mock.

- [ ] **Task 11: Write mandatory security isolation tests** (AC: 4, 5, 6, 7) — *depends on Tasks 8, 9*
  - [ ] Create `api/tests/Infrastructure.IntegrationTests/Data/TenantContextFilterTests.cs`:
    - [ ] **Test 1:** User in Recruitment A cannot see candidates from Recruitment B — set `ITenantContext.UserId` to user who is a member of Recruitment A, seed candidates in both A and B, assert only A's candidates returned
    - [ ] **Test 2:** Import service can write to assigned recruitment — set `ITenantContext.RecruitmentId`, assert candidates scoped to that recruitment
    - [ ] **Test 3:** GDPR service can query all expired recruitments — set `ITenantContext.IsServiceContext = true`, assert all recruitments queryable
    - [ ] **Test 4:** Misconfigured context returns zero results — no UserId, no RecruitmentId, `IsServiceContext = false`, assert empty result set (no exception)
    - [ ] **Test 5:** UserId set + RecruitmentId set — verify both are respected (OR logic: user sees their memberships AND the specific recruitment)
    - [ ] **Test 6:** IsServiceContext = true overrides UserId filter — service context bypasses all restrictions regardless of UserId
    - [ ] **Test 7:** User who is member of multiple recruitments — sees candidates from all their recruitments
    - [ ] **Test 8:** User removed from recruitment — immediately loses access to that recruitment's candidates
  - [ ] These tests require a real database (SQL Server or Testcontainers with SQL Server). Use the Infrastructure.IntegrationTests project.
  - [ ] **Testing mode: Test-first** — Write these tests FIRST. They define the security contract. Implementation in Task 8 must make them pass.

- [ ] **Task 12: Write domain entity unit tests** (AC: 1) — *should be written BEFORE Task 6 implementation (test-first)*
  - [ ] Create `api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`
  - [ ] Create `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`
  - [ ] Create `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`
  - [ ] Create `api/tests/Domain.UnitTests/ValueObjects/CandidateMatchTests.cs`
  - [ ] Create `api/tests/Domain.UnitTests/ValueObjects/AnonymizationResultTests.cs`
  - [ ] Test aggregate invariants through root methods only (see Task 6 test list)
  - [ ] **Testing mode: Test-first** — Write failing tests for each invariant, then implement entity to make them pass.

- [ ] **Task 13: Verify build and all tests pass** (AC: all) — *depends on Tasks 1–12*
  - [ ] Run `dotnet build api/api.sln` — zero errors
  - [ ] Run `dotnet test api/api.sln` — all tests pass (including new domain unit tests, integration tests, and existing template tests)
  - [ ] Run `dotnet format api/api.sln --verify-no-changes` — code style compliant
  - [ ] Verify migration applies cleanly
  - [ ] **Testing mode: N/A** — Final verification.

## Dev Notes

- **Affected aggregate(s):** All three aggregate roots are created in this story: **Recruitment** (with WorkflowStep, RecruitmentMember children), **Candidate** (with CandidateOutcome, CandidateDocument children), **ImportSession** (standalone). Plus **AuditEntry** (standalone, no aggregate).
- **Cross-aggregate interactions:** Cross-aggregate references use IDs only. `Candidate.RecruitmentId` is a Guid, not a navigation property. `CandidateOutcome.WorkflowStepId` is a Guid, not a navigation to `WorkflowStep`.

### Critical Architecture Constraints

**IMPORTANT: Stories 1-1 (scaffolding) and 1-2 (SSO authentication) must be completed first.** This story assumes:
- `api/` directory exists with Clean Architecture template structure
- `ITenantContext` interface exists at `api/src/Application/Common/Interfaces/ITenantContext.cs` (created in story 1.2)
- `TenantContext` implementation exists at `api/src/Infrastructure/Identity/TenantContext.cs` (created in story 1.2)
- `ICurrentUserService` interface and `CurrentUserService` implementation exist (created in story 1.2)
- Authentication middleware pipeline is configured in `Program.cs` (story 1.2)

**Aggregate Boundary Enforcement (MANDATORY):**
- Child entities modified ONLY through aggregate root methods
- `recruitment.AddStep(name, order)` — NOT `dbContext.WorkflowSteps.Add(step)`
- `candidate.RecordOutcome(stepId, status)` — NOT `dbContext.CandidateOutcomes.Add(outcome)`
- Unit of persistence = entire aggregate (load and save the whole aggregate)
- One aggregate per transaction (MediatR command = one transaction)

**Domain Layer Rules:**
- Domain project has ZERO dependencies on Application, Infrastructure, or Web
- No EF Core attributes, no data annotations — entities are technology-agnostic
- Domain events implement `MediatR.INotification` (MediatR is the one allowed dependency via the template's `Domain.csproj`)
- Domain exceptions extend `Exception` (no framework dependency)

**EF Core Configuration Rules:**
- Fluent API ONLY in configuration classes — zero data annotations on entities
- Table names: PascalCase plural (`Recruitments`, `Candidates`, `WorkflowSteps`)
- Column names: PascalCase matching C# property names (EF Core default — no translation layer)
- Foreign keys: `{Entity}Id` convention
- Indexes: `IX_{Table}_{Columns}` naming
- Unique constraints: `UQ_{Table}_{Columns}` naming
- No AutoMapper — manual `ToDto()` / `From()` mappings when DTOs are needed (not needed in this story)

**Global Query Filter Strategy:**
- Filters apply to candidate-related entities only (`Candidate`, `CandidateOutcome`, `CandidateDocument`)
- `Recruitment`, `WorkflowStep`, `RecruitmentMember` are NOT globally filtered — access controlled at endpoint level
- `AuditEntry` is NOT globally filtered — filtered at query level
- `ImportSession` is NOT globally filtered — filtered at query level
- Filter logic: returns data if user is a member of the candidate's recruitment, OR `RecruitmentId` matches (import), OR `IsServiceContext` is true (GDPR), OR returns empty if none set

**NSubstitute Mocking (MANDATORY):**
- All backend mocking uses NSubstitute. Do NOT use Moq.
- Pattern: `var tenantContext = Substitute.For<ITenantContext>();`

**Testing: Pragmatic TDD**

This story's domain entities and security isolation are the highest-value test targets in the entire project. The global query filter IS the security boundary — it must be tested before any feature depends on it.

| Task | Mode | Rationale |
|------|------|-----------|
| Tasks 1-2 (enums, value objects) | Test-first | Simple but define domain vocabulary — tests prevent accidental renames |
| Task 3 (domain events) | Spike | Data records with no logic |
| Task 4 (exceptions) | Spike | Tested indirectly via aggregate tests |
| Task 5 (BaseEntity inspection) | Spike | Template exploration — must run BEFORE Task 6 |
| Task 6 (aggregate entities) | Test-first | Core domain logic — highest-value test target |
| Task 7 (EF configurations) | Spike | Verified via migration + integration tests |
| Task 8 (DbContext + filters) | Test-first | Security boundary — integration tests first |
| Task 10 (AuditBehaviour) | Test-first | Cross-cutting concern, must be reliable |
| Task 11 (security tests) | Test-first | Write BEFORE implementation code |
| Task 12 (domain entity tests) | Test-first | Write BEFORE implementation code |

**Tests added by this story:**
- `Domain.UnitTests/Entities/RecruitmentTests.cs` — aggregate invariants (add/remove step, duplicate step name, add/remove member, creator protection, close)
- `Domain.UnitTests/Entities/CandidateTests.cs` — aggregate invariants (record outcome, attach document, duplicate document type, anonymize sets FullName/Email/PhoneNumber/Location to null while preserving Id/RecruitmentId/DateApplied/CreatedAt/Outcomes)
- `Domain.UnitTests/Entities/ImportSessionTests.cs` — state machine transitions (all valid + all invalid)
- `Domain.UnitTests/ValueObjects/CandidateMatchTests.cs` — value equality
- `Domain.UnitTests/ValueObjects/AnonymizationResultTests.cs` — value equality
- `Application.UnitTests/Common/Behaviours/AuditBehaviourTests.cs` — audit entry creation for commands, skip for queries, skip for validation failures, PII-free context assertion
- `Infrastructure.IntegrationTests/Data/TenantContextFilterTests.cs` — 8 security scenarios (4 core + 4 edge cases)

**Risk covered:** Data isolation is the security foundation. If global query filters fail, all subsequent stories have data leaks. Test-first approach ensures the filter contract is verified before any feature code depends on it.

### Previous Story Intelligence

**Story 1-1 (Project Scaffolding):**
- Project structure: `api/` with Clean Architecture template (Domain, Application, Infrastructure, Web), `web/` with Vite + React
- Test infrastructure: xUnit + NSubstitute (backend), Vitest + Testing Library + MSW (frontend)
- CI: `.github/workflows/ci.yml` runs `dotnet build`, `dotnet test`, `dotnet format`, `npm ci`, `npm run lint`, `npm run build`, `npm run test`
- Template version: Jason Taylor Clean Architecture v10.0.0, .NET 10.0.2

**Story 1-2 (SSO Authentication):**
- Created `ITenantContext` interface at `api/src/Application/Common/Interfaces/ITenantContext.cs` with `UserId`, `RecruitmentId`, `IsServiceContext` properties
- Created `TenantContext` implementation at `api/src/Infrastructure/Identity/TenantContext.cs` — wraps `ICurrentUserService` for `UserId`, settable `RecruitmentId` and `IsServiceContext` (defaulting to null/false)
- Created `ICurrentUserService` at `api/src/Application/Common/Interfaces/ICurrentUserService.cs` — extracts user ID from JWT claims
- Created `CurrentUserService` at `api/src/Infrastructure/Identity/CurrentUserService.cs`
- Both registered as scoped services in DI
- **No `TenantContextMiddleware` yet** — tenant resolution was lazy via DI. This story creates the explicit middleware.
- Dev auth bypass: `DevelopmentAuthenticationConfiguration.cs` reads `X-Dev-User-Id`/`X-Dev-User-Name` headers for development mode
- Auth pipeline order: `UseAuthentication()` -> `UseAuthorization()` -> endpoints

### File Structure (What This Story Creates)

```
api/src/
  Domain/
    Enums/
      OutcomeStatus.cs
      ImportMatchConfidence.cs
      RecruitmentStatus.cs
      ImportSessionStatus.cs
    ValueObjects/
      CandidateMatch.cs
      AnonymizationResult.cs
    Events/
      CandidateImportedEvent.cs
      OutcomeRecordedEvent.cs
      DocumentUploadedEvent.cs
      RecruitmentCreatedEvent.cs
      RecruitmentClosedEvent.cs
      MembershipChangedEvent.cs
    Exceptions/
      RecruitmentClosedException.cs
      DuplicateCandidateException.cs
      DuplicateStepNameException.cs
      InvalidWorkflowTransitionException.cs
      StepHasOutcomesException.cs
    Entities/
      Recruitment.cs
      WorkflowStep.cs
      RecruitmentMember.cs
      Candidate.cs
      CandidateOutcome.cs
      CandidateDocument.cs
      ImportSession.cs
      AuditEntry.cs
  Application/
    Common/
      Behaviours/
        AuditBehaviour.cs
  Infrastructure/
    Data/
      ApplicationDbContext.cs          # Updated with DbSets + global query filters
      Configurations/
        RecruitmentConfiguration.cs
        WorkflowStepConfiguration.cs
        RecruitmentMemberConfiguration.cs
        CandidateConfiguration.cs
        CandidateOutcomeConfiguration.cs
        CandidateDocumentConfiguration.cs
        ImportSessionConfiguration.cs
        AuditEntryConfiguration.cs
      Migrations/
        {timestamp}_InitialSchema.cs   # Generated migration
  Web/
    Middleware/
      TenantContextMiddleware.cs

api/tests/
  Domain.UnitTests/
    Entities/
      RecruitmentTests.cs
      CandidateTests.cs
      ImportSessionTests.cs
    ValueObjects/
      CandidateMatchTests.cs
      AnonymizationResultTests.cs
  Application.UnitTests/
    Common/
      Behaviours/
        AuditBehaviourTests.cs
  Infrastructure.IntegrationTests/
    Data/
      TenantContextFilterTests.cs
```

### Entity Relationship Quick Reference

```
Recruitment (aggregate root)
  ├── WorkflowStep (child, owned collection)
  └── RecruitmentMember (child, owned collection)

Candidate (aggregate root)
  ├── CandidateOutcome (child, references WorkflowStep via ID only)
  └── CandidateDocument (child)

ImportSession (aggregate root, standalone)

AuditEntry (standalone, append-only, not an aggregate)
```

**Cross-aggregate ID references (Guid only, no navigation properties):**
- `Candidate.RecruitmentId` -> `Recruitment.Id`
- `CandidateOutcome.WorkflowStepId` -> `WorkflowStep.Id`
- `ImportSession.RecruitmentId` -> `Recruitment.Id`
- `AuditEntry.RecruitmentId` -> `Recruitment.Id`

### Database Schema Reference

| Table | Key Columns | Indexes | Unique Constraints |
|-------|-------------|---------|-------------------|
| `Recruitments` | Id, Title, Description, JobRequisitionId, Status, CreatedAt, ClosedAt, CreatedByUserId | PK | — |
| `WorkflowSteps` | Id, RecruitmentId, Name, Order, CreatedAt | `IX_WorkflowSteps_RecruitmentId` | `UQ_WorkflowSteps_RecruitmentId_Name` |
| `RecruitmentMembers` | Id, RecruitmentId, UserId, Role, InvitedAt | `IX_RecruitmentMembers_UserId` | `UQ_RecruitmentMembers_RecruitmentId_UserId` |
| `Candidates` | Id, RecruitmentId, FullName, Email, PhoneNumber, Location, DateApplied, CreatedAt | `IX_Candidates_RecruitmentId` | `UQ_Candidates_RecruitmentId_Email` |
| `CandidateOutcomes` | Id, CandidateId, WorkflowStepId, Status, RecordedAt, RecordedByUserId | `IX_CandidateOutcomes_CandidateId_WorkflowStepId` | — |
| `CandidateDocuments` | Id, CandidateId, DocumentType, BlobStorageUrl, UploadedAt | `IX_CandidateDocuments_CandidateId` | — |
| `ImportSessions` | Id, RecruitmentId, Status, CreatedAt, CompletedAt, TotalRows, SuccessfulRows, FailedRows, FailureReason, CreatedByUserId | `IX_ImportSessions_RecruitmentId` | — |
| `AuditEntries` | Id, RecruitmentId, EntityId, EntityType, ActionType, PerformedBy, PerformedAt, Context | `IX_AuditEntries_RecruitmentId_PerformedAt` | — |

### Global Query Filter Logic (Pseudocode)

```csharp
// In ApplicationDbContext.OnModelCreating()
modelBuilder.Entity<Candidate>().HasQueryFilter(c =>
    // Service context bypasses all filters (GDPR job)
    _tenantContext.IsServiceContext ||
    // Import service scoped to specific recruitment
    (_tenantContext.RecruitmentId != null && c.RecruitmentId == _tenantContext.RecruitmentId) ||
    // Web user: only candidates in recruitments where user is a member
    (_tenantContext.UserId != null &&
     c.Recruitment.Members.Any(m => m.UserId == _tenantContext.UserId))
);
// Note: CandidateOutcome and CandidateDocument cascade through Candidate navigation
```

**IMPORTANT:** The filter above references `c.Recruitment.Members` which requires a **read-only navigation from Candidate to Recruitment** specifically for the query filter. This is an EF Core configuration concern ONLY:
- The domain entity `Candidate` MUST NOT have a `Recruitment` navigation property (aggregate boundary rule).
- The EF Core configuration must solve this via one of: (a) a shadow navigation property configured in `CandidateConfiguration.cs` using `HasOne<Recruitment>().WithMany().HasForeignKey(c => c.RecruitmentId)` — this creates a navigation usable in the filter expression without polluting the domain entity; or (b) a raw SQL subquery approach if shadow navigation doesn't work with `HasQueryFilter`.
- Test both approaches during Task 8 implementation. Shadow navigation is the preferred approach.

### Anti-Patterns to Avoid

- **Do NOT add navigation properties across aggregate boundaries in domain entities.** Cross-aggregate references are IDs only (`Guid RecruitmentId`, not `Recruitment Recruitment`). The shadow navigation for the query filter is an EF Core configuration concern only — it must NOT appear on the domain entity class.
- **Do NOT use data annotations** (`[Required]`, `[MaxLength]`, etc.) on domain entities. Use Fluent API exclusively.
- **Do NOT use AutoMapper.** Manual mapping via `ToDto()` / `From()` methods.
- **Do NOT use Moq.** NSubstitute is mandatory for all backend mocking.
- **Do NOT bypass aggregate roots.** Never `dbContext.WorkflowSteps.Add()` — always `recruitment.AddStep()`.
- **Do NOT store PII in AuditEntry.Context.** IDs and metadata only. No names, emails, or phone numbers.
- **Do NOT filter Recruitment/WorkflowStep/RecruitmentMember via global query filters.** These are filtered at endpoint authorization level only.
- **Do NOT create API endpoints or frontend code.** This story is backend domain + infrastructure only.
- **Do NOT install new NuGet packages** unless the Jason Taylor template doesn't already include them. The template provides EF Core, MediatR, FluentValidation, NSubstitute, xUnit.
- **Do NOT use `#if DEBUG` preprocessor directives.** Use runtime `IHostEnvironment.IsDevelopment()` checks.
- **Do NOT add `public` constructors to child entities** (WorkflowStep, RecruitmentMember, CandidateOutcome, CandidateDocument). Use `private` or `internal` constructors. EF Core can materialize entities via parameterless private constructors.
- **Do NOT use `{ get; set; }` on entity properties.** Use `{ get; private set; }` or `{ get; init; }` for encapsulation. State changes go through aggregate root methods only.
- **Do NOT use EF Core `OwnsMany`/`OwnsOne`** for WorkflowStep, RecruitmentMember, CandidateOutcome, or CandidateDocument. These have separate tables and independent primary keys — configure as standard one-to-many relationships via `HasMany(...).WithOne()`.
- **Do NOT expose child entities as top-level `DbSet<T>`** in ApplicationDbContext. Only aggregate roots (`Recruitment`, `Candidate`, `ImportSession`) and standalone entities (`AuditEntry`) get DbSets.

### What This Story Does NOT Include

- No API endpoints (starts in Epic 2)
- No frontend code (frontend stories are 1.4, 1.5)
- No seed data (development seed data is acceptable but not required)
- No Blob Storage integration (story 3.x)
- No XLSX parsing (story 3.x)
- No PDF splitting (story 3.x)
- No import pipeline hosted service (story 3.x)
- No GDPR retention service (story 6.x)
- No `EntraIdDirectoryService` (story 2.4)
- No default workflow template initialization — application-layer concern in CreateRecruitment command (Epic 2)
- No candidate initial step placement — application-layer concern in import/create commands (Epic 3)
- No "cannot close with active candidates" enforcement — cross-aggregate check in CloseRecruitment command handler (Epic 2)
- No ImportSession row-level results — deferred to story 3.x (this story implements aggregate counts only)

### Project Structure Notes

- All new files follow the architecture document's directory structure exactly
- Domain entities go in `api/src/Domain/Entities/`
- Enums go in `api/src/Domain/Enums/`
- Value objects go in `api/src/Domain/ValueObjects/`
- Events go in `api/src/Domain/Events/`
- Exceptions go in `api/src/Domain/Exceptions/`
- EF configurations go in `api/src/Infrastructure/Data/Configurations/`
- Middleware goes in `api/src/Web/Middleware/`
- Behaviours go in `api/src/Application/Common/Behaviours/`
- Domain unit tests mirror source: `api/tests/Domain.UnitTests/Entities/`
- Integration tests go in `api/tests/Infrastructure.IntegrationTests/Data/`

### References

- [Source: `_bmad-output/planning-artifacts/architecture.md` (core) — Aggregate Boundaries, Data Architecture, ITenantContext, Enforcement Guidelines]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` — Naming Conventions, Structure Patterns, DTO Mapping, Test Conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` — Project Structure, Directory Tree]
- [Source: `_bmad-output/planning-artifacts/epics/epic-1-project-foundation-user-access.md` — Story 1.3 definition and acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` — GDPR requirements, security NFRs, data model requirements]
- [Source: `_bmad-output/implementation-artifacts/1-1-project-scaffolding-ci-pipeline.md` — Project structure, template version, CI pipeline]
- [Source: `_bmad-output/implementation-artifacts/1-2-sso-authentication.md` — ITenantContext creation, auth pipeline, dev auth bypass]
- [Source: `docs/testing-pragmatic-tdd.md` — Testing policy]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

- Cascading compile errors when deleting template artifacts (Colour.cs -> TodoItem.cs -> GlobalUsings.cs)
- ApplicationDbContext constructor change broke SqlTestcontainersTestDatabase.cs and SqlTestDatabase.cs -- added ITenantContext mock
- MediatR 13 RequestHandlerDelegate takes CancellationToken -- changed test lambda from `()` to `(_)`
- Application.UnitTests require ASP.NET Core runtime (not installed locally) -- compile-only verification, runs in CI

### Completion Notes List

- All 3 aggregate roots implemented with invariants: Recruitment (with WorkflowStep, RecruitmentMember), Candidate (with CandidateOutcome, CandidateDocument), ImportSession
- AuditEntry standalone append-only entity
- GuidEntity base class (parallel to template's BaseEntity) for Guid IDs + domain events
- Global query filter on Candidate via shadow navigation property to Recruitment
- AuditBehaviour MediatR pipeline for command auditing
- Template code fully removed (TodoItem/TodoList/WeatherForecasts)
- Review fix: Creator-cannot-be-removed invariant added to Recruitment.RemoveMember()
- Review fix: No-op TenantContextMiddleware removed (lazy DI handles resolution)
- Review fix: [NotMapped] removed from GuidEntity (configs already Ignore())
- Review fix: 4 additional tenant isolation security tests (tests 5-8)

### File List

**Created:**
- `api/src/Domain/Common/GuidEntity.cs`
- `api/src/Domain/Enums/OutcomeStatus.cs`
- `api/src/Domain/Enums/ImportMatchConfidence.cs`
- `api/src/Domain/Enums/RecruitmentStatus.cs`
- `api/src/Domain/Enums/ImportSessionStatus.cs`
- `api/src/Domain/ValueObjects/CandidateMatch.cs`
- `api/src/Domain/ValueObjects/AnonymizationResult.cs`
- `api/src/Domain/Events/CandidateImportedEvent.cs`
- `api/src/Domain/Events/OutcomeRecordedEvent.cs`
- `api/src/Domain/Events/DocumentUploadedEvent.cs`
- `api/src/Domain/Events/RecruitmentCreatedEvent.cs`
- `api/src/Domain/Events/RecruitmentClosedEvent.cs`
- `api/src/Domain/Events/MembershipChangedEvent.cs`
- `api/src/Domain/Exceptions/RecruitmentClosedException.cs`
- `api/src/Domain/Exceptions/DuplicateCandidateException.cs`
- `api/src/Domain/Exceptions/DuplicateStepNameException.cs`
- `api/src/Domain/Exceptions/InvalidWorkflowTransitionException.cs`
- `api/src/Domain/Exceptions/StepHasOutcomesException.cs`
- `api/src/Domain/Entities/Recruitment.cs`
- `api/src/Domain/Entities/WorkflowStep.cs`
- `api/src/Domain/Entities/RecruitmentMember.cs`
- `api/src/Domain/Entities/Candidate.cs`
- `api/src/Domain/Entities/CandidateOutcome.cs`
- `api/src/Domain/Entities/CandidateDocument.cs`
- `api/src/Domain/Entities/ImportSession.cs`
- `api/src/Domain/Entities/AuditEntry.cs`
- `api/src/Infrastructure/Data/Configurations/RecruitmentConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/WorkflowStepConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/CandidateDocumentConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/AuditEntryConfiguration.cs`
- `api/src/Application/Common/Behaviours/AuditBehaviour.cs`
- `api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`
- `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`
- `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`
- `api/tests/Domain.UnitTests/ValueObjects/CandidateMatchTests.cs`
- `api/tests/Domain.UnitTests/ValueObjects/AnonymizationResultTests.cs`
- `api/tests/Domain.UnitTests/Enums/EnumValueTests.cs`
- `api/tests/Application.UnitTests/Common/Behaviours/AuditBehaviourTests.cs`
- `api/tests/Infrastructure.IntegrationTests/Data/TenantContextFilterTests.cs`

**Modified:**
- `api/src/Application/Common/Interfaces/IApplicationDbContext.cs` -- new DbSets
- `api/src/Infrastructure/Data/ApplicationDbContext.cs` -- new DbSets, ITenantContext injection, global query filter
- `api/src/Application/DependencyInjection.cs` -- AuditBehaviour registration
- `api/src/Web/Program.cs` -- removed TenantContextMiddleware registration
- `api/tests/Application.FunctionalTests/SqlTestcontainersTestDatabase.cs` -- ITenantContext mock
- `api/tests/Application.FunctionalTests/SqlTestDatabase.cs` -- ITenantContext mock
- `api/tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs` -- removed TodoItem reference
- `api/tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj` -- added project refs and packages

**Deleted:**
- Template artifacts: TodoItem.cs, TodoList.cs, Colour.cs, PriorityLevel.cs, ColourTests.cs, and all TodoItem/TodoList/WeatherForecast commands/queries/endpoints/configurations/tests
- `api/src/Web/Middleware/TenantContextMiddleware.cs` -- was a no-op

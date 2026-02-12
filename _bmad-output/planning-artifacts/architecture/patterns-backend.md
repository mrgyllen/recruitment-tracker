# Implementation Patterns — Backend

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains aggregate boundaries, ubiquitous language, and enforcement guidelines. See also [patterns-frontend.md](./patterns-frontend.md) for frontend conventions._

_These patterns prevent AI agent implementation conflicts. All agents MUST follow these conventions._

## Naming Patterns

### C# / .NET

| Element | Convention | Example |
|---------|-----------|---------|
| Classes, methods, properties | PascalCase | `ImportCandidatesCommand`, `GetById()` |
| Private fields | `_camelCase` with underscore prefix | `_recruitmentRepository` |
| Local variables, parameters | camelCase | `candidateCount`, `importSession` |
| Interfaces | `I` prefix + PascalCase | `ITenantContext`, `ICandidateRepository` |
| Constants | PascalCase | `MaxImportFileSize` |
| Enums | PascalCase (singular) | `OutcomeStatus.Approved` |
| Async methods | `Async` suffix | `GetCandidatesAsync()` |

### Database (EF Core → Azure SQL)

| Element | Convention | Example |
|---------|-----------|---------|
| Tables | PascalCase plural | `Recruitments`, `Candidates`, `WorkflowSteps` |
| Columns | PascalCase (match C# property) | `FullName`, `DateApplied`, `RecruitmentId` |
| Foreign keys | `{Entity}Id` | `RecruitmentId`, `CandidateId` |
| Indexes | `IX_{Table}_{Columns}` | `IX_Candidates_RecruitmentId_Email` |
| Unique constraints | `UQ_{Table}_{Columns}` | `UQ_Candidates_RecruitmentId_Email` |

EF Core maps C# PascalCase properties directly — no snake_case translation layer.

### API (Minimal API Endpoints)

| Element | Convention | Example |
|---------|-----------|---------|
| URL paths | kebab-case, plural nouns | `/api/recruitments/{id}/candidates` |
| Route parameters | camelCase in `{braces}` | `{recruitmentId}`, `{candidateId}` |
| Query parameters | camelCase | `?stepId=abc&outcome=approved` |
| JSON response fields | camelCase | `{ "fullName": "...", "dateApplied": "..." }` |

ASP.NET Core's `System.Text.Json` uses camelCase by default.

## Structure Patterns

### Backend Project Organization (Clean Architecture)

```
api/src/
  Domain/
    Entities/              # Recruitment, Candidate, WorkflowStep, etc.
    ValueObjects/          # OutcomeResult, CandidateMatch, etc.
    Enums/                 # OutcomeStatus, ImportMatchConfidence, etc.
    Events/                # Domain events
    Exceptions/            # Domain-specific exceptions
  Application/
    Common/                # Shared interfaces, behaviors, mappings
      Interfaces/          # IRecruitmentRepository, ITenantContext, etc.
      Behaviours/          # Validation, logging pipeline behaviors
    Features/              # Organized by feature (CQRS)
      Recruitments/
        Commands/
          CreateRecruitment/
            CreateRecruitmentCommand.cs
            CreateRecruitmentCommandValidator.cs
            CreateRecruitmentCommandHandler.cs
        Queries/
          GetRecruitmentOverview/
            GetRecruitmentOverviewQuery.cs
            GetRecruitmentOverviewQueryHandler.cs
            RecruitmentOverviewDto.cs
      Candidates/
      Import/
      Screening/
  Infrastructure/
    Data/                  # DbContext, configurations, migrations
    Services/              # Blob storage, XLSX parser, PDF splitter
    Identity/              # Entra ID integration, ITenantContext impl
  Web/
    Endpoints/             # Minimal API endpoint definitions (by feature)
    Middleware/             # Auth, error handling, tenant context
    Configuration/         # DI registration, app config
```

**Rule: One command/query per folder.** Each command or query gets its own folder containing the request, validator, handler, and any DTOs. The `ca-usecase` scaffolding follows this pattern.

## DTO Mapping

**Rule: Manual mapping, no AutoMapper.** Use `ToDto()` extension methods or static `From()` factory methods on DTOs. Every field is visibly mapped — explicit beats magic for debuggability and solo dev maintenance.

```csharp
// Example: DTO with explicit mapping
public record RecruitmentOverviewDto
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public int TotalCandidates { get; init; }

    public static RecruitmentOverviewDto From(Recruitment entity, int candidateCount) =>
        new()
        {
            Id = entity.Id,
            Title = entity.Title,
            TotalCandidates = candidateCount
        };
}
```

## Error Handling

| Layer | Pattern |
|-------|---------|
| Domain | Throw domain-specific exceptions (`RecruitmentClosedException`, `DuplicateCandidateException`) |
| Application | FluentValidation catches input errors before handler executes. Handler catches domain exceptions and translates to results. |
| Web | Global exception middleware converts unhandled exceptions to Problem Details. No stack traces in production. |

**Rule: Domain never catches silently.** If a business rule is violated, throw. Let the application layer decide what to do.

## Test Conventions

### Test Project Organization

```
api/tests/
  Domain.UnitTests/        # Entity logic, value objects, domain rules
  Application.UnitTests/   # Command/query handlers (mocked infra)
  Application.FunctionalTests/  # API endpoints via WebApplicationFactory
  Infrastructure.IntegrationTests/  # EF Core, Blob Storage, real database
```

**Rule: Test files mirror source structure.** `Application.UnitTests/Features/Recruitments/Commands/CreateRecruitmentCommandTests.cs` matches the source path.

**Rule: NSubstitute for backend mocking.** The Jason Taylor template uses NSubstitute. All Application.UnitTests use NSubstitute for mocking interfaces (`ITenantContext`, `IApplicationDbContext`, `IBlobStorageService`, etc.). No Moq, no other mocking library. Example: `var tenantContext = Substitute.For<ITenantContext>();`

## MediatR Domain Events

| Element | Convention | Example |
|---------|-----------|---------|
| Event name | PascalCase past tense | `CandidateImportedEvent`, `OutcomeRecordedEvent` |
| Event class | Implements `INotification` | Lives in `Domain/Events/` |
| Handler | `{EventName}Handler` | In `Application/Features/` near related feature |

## Audit Events

```csharp
public record AuditEvent(
    Guid RecruitmentId,
    Guid? EntityId,
    string EntityType,       // "Candidate", "Recruitment", "Document"
    string ActionType,       // "Created", "Updated", "Deleted", "Accessed"
    Guid PerformedBy,
    DateTimeOffset PerformedAt,
    JsonDocument? Context    // No PII — IDs and metadata only
);
```

**Rule: No PII in audit event `Context`.** Use entity IDs, not names or emails. The audit trail references entities; it doesn't duplicate their data.

## Verification Checkpoints

_Scannable checklist for review agents. Derived from architecture docs — check each item during code review._

### DDD / Aggregate Rules

- [ ] Domain entity properties use private setters (`{ get; private set; }` or `init`)
- [ ] Child entities modified only through aggregate root methods (e.g., `recruitment.AddStep()`)
- [ ] No direct `dbContext.WorkflowSteps.Add()` or similar bypasses
- [ ] Cross-aggregate references use IDs only — no navigation properties between aggregates
- [ ] One aggregate per transaction — no multi-aggregate saves in a single handler
- [ ] Domain events raised for significant state changes
- [ ] Domain exceptions thrown for business rule violations (never caught silently)

### EF Core

- [ ] Fluent API only — no `[Required]`, `[MaxLength]`, or other DataAnnotations on domain entities
- [ ] Global query filters configured for tenant isolation
- [ ] Index naming: `IX_{Table}_{Columns}`
- [ ] Unique constraint naming: `UQ_{Table}_{Columns}`
- [ ] Table naming: PascalCase plural
- [ ] No `UseInMemoryDatabase` — use Testcontainers for integration tests

### Security / Data Isolation

- [ ] All data queries go through `ITenantContext` — no unscoped queries
- [ ] 8 mandatory security test scenarios present (see `testing-standards.md`)
- [ ] No PII in audit events or logs
- [ ] SAS tokens used for document access (no direct Blob URLs)
- [ ] FluentValidation on all command/query inputs
- [ ] Problem Details (RFC 9457) for all error responses

### Template Cleanup

- [ ] Angular `ClientApp` directory removed (if from Jason Taylor template)
- [ ] No `using Moq;` — use NSubstitute exclusively
- [ ] No `[Fact]` or `[Theory]` — use NUnit's `[Test]` and `[TestCase]`
- [ ] No `UseInMemoryDatabase` — use Testcontainers with real SQL Server

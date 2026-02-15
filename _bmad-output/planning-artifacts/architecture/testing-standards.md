# Testing Standards

_Shard of the [Architecture Decision Document](../architecture.md). Load when implementing any story that includes tests._

_See also: [`docs/testing-pragmatic-tdd.md`](../../../docs/testing-pragmatic-tdd.md) for the project's Pragmatic TDD philosophy (test-first, spike, characterization modes)._

## Test Frameworks

### Backend (.NET)

| Concern | Tool | Notes |
|---------|------|-------|
| Test framework | **NUnit** | `[Test]`, `[TestCase]`, `[SetUp]`, `[TearDown]` |
| Mocking | **NSubstitute** | `Substitute.For<T>()` — no Moq, no other mocking library |
| Assertions | **FluentAssertions** | `.Should().Be()`, `.Should().Throw<T>()` |
| Integration DB | **Testcontainers** | Real SQL Server in Docker — see Integration Tests below |
| API testing | **WebApplicationFactory** | In-process HTTP testing for functional tests |

**FORBIDDEN:**
- `UseInMemoryDatabase` — silently skips FK constraints, query filters, and SQL-specific behavior. Every security and integration test that passes with InMemory but fails against real SQL Server is a bug you'll find in production.
- `Moq` — project standardized on NSubstitute. Do not add Moq packages or `using Moq;`.
- `[Fact]` / `[Theory]` — these are xUnit attributes. Use NUnit's `[Test]` / `[TestCase]`.

### Frontend (TypeScript/React)

| Concern | Tool | Notes |
|---------|------|-------|
| Test runner | **Vitest** | `describe`, `it`, `expect` |
| Component testing | **Testing Library** (`@testing-library/react`) | `render`, `screen`, `userEvent` |
| API mocking | **MSW** (Mock Service Worker) | Intercepts fetch at network level |
| Test utilities | Custom `test-utils.tsx` | Wraps `render` with providers (router, query client, auth) |

## Test Naming Convention

### Backend

```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `AddStep_DuplicateName_ThrowsDuplicateStepException`
- `RecordOutcome_InvalidStep_ThrowsInvalidOperationException`
- `GetCandidates_UserNotInRecruitment_ReturnsEmptyList`
- `ImportCandidates_ValidXlsx_CreatesAllCandidates`

### Frontend

```
"should [expected behavior] when [scenario]"
```

Examples:
- `"should display empty state when no recruitments exist"`
- `"should disable submit button when form is invalid"`
- `"should call API with correct parameters when filtering"`

## Test Project Organization

```
api/tests/
  Domain.UnitTests/              # Entity logic, value objects, domain rules
  Application.UnitTests/         # Command/query handlers (mocked infra)
  Application.FunctionalTests/   # API endpoints via WebApplicationFactory
  Infrastructure.IntegrationTests/  # EF Core, Blob Storage, real database
```

**Rule: Test files mirror source structure.**
`Application.UnitTests/Features/Recruitments/Commands/CreateRecruitmentCommandTests.cs` matches `Application/Features/Recruitments/Commands/CreateRecruitment/`.

## Integration Tests with Testcontainers

Security and data isolation tests MUST run against a real SQL Server instance via Testcontainers. This ensures EF Core global query filters, FK constraints, and SQL-specific behavior are exercised.

```csharp
// Example: Base class for integration tests
public class IntegrationTestBase : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected string ConnectionString => _sqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        // Apply migrations, seed test data
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }
}
```

### Mandatory Security Test Scenarios

These 8 scenarios MUST exist as integration tests before any feature tests. They validate the `ITenantContext` + global query filter security model:

1. **Cross-recruitment isolation:** User in Recruitment A cannot see candidates from Recruitment B
2. **Positive access:** User in Recruitment A CAN see candidates from Recruitment A
3. **Multi-membership:** User in both Recruitment A and B sees correct candidates for each
4. **Import service bypass:** Import service with `RecruitmentId` set can write candidates to that recruitment
5. **Import service scoping:** Import service cannot access candidates outside its scoped recruitment
6. **GDPR service bypass:** GDPR job with `IsServiceContext = true` can query across all recruitments
7. **Misconfigured context:** No user ID, no service flag returns zero results (not an error)
8. **Filter applies to all queries:** Global query filter works on direct queries, includes, and projections

## Code Coverage Enforcement

**Status:** Coverage tooling installed (coverlet.collector in all test .csproj files) but not yet generating reports. This section documents the target configuration.

### Minimum Thresholds

| Project | Line Coverage | Enforcement |
|---------|-------------|-------------|
| Domain.UnitTests | 70% | Fail CI if below threshold |
| Application.UnitTests | 60% | Fail CI if below threshold |
| Infrastructure/Web | No threshold | Integration tests suffice |
| Frontend (Vitest) | 60% | Warn only (monitor for 1 epic before enforcing) |

### Backend Coverage

```bash
# Generate coverage reports
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage/report -reporttypes:Html
```

coverlet.collector is already in test .csproj files. Add `--collect:"XPlat Code Coverage"` to the test command in CI.

### Frontend Coverage

```bash
# Add coverage reporter (if not installed)
npm install -D @vitest/coverage-v8

# Run with coverage
npx vitest run --coverage
```

Configure in `vitest.config.ts`:
```typescript
export default defineConfig({
  test: {
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'cobertura'],
      thresholds: { lines: 60 },
    },
  },
});
```

### CI Integration

CI workflow should:
1. Run tests with coverage collection
2. Generate coverage report (HTML + Cobertura)
3. Publish report as CI artifact
4. Fail if thresholds not met (after 1 epic of monitoring-only)

## Pragmatic TDD Modes

Before coding any task, declare which mode applies:

| Mode | When | Rule |
|------|------|------|
| **Test-first** | Domain logic, business rules, aggregate invariants, security isolation | Write test → see it fail → implement → pass |
| **Spike** | High uncertainty (new library, unclear API shape) | Explore first, but tests MUST be added before merge |
| **Characterization** | Existing code without tests, template-generated code | Write tests that document current behavior before modifying |

Default to test-first for domain/business logic. Use spikes only when uncertainty is high.

## Test Pyramid Layers

_Added by [ADR-001](./adrs/ADR-001-test-pyramid-e2e-decomposition.md). Defines the five test layers, when each is required, and the decision tree for layer selection._

### Layer Definitions

| Layer | Project | Speed | What It Proves | When Required |
|-------|---------|-------|----------------|---------------|
| **Unit** | `Domain.UnitTests`, `Application.UnitTests` | < 1s | Business logic, validation, domain rules in isolation | Every handler, entity, and value object |
| **Contract** | `Application.UnitTests/Contracts/` | < 1s | Frontend DTO ↔ Backend DTO structural alignment | Every endpoint with a corresponding MSW handler |
| **Integration** | `Infrastructure.IntegrationTests` | 2-5s | EF Core mappings, query filters, tenant isolation against real SQL | Security isolation scenarios (see Mandatory Security Test Scenarios above), complex EF Core configurations |
| **Functional** | `Application.FunctionalTests` | 3-10s | Full HTTP pipeline (routing → auth → MediatR → EF Core → SQL → response) via `WebApplicationFactory` + Testcontainers | Handlers using LINQ-to-SQL features (see coverage rule below), API contract verification |
| **E2E** | `Web.AcceptanceTests` | 10-30s | Browser-based user journeys via SpecFlow + Playwright | Only when no combination of lower tests covers the risk (see E2E Decomposition below) |

### Layer Selection Decision Tree

Walk this tree top-to-bottom for each handler or component. Select the **highest layer that applies**:

1. **Does the handler use `.Include()`, `.Where()` with navigation properties, `.Select()` projections, `.GroupBy()`, or complex LINQ expressions?**
   - YES → **Functional test required** (in addition to unit test)
   - NO → Continue

2. **Does the handler enforce tenant isolation or authorization by loading aggregate members?**
   - YES → **Integration test required** (covered by mandatory security scenarios)
   - NO → Continue

3. **Does the endpoint have a corresponding MSW handler in the frontend?**
   - YES → **Contract test required** (in addition to unit test)
   - NO → Continue

4. **Default → Unit test sufficient**

### Handler Functional Test Coverage Rule

Any query or command handler using EF Core LINQ features that cannot be fully validated with in-memory mocks **MUST** have at least one functional test running through `WebApplicationFactory` + Testcontainers against real SQL Server.

**Trigger:** The handler's LINQ expression uses any of:
- `.Include()` / `.ThenInclude()` — navigation property loading
- `.Where()` referencing navigation properties — cross-entity filtering
- `.Select()` with property projections — DTO mapping in SQL
- `.GroupBy()` — SQL aggregation
- Raw SQL or `FromSqlRaw()` — direct SQL execution

**Rationale:** NSubstitute mocks evaluate LINQ expressions against in-memory `List<T>` collections. This silently passes expressions that EF Core's LINQ-to-SQL translator cannot translate, producing runtime failures only discoverable against a real database. The `GetRecruitmentOverviewQueryHandler` bug (2026-02-15) is the canonical example.

**Functional test pattern:**
```csharp
// In Application.FunctionalTests/Features/{Feature}/{HandlerName}Tests.cs
[Test]
public async Task HandlerName_Scenario_ExpectedBehavior()
{
    // Arrange: seed data via Testing.cs helpers
    await Testing.AddAsync(new Recruitment { ... });

    // Act: send HTTP request through WebApplicationFactory
    var response = await Testing.SendAsync(new GetSomethingQuery { Id = id });

    // Assert: verify response shape and data
    response.Should().NotBeNull();
    response.Items.Should().HaveCount(expectedCount);
}
```

## E2E Decomposition Method

_Added by [ADR-001](./adrs/ADR-001-test-pyramid-e2e-decomposition.md). Defines how E2E scenarios are documented, decomposed to lower tests, and when automated E2E tests are warranted._

### Methodology

1. **Define E2E scenarios** from PRD user journeys — documented in [`docs/e2e-scenarios.md`](../../../docs/e2e-scenarios.md)
2. **Decompose each scenario** into lower-level tests (unit, contract, integration, functional) that collectively cover the risk
3. **Mark coverage gaps** — scenarios where no combination of lower tests suffices
4. **Automate E2E only** when a gap meets all four criteria below

### Scenario Registry Format

Each user journey has a table in `docs/e2e-scenarios.md`:

```markdown
### J0: First Five Minutes (Onboarding)

| Scenario | Risk | Covered By | Gap? |
|----------|------|-----------|------|
| SSO login redirects correctly | Auth bypass | Functional: auth middleware tests | No |
| Empty state shows create prompt | UI regression | Frontend: component tests | No |
| XLSX import creates candidates | Data integrity | Functional: import handler tests | No |
```

### When to Add a Real E2E Test

Add an automated browser-based E2E test (SpecFlow + Playwright) only when ALL four criteria are met:

1. **Cross-boundary risk:** The scenario crosses 3+ system boundaries (browser → API → database → blob storage)
2. **Integration-sensitive:** The risk is in the integration between systems, not in any single system's logic
3. **Lower tests insufficient:** No combination of unit + integration + functional tests can cover the specific failure mode
4. **High business impact:** Failure in production would directly impact a core user journey (J0-J3)

### Agent Workflow for E2E Decomposition

During story implementation, when the Test Layer Map (see team-workflow.md) identifies a scenario from `docs/e2e-scenarios.md`:

1. Check the scenario's "Covered By" column
2. If "Gap? = No" — verify the referenced tests still exist and pass
3. If "Gap? = Partial" — add the missing lower-level tests to close the gap
4. If "Gap? = Yes" — evaluate the four criteria above; add E2E test only if all met
5. Update the registry after adding tests

## Contract Tests

_Added by [ADR-001](./adrs/ADR-001-test-pyramid-e2e-decomposition.md). Structural DTO verification to catch frontend-backend drift._

### Purpose

Contract tests verify that backend response DTOs have the expected property names and types that the frontend depends on. They catch breaking API changes at compile/test time rather than at runtime.

### Location

`api/tests/Application.UnitTests/Contracts/`

### Approach: Reflection-Based Property Checks

```csharp
// Application.UnitTests/Contracts/RecruitmentDtoContractTests.cs
[TestFixture]
public class RecruitmentDtoContractTests
{
    [Test]
    public void RecruitmentOverviewDto_HasExpectedProperties()
    {
        var type = typeof(RecruitmentOverviewDto);

        type.Should().HaveProperty<Guid>("Id");
        type.Should().HaveProperty<string>("Title");
        type.Should().HaveProperty<string>("Status");
        type.Should().HaveProperty<int>("TotalCandidates");
        type.Should().HaveProperty<int>("ActiveMembers");
    }
}
```

### When Required

A contract test is required for every backend endpoint that has a corresponding MSW handler in the frontend's `src/test/handlers/` directory. This ensures the mock responses in tests match the actual API shape.

### Maintenance

When a DTO property is added, renamed, or removed:
1. Update the contract test first (test-first for contract changes)
2. Update the DTO
3. Update the MSW handler to match

## Post-Deployment Smoke Tests

_Added by [ADR-001](./adrs/ADR-001-test-pyramid-e2e-decomposition.md). Lightweight verification after each CD deployment._

### Checks

Three curl-based checks run in the CD pipeline after deployment:

| Check | Endpoint | Expected | Verifies |
|-------|----------|----------|----------|
| Liveness | `/health` | 200 | Process is alive |
| Readiness | `/ready` | 200 | Database connected, dependencies available |
| Auth enforcement | `/api/recruitments` (unauthenticated) | 401 | Authentication middleware active, no accidental anonymous access |

### Implementation

The liveness and readiness checks already exist in `cd.yml`. The auth enforcement check is added as an additional step (see `.github/workflows/cd.yml`).

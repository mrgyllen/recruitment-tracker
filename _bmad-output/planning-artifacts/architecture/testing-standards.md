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

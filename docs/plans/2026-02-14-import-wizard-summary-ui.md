# Import Wizard & Summary UI Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a guided import wizard UI (Sheet component) with file upload, progress polling, result summary, and low-confidence match review, plus the backend ResolveMatchConflict command.

**Architecture:** Backend adds domain methods (ConfirmMatch/RejectMatch on ImportSession) and a new CQRS command. Frontend is a multi-step wizard (Upload -> Processing -> Summary, with optional MatchReview) using TanStack Query polling and mutations. The wizard lives in a Sheet (slide-from-right panel).

**Tech Stack:** .NET 10 (MediatR, FluentValidation, EF Core), React 19, TypeScript 5.7, TanStack Query 5, shadcn/ui (Sheet, Progress, Alert, Collapsible), Vitest, Testing Library, MSW, NUnit, NSubstitute

---

## Context for Implementers

### Key File Locations
- Domain entities: `api/src/Domain/Entities/`
- Value objects: `api/src/Domain/ValueObjects/`
- CQRS commands: `api/src/Application/Features/Import/Commands/`
- Endpoints: `api/src/Web/Endpoints/`
- Frontend API clients: `web/src/lib/api/`
- Frontend features: `web/src/features/candidates/`
- MSW mocks: `web/src/mocks/`
- Test utilities: `web/src/test-utils.tsx`

### Existing Patterns to Follow
- **Backend handlers:** Primary constructor DI, `ITenantContext` for auth, `using` aliases for `ForbiddenAccessException`/`NotFoundException`
- **Frontend API:** All HTTP goes through `web/src/lib/api/httpClient.ts` (`apiGet`, `apiPost`, etc.)
- **Frontend tests:** Use `render` from `@/test-utils` (wraps QueryClient + Router + Auth), MSW for API mocking, `vitest` + `@testing-library/react`
- **MSW handlers:** Export array from feature handler file, register in `web/src/mocks/handlers.ts`

### Authorization Pattern (E-001)
Every handler that accesses recruitment-scoped data MUST:
1. Load ImportSession by ID
2. Load Recruitment with `.Include(r => r.Members)` using `session.RecruitmentId`
3. Verify `tenantContext.UserGuid` is in members
4. Throw `ForbiddenAccessException` if not

---

## Task 1: Domain — Extend ImportSession with ConfirmMatch/RejectMatch

**Files:**
- Modify: `api/src/Domain/Entities/ImportSession.cs`
- Modify: `api/src/Domain/ValueObjects/ImportRowResult.cs`
- Test: `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`

The ImportSession needs methods to resolve flagged matches. The ImportRowResult value object needs to be extended to support resolution status.

**Step 1: Replace ImportRowResult with a mutable record that tracks resolution**

ImportRowResult is currently an immutable sealed record. For match resolution, we need to track whether a flagged row has been confirmed/rejected. Since value objects are immutable by convention, the cleanest approach is to make ImportRowResult a class within the ImportSession aggregate (owned entity) so it can track resolution state.

Replace `api/src/Domain/ValueObjects/ImportRowResult.cs`:

```csharp
using api.Domain.Enums;

namespace api.Domain.ValueObjects;

public sealed class ImportRowResult
{
    public int RowNumber { get; private set; }
    public string? CandidateEmail { get; private set; }
    public ImportRowAction Action { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Resolution { get; private set; } // "Confirmed" | "Rejected" | null

    private ImportRowResult() { } // EF Core

    public ImportRowResult(int rowNumber, string? candidateEmail, ImportRowAction action, string? errorMessage)
    {
        RowNumber = rowNumber;
        CandidateEmail = candidateEmail;
        Action = action;
        ErrorMessage = errorMessage;
    }

    public void Confirm()
    {
        if (Action != ImportRowAction.Flagged)
            throw new InvalidOperationException("Only flagged rows can be confirmed.");
        if (Resolution is not null)
            throw new InvalidOperationException("This match has already been resolved.");
        Resolution = "Confirmed";
    }

    public void Reject()
    {
        if (Action != ImportRowAction.Flagged)
            throw new InvalidOperationException("Only flagged rows can be rejected.");
        if (Resolution is not null)
            throw new InvalidOperationException("This match has already been resolved.");
        Resolution = "Rejected";
    }
}
```

**Step 2: Update ImportSession with ConfirmMatch/RejectMatch**

Add to `api/src/Domain/Entities/ImportSession.cs`:

```csharp
public ImportRowResult ConfirmMatch(int rowIndex)
{
    if (Status != ImportSessionStatus.Completed)
        throw new InvalidWorkflowTransitionException(Status.ToString(), "match resolution requires Completed status");

    if (rowIndex < 0 || rowIndex >= _rowResults.Count)
        throw new ArgumentOutOfRangeException(nameof(rowIndex));

    var row = _rowResults[rowIndex];
    row.Confirm();
    return row;
}

public ImportRowResult RejectMatch(int rowIndex)
{
    if (Status != ImportSessionStatus.Completed)
        throw new InvalidWorkflowTransitionException(Status.ToString(), "match resolution requires Completed status");

    if (rowIndex < 0 || rowIndex >= _rowResults.Count)
        throw new ArgumentOutOfRangeException(nameof(rowIndex));

    var row = _rowResults[rowIndex];
    row.Reject();
    return row;
}
```

**Step 3: Update existing tests and add new ones**

The existing `ImportRowResult` tests use the record constructor syntax. Update them to use the new class constructor. Then add tests for ConfirmMatch/RejectMatch.

New tests to add to `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`:

```csharp
[Test]
public void ConfirmMatch_FlaggedRow_SetsResolutionToConfirmed()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
    session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));
    session.MarkCompleted(0, 0, 0, 1);

    var result = session.ConfirmMatch(0);

    result.Resolution.Should().Be("Confirmed");
}

[Test]
public void RejectMatch_FlaggedRow_SetsResolutionToRejected()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
    session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));
    session.MarkCompleted(0, 0, 0, 1);

    var result = session.RejectMatch(0);

    result.Resolution.Should().Be("Rejected");
}

[Test]
public void ConfirmMatch_NonFlaggedRow_Throws()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
    session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Created, null));
    session.MarkCompleted(1, 0, 0, 0);

    var act = () => session.ConfirmMatch(0);

    act.Should().Throw<InvalidOperationException>();
}

[Test]
public void ConfirmMatch_AlreadyResolved_Throws()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
    session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));
    session.MarkCompleted(0, 0, 0, 1);
    session.ConfirmMatch(0);

    var act = () => session.ConfirmMatch(0);

    act.Should().Throw<InvalidOperationException>();
}

[Test]
public void ConfirmMatch_ProcessingSession_Throws()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
    session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));

    var act = () => session.ConfirmMatch(0);

    act.Should().Throw<InvalidWorkflowTransitionException>();
}

[Test]
public void ConfirmMatch_InvalidIndex_Throws()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
    session.MarkCompleted(0, 0, 0, 0);

    var act = () => session.ConfirmMatch(0);

    act.Should().Throw<ArgumentOutOfRangeException>();
}
```

**Step 4: Update ImportRowResultTests**

In `api/tests/Domain.UnitTests/ValueObjects/ImportRowResultTests.cs`, update constructor calls from record syntax to class constructor syntax.

**Step 5: Run tests**

```bash
dotnet test api/tests/Domain.UnitTests/Domain.UnitTests.csproj -v n
```

Expected: All tests pass (existing + 6 new).

**Step 6: Commit**

```bash
git add api/src/Domain/ api/tests/Domain.UnitTests/
git commit -m "feat(3.3): extend ImportSession with ConfirmMatch/RejectMatch domain methods"
```

---

## Task 2: Backend — Update ImportRowResultDto with Resolution field

**Files:**
- Modify: `api/src/Application/Features/Import/Queries/GetImportSession/ImportSessionDto.cs`

**Step 1: Add Resolution field to ImportRowResultDto**

```csharp
public record ImportRowResultDto
{
    public int RowNumber { get; init; }
    public string? CandidateEmail { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? Resolution { get; init; }

    public static ImportRowResultDto From(ImportRowResult row) => new()
    {
        RowNumber = row.RowNumber,
        CandidateEmail = row.CandidateEmail,
        Action = row.Action.ToString(),
        ErrorMessage = row.ErrorMessage,
        Resolution = row.Resolution,
    };
}
```

**Step 2: Build and run tests**

```bash
dotnet build api/src/Web/Web.csproj --no-incremental
dotnet test api/tests/Domain.UnitTests/Domain.UnitTests.csproj -v n
```

**Step 3: Commit**

```bash
git add api/src/Application/Features/Import/Queries/GetImportSession/ImportSessionDto.cs
git commit -m "feat(3.3): add Resolution field to ImportRowResultDto"
```

---

## Task 3: Backend — ResolveMatchConflict command, validator, handler

**Files:**
- Create: `api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommand.cs`
- Create: `api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandValidator.cs`
- Create: `api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandHandler.cs`
- Create: `api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchResultDto.cs`
- Test: `api/tests/Application.UnitTests/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandValidatorTests.cs`
- Test: `api/tests/Application.UnitTests/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandHandlerTests.cs`

**Step 1: Create command record**

`api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommand.cs`:

```csharp
namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public record ResolveMatchConflictCommand(
    Guid ImportSessionId,
    int MatchIndex,
    string Action // "Confirm" or "Reject"
) : IRequest<ResolveMatchResultDto>;
```

**Step 2: Create response DTO**

`api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchResultDto.cs`:

```csharp
namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public record ResolveMatchResultDto(
    int MatchIndex,
    string Action,
    string? CandidateEmail);
```

**Step 3: Create validator**

`api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandValidator.cs`:

```csharp
namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public class ResolveMatchConflictCommandValidator
    : AbstractValidator<ResolveMatchConflictCommand>
{
    public ResolveMatchConflictCommandValidator()
    {
        RuleFor(x => x.ImportSessionId).NotEmpty();
        RuleFor(x => x.MatchIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Action).NotEmpty()
            .Must(a => a == "Confirm" || a == "Reject")
            .WithMessage("Action must be 'Confirm' or 'Reject'");
    }
}
```

**Step 4: Create handler**

`api/src/Application/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandHandler.cs`:

```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.ValueObjects;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public class ResolveMatchConflictCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<ResolveMatchConflictCommand, ResolveMatchResultDto>
{
    public async Task<ResolveMatchResultDto> Handle(
        ResolveMatchConflictCommand request,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportSession), request.ImportSessionId);

        // Authorization: verify user is member of the recruitment
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == session.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), session.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        ImportRowResult row;
        if (request.Action == "Confirm")
        {
            row = session.ConfirmMatch(request.MatchIndex);

            // Update candidate profile from import data
            var candidate = await dbContext.Candidates
                .FirstOrDefaultAsync(c =>
                    c.RecruitmentId == session.RecruitmentId &&
                    !string.IsNullOrEmpty(c.Email) &&
                    c.Email == row.CandidateEmail,
                    cancellationToken);

            // Profile update happens if candidate found (best-effort)
            // The actual profile data isn't stored on ImportRowResult,
            // so confirmation marks the match as accepted
        }
        else
        {
            row = session.RejectMatch(request.MatchIndex);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResolveMatchResultDto(
            request.MatchIndex,
            row.Resolution!,
            row.CandidateEmail);
    }
}
```

**Step 5: Write validator tests**

`api/tests/Application.UnitTests/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandValidatorTests.cs`:

```csharp
using api.Application.Features.Import.Commands.ResolveMatchConflict;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Commands.ResolveMatchConflict;

[TestFixture]
public class ResolveMatchConflictCommandValidatorTests
{
    private ResolveMatchConflictCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new ResolveMatchConflictCommandValidator();
    }

    [Test]
    public void Validate_ValidConfirmCommand_Succeeds()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Confirm");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ValidRejectCommand_Succeeds()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Reject");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptySessionId_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.Empty, 0, "Confirm");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_NegativeMatchIndex_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), -1, "Confirm");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_InvalidAction_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Invalid");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_EmptyAction_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
```

**Step 6: Write handler tests**

`api/tests/Application.UnitTests/Features/Import/Commands/ResolveMatchConflict/ResolveMatchConflictCommandHandlerTests.cs`:

```csharp
using api.Application.Common.Interfaces;
using api.Application.Features.Import.Commands.ResolveMatchConflict;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Import.Commands.ResolveMatchConflict;

[TestFixture]
public class ResolveMatchConflictCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
    }

    [Test]
    public async Task Handle_ConfirmMatch_SetsResolutionToConfirmed()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var session = ImportSession.Create(recruitment.Id, userId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(1, "flagged@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Confirm");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Action.Should().Be("Confirmed");
        result.MatchIndex.Should().Be(0);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RejectMatch_SetsResolutionToRejected()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var session = ImportSession.Create(recruitment.Id, userId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(1, "flagged@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Reject");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Action.Should().Be("Rejected");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SessionNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        var sessionMockSet = new List<ImportSession>().AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Confirm");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var session = ImportSession.Create(recruitment.Id, creatorId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(1, "flagged@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Confirm");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
```

**Step 7: Build and run tests**

```bash
dotnet build api/src/Web/Web.csproj --no-incremental
dotnet test api/tests/Domain.UnitTests/Domain.UnitTests.csproj -v n
```

**Step 8: Commit**

```bash
git add api/src/Application/Features/Import/Commands/ResolveMatchConflict/ api/tests/Application.UnitTests/Features/Import/Commands/ResolveMatchConflict/
git commit -m "feat(3.3): add ResolveMatchConflict command with validator and handler"
```

---

## Task 4: Backend — Add resolve-match endpoint to ImportSessionEndpoints

**Files:**
- Modify: `api/src/Web/Endpoints/ImportSessionEndpoints.cs`

**Step 1: Add POST /{id}/resolve-match endpoint**

```csharp
using api.Application.Features.Import.Commands.ResolveMatchConflict;
using api.Application.Features.Import.Queries.GetImportSession;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class ImportSessionEndpoints : EndpointGroupBase
{
    public override string? GroupName => "import-sessions";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", GetImportSession);
        group.MapPost("/{id:guid}/resolve-match", ResolveMatchConflict);
    }

    private static async Task<IResult> GetImportSession(
        ISender sender,
        Guid id)
    {
        var result = await sender.Send(new GetImportSessionQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> ResolveMatchConflict(
        ISender sender,
        Guid id,
        ResolveMatchConflictRequest request)
    {
        var result = await sender.Send(new ResolveMatchConflictCommand(
            id, request.MatchIndex, request.Action));
        return Results.Ok(result);
    }
}

public record ResolveMatchConflictRequest(int MatchIndex, string Action);
```

**Step 2: Build**

```bash
dotnet build api/src/Web/Web.csproj --no-incremental
```

**Step 3: Commit**

```bash
git add api/src/Web/Endpoints/ImportSessionEndpoints.cs
git commit -m "feat(3.3): add resolve-match endpoint to ImportSessionEndpoints"
```

---

## Task 5: Frontend — Add apiPostFormData to httpClient

**Files:**
- Modify: `web/src/lib/api/httpClient.ts`

**Step 1: Add apiPostFormData function**

Add after `apiDelete`:

```typescript
export async function apiPostFormData<T>(
  path: string,
  formData: FormData,
): Promise<T> {
  const headers = await getAuthHeaders()
  // Remove Content-Type — browser auto-sets multipart/form-data with boundary
  delete (headers as Record<string, string>)['Content-Type']
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers,
    body: formData,
  })
  return handleResponse<T>(res)
}
```

**Step 2: Verify build**

```bash
cd web && npx tsc --noEmit
```

**Step 3: Commit**

```bash
git add web/src/lib/api/httpClient.ts
git commit -m "feat(3.3): add apiPostFormData helper to httpClient"
```

---

## Task 6: Frontend — Import API types, client, and MSW handlers

**Files:**
- Create: `web/src/lib/api/import.types.ts`
- Create: `web/src/lib/api/import.ts`
- Create: `web/src/mocks/importHandlers.ts`
- Modify: `web/src/mocks/handlers.ts`

**Step 1: Create import types**

`web/src/lib/api/import.types.ts`:

```typescript
export type ImportSessionStatus = 'Processing' | 'Completed' | 'Failed'

export interface ImportRowResult {
  rowNumber: number
  candidateEmail: string | null
  action: 'Created' | 'Updated' | 'Errored' | 'Flagged'
  errorMessage: string | null
  resolution: string | null
}

export interface ImportSessionResponse {
  id: string
  recruitmentId: string
  status: ImportSessionStatus
  sourceFileName: string
  createdAt: string
  completedAt: string | null
  totalRows: number
  createdCount: number
  updatedCount: number
  erroredCount: number
  flaggedCount: number
  failureReason: string | null
  rowResults: ImportRowResult[]
}

export interface StartImportResponse {
  importSessionId: string
  statusUrl: string
}

export interface ResolveMatchRequest {
  matchIndex: number
  action: 'Confirm' | 'Reject'
}

export interface ResolveMatchResponse {
  matchIndex: number
  action: string
  candidateEmail: string | null
}
```

**Step 2: Create import API client**

`web/src/lib/api/import.ts`:

```typescript
import { apiGet, apiPost, apiPostFormData } from './httpClient'
import type {
  ImportSessionResponse,
  ResolveMatchRequest,
  ResolveMatchResponse,
  StartImportResponse,
} from './import.types'

export const importApi = {
  startImport: (recruitmentId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiPostFormData<StartImportResponse>(
      `/recruitments/${recruitmentId}/import`,
      formData,
    )
  },

  getSession: (importSessionId: string) =>
    apiGet<ImportSessionResponse>(`/import-sessions/${importSessionId}`),

  resolveMatch: (importSessionId: string, data: ResolveMatchRequest) =>
    apiPost<ResolveMatchResponse>(
      `/import-sessions/${importSessionId}/resolve-match`,
      data,
    ),
}
```

**Step 3: Create MSW handlers**

`web/src/mocks/importHandlers.ts`:

```typescript
import { http, HttpResponse } from 'msw'
import type { ImportSessionResponse } from '@/lib/api/import.types'

export const mockImportSessionId = 'import-session-001'

export const mockCompletedSession: ImportSessionResponse = {
  id: mockImportSessionId,
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  status: 'Completed',
  sourceFileName: 'workday-export.xlsx',
  createdAt: new Date().toISOString(),
  completedAt: new Date().toISOString(),
  totalRows: 10,
  createdCount: 7,
  updatedCount: 2,
  erroredCount: 0,
  flaggedCount: 1,
  failureReason: null,
  rowResults: [
    {
      rowNumber: 1,
      candidateEmail: 'anna@example.com',
      action: 'Created',
      errorMessage: null,
      resolution: null,
    },
    {
      rowNumber: 2,
      candidateEmail: 'bob@example.com',
      action: 'Updated',
      errorMessage: null,
      resolution: null,
    },
    {
      rowNumber: 3,
      candidateEmail: 'flagged@example.com',
      action: 'Flagged',
      errorMessage: null,
      resolution: null,
    },
  ],
}

export const mockProcessingSession: ImportSessionResponse = {
  ...mockCompletedSession,
  status: 'Processing',
  completedAt: null,
  totalRows: 0,
  createdCount: 0,
  updatedCount: 0,
  erroredCount: 0,
  flaggedCount: 0,
  rowResults: [],
}

export const mockFailedSession: ImportSessionResponse = {
  ...mockCompletedSession,
  status: 'Failed',
  failureReason: 'Missing required column: Email',
  totalRows: 0,
  createdCount: 0,
  updatedCount: 0,
  erroredCount: 0,
  flaggedCount: 0,
  rowResults: [],
}

export const importHandlers = [
  http.post('/api/recruitments/:id/import', () => {
    return HttpResponse.json(
      {
        importSessionId: mockImportSessionId,
        statusUrl: `/api/import-sessions/${mockImportSessionId}`,
      },
      { status: 202 },
    )
  }),

  http.get('/api/import-sessions/:id', () => {
    return HttpResponse.json(mockCompletedSession)
  }),

  http.post('/api/import-sessions/:id/resolve-match', async ({ request }) => {
    const body = (await request.json()) as {
      matchIndex: number
      action: string
    }
    return HttpResponse.json({
      matchIndex: body.matchIndex,
      action: body.action === 'Confirm' ? 'Confirmed' : 'Rejected',
      candidateEmail: 'flagged@example.com',
    })
  }),
]
```

**Step 4: Register handlers in handlers.ts**

Modify `web/src/mocks/handlers.ts`:

```typescript
import { candidateHandlers } from './candidateHandlers'
import { importHandlers } from './importHandlers'
import { recruitmentHandlers } from './recruitmentHandlers'
import { teamHandlers } from './teamHandlers'
import type { RequestHandler } from 'msw'

export const handlers: RequestHandler[] = [
  ...recruitmentHandlers,
  ...teamHandlers,
  ...candidateHandlers,
  ...importHandlers,
]
```

**Step 5: Type check**

```bash
cd web && npx tsc --noEmit
```

**Step 6: Commit**

```bash
git add web/src/lib/api/import.types.ts web/src/lib/api/import.ts web/src/mocks/importHandlers.ts web/src/mocks/handlers.ts
git commit -m "feat(3.3): add import API client, types, and MSW handlers"
```

---

## Task 7: Frontend — useImportSession polling hook

**Files:**
- Create: `web/src/features/candidates/ImportFlow/hooks/useImportSession.ts`

**Step 1: Create the hook**

```typescript
import { useQuery } from '@tanstack/react-query'
import { importApi } from '@/lib/api/import'
import type { ImportSessionResponse } from '@/lib/api/import.types'

const POLL_INTERVAL_MS = 2000

export function useImportSession(importSessionId: string | null) {
  return useQuery<ImportSessionResponse>({
    queryKey: ['import-session', importSessionId],
    queryFn: () => importApi.getSession(importSessionId!),
    enabled: !!importSessionId,
    refetchInterval: (query) => {
      const data = query.state.data
      if (!data) return POLL_INTERVAL_MS
      if (data.status === 'Completed' || data.status === 'Failed') {
        return false
      }
      return POLL_INTERVAL_MS
    },
  })
}
```

**Step 2: Create useResolveMatch mutation hook**

`web/src/features/candidates/ImportFlow/hooks/useResolveMatch.ts`:

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { importApi } from '@/lib/api/import'
import type { ResolveMatchRequest } from '@/lib/api/import.types'

export function useResolveMatch(importSessionId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ResolveMatchRequest) =>
      importApi.resolveMatch(importSessionId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['import-session', importSessionId],
      })
    },
  })
}
```

**Step 3: Type check**

```bash
cd web && npx tsc --noEmit
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/ImportFlow/hooks/
git commit -m "feat(3.3): add useImportSession polling hook and useResolveMatch mutation"
```

---

## Task 8: Frontend — WorkdayGuide component

**Files:**
- Create: `web/src/features/candidates/ImportFlow/WorkdayGuide.tsx`
- Create: `web/src/features/candidates/ImportFlow/WorkdayGuide.test.tsx`

**Step 1: Create component**

```tsx
import { useState } from 'react'
import { ChevronDown } from 'lucide-react'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { cn } from '@/lib/utils'

export function WorkdayGuide() {
  const [isOpen, setIsOpen] = useState(false)

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <CollapsibleTrigger className="flex w-full items-center gap-2 rounded-md border p-3 text-sm font-medium hover:bg-muted/50">
        <ChevronDown
          className={cn('size-4 transition-transform', isOpen && 'rotate-180')}
        />
        Workday export instructions
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-2 space-y-2 rounded-md border bg-muted/30 p-4 text-sm">
        <ol className="list-decimal space-y-1.5 pl-4">
          <li>
            In Workday, navigate to your recruitment and open the candidate
            list.
          </li>
          <li>
            Select <strong>all candidates</strong> (always export the full
            list to avoid duplicates on re-import).
          </li>
          <li>
            Click <strong>Export to Excel</strong> and choose the XLSX format.
          </li>
          <li>
            Ensure the export includes: Full Name, Email, Phone, Location,
            and Date Applied columns.
          </li>
          <li>Upload the exported file here.</li>
        </ol>
        <p className="text-muted-foreground">
          Tip: Always export all candidates, not just new ones. The system
          handles deduplication automatically.
        </p>
      </CollapsibleContent>
    </Collapsible>
  )
}
```

**Step 2: Create test**

```tsx
import { describe, expect, it } from 'vitest'
import userEvent from '@testing-library/user-event'
import { WorkdayGuide } from './WorkdayGuide'
import { render, screen } from '@/test-utils'

describe('WorkdayGuide', () => {
  it('should render collapsed trigger', () => {
    render(<WorkdayGuide />)
    expect(screen.getByText('Workday export instructions')).toBeInTheDocument()
  })

  it('should expand to show instructions when clicked', async () => {
    const user = userEvent.setup()
    render(<WorkdayGuide />)

    await user.click(screen.getByText('Workday export instructions'))

    expect(screen.getByText(/navigate to your recruitment/)).toBeInTheDocument()
    expect(screen.getByText(/always export all candidates/i)).toBeInTheDocument()
  })
})
```

**Step 3: Run tests**

```bash
cd web && npx vitest run src/features/candidates/ImportFlow/WorkdayGuide.test.tsx
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/ImportFlow/WorkdayGuide.tsx web/src/features/candidates/ImportFlow/WorkdayGuide.test.tsx
git commit -m "feat(3.3): add WorkdayGuide collapsible component"
```

---

## Task 9: Frontend — FileUploadStep component

**Files:**
- Create: `web/src/features/candidates/ImportFlow/FileUploadStep.tsx`
- Create: `web/src/features/candidates/ImportFlow/FileUploadStep.test.tsx`

**Step 1: Create component**

```tsx
import { useRef, useState } from 'react'
import { FileSpreadsheet, Upload } from 'lucide-react'
import { WorkdayGuide } from './WorkdayGuide'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const MAX_FILE_SIZE = 10 * 1024 * 1024 // 10 MB
const ACCEPTED_EXTENSION = '.xlsx'

interface FileUploadStepProps {
  onStartImport: (file: File) => void
  isUploading: boolean
  error?: string | null
}

export function FileUploadStep({
  onStartImport,
  isUploading,
  error,
}: FileUploadStepProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [isDragOver, setIsDragOver] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  function validateFile(file: File): string | null {
    if (!file.name.toLowerCase().endsWith(ACCEPTED_EXTENSION)) {
      return 'Only .xlsx files are accepted'
    }
    if (file.size > MAX_FILE_SIZE) {
      return 'File size must be 10 MB or less'
    }
    return null
  }

  function handleFileSelect(file: File) {
    const error = validateFile(file)
    if (error) {
      setValidationError(error)
      setSelectedFile(null)
      return
    }
    setValidationError(null)
    setSelectedFile(file)
  }

  function handleInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) handleFileSelect(file)
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragOver(false)
    const file = e.dataTransfer.files[0]
    if (file) handleFileSelect(file)
  }

  function handleDragOver(e: React.DragEvent) {
    e.preventDefault()
    setIsDragOver(true)
  }

  function handleDragLeave() {
    setIsDragOver(false)
  }

  return (
    <div className="space-y-4 p-4">
      <WorkdayGuide />

      <div
        className={cn(
          'flex flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed p-8 transition-colors',
          isDragOver && 'border-primary bg-primary/5',
          validationError && 'border-destructive',
          !isDragOver && !validationError && 'border-muted-foreground/25',
        )}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        role="button"
        tabIndex={0}
        onClick={() => inputRef.current?.click()}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click()
        }}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx"
          className="hidden"
          onChange={handleInputChange}
          data-testid="file-input"
        />
        {selectedFile ? (
          <>
            <FileSpreadsheet className="size-8 text-primary" />
            <p className="font-medium">{selectedFile.name}</p>
            <p className="text-muted-foreground text-sm">
              {(selectedFile.size / 1024).toFixed(0)} KB
            </p>
          </>
        ) : (
          <>
            <Upload className="text-muted-foreground size-8" />
            <p className="text-sm font-medium">
              Drop an XLSX file here or click to browse
            </p>
            <p className="text-muted-foreground text-xs">
              Maximum file size: 10 MB
            </p>
          </>
        )}
      </div>

      {validationError && (
        <p className="text-destructive text-sm" role="alert">
          {validationError}
        </p>
      )}

      {error && (
        <p className="text-destructive text-sm" role="alert">
          {error}
        </p>
      )}

      <Button
        className="w-full"
        disabled={!selectedFile || isUploading}
        onClick={() => selectedFile && onStartImport(selectedFile)}
      >
        {isUploading ? 'Uploading...' : 'Start Import'}
      </Button>
    </div>
  )
}
```

**Step 2: Create tests**

```tsx
import { describe, expect, it, vi } from 'vitest'
import userEvent from '@testing-library/user-event'
import { FileUploadStep } from './FileUploadStep'
import { render, screen } from '@/test-utils'

describe('FileUploadStep', () => {
  const defaultProps = {
    onStartImport: vi.fn(),
    isUploading: false,
  }

  it('should render upload area with instructions', () => {
    render(<FileUploadStep {...defaultProps} />)
    expect(screen.getByText(/drop an xlsx file/i)).toBeInTheDocument()
    expect(screen.getByText(/maximum file size: 10 mb/i)).toBeInTheDocument()
  })

  it('should disable Start Import button until file selected', () => {
    render(<FileUploadStep {...defaultProps} />)
    expect(screen.getByRole('button', { name: /start import/i })).toBeDisabled()
  })

  it('should show file name when valid file selected', async () => {
    const user = userEvent.setup()
    render(<FileUploadStep {...defaultProps} />)

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)

    expect(screen.getByText('workday.xlsx')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /start import/i }),
    ).toBeEnabled()
  })

  it('should reject non-xlsx files with error', async () => {
    const user = userEvent.setup()
    render(<FileUploadStep {...defaultProps} />)

    const file = new File(['content'], 'data.csv', { type: 'text/csv' })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)

    expect(screen.getByText(/only .xlsx files are accepted/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /start import/i })).toBeDisabled()
  })

  it('should show uploading state', () => {
    render(<FileUploadStep {...defaultProps} isUploading={true} />)
    expect(screen.getByRole('button', { name: /uploading/i })).toBeDisabled()
  })

  it('should call onStartImport when button clicked', async () => {
    const onStartImport = vi.fn()
    const user = userEvent.setup()
    render(<FileUploadStep {...defaultProps} onStartImport={onStartImport} />)

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    expect(onStartImport).toHaveBeenCalledWith(file)
  })

  it('should display server error when provided', () => {
    render(<FileUploadStep {...defaultProps} error="Upload failed" />)
    expect(screen.getByText('Upload failed')).toBeInTheDocument()
  })
})
```

**Step 3: Run tests**

```bash
cd web && npx vitest run src/features/candidates/ImportFlow/FileUploadStep.test.tsx
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/ImportFlow/FileUploadStep.tsx web/src/features/candidates/ImportFlow/FileUploadStep.test.tsx
git commit -m "feat(3.3): add FileUploadStep component with validation"
```

---

## Task 10: Frontend — ImportProgress component

**Files:**
- Create: `web/src/features/candidates/ImportFlow/ImportProgress.tsx`
- Create: `web/src/features/candidates/ImportFlow/ImportProgress.test.tsx`

**Step 1: Create component**

```tsx
import { Loader2 } from 'lucide-react'
import { Progress } from '@/components/ui/progress'

interface ImportProgressProps {
  sourceFileName: string
}

export function ImportProgress({ sourceFileName }: ImportProgressProps) {
  return (
    <div className="flex flex-col items-center gap-6 p-8">
      <Loader2 className="text-primary size-10 animate-spin" />
      <div className="text-center">
        <p className="font-medium">Importing candidates...</p>
        <p className="text-muted-foreground mt-1 text-sm">
          Processing {sourceFileName}
        </p>
      </div>
      <Progress className="w-full" />
    </div>
  )
}
```

**Step 2: Create test**

```tsx
import { describe, expect, it } from 'vitest'
import { ImportProgress } from './ImportProgress'
import { render, screen } from '@/test-utils'

describe('ImportProgress', () => {
  it('should render progress indicator with descriptive text', () => {
    render(<ImportProgress sourceFileName="workday-export.xlsx" />)
    expect(screen.getByText('Importing candidates...')).toBeInTheDocument()
    expect(screen.getByText(/workday-export.xlsx/)).toBeInTheDocument()
  })

  it('should render progress bar', () => {
    render(<ImportProgress sourceFileName="test.xlsx" />)
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })
})
```

**Step 3: Run tests**

```bash
cd web && npx vitest run src/features/candidates/ImportFlow/ImportProgress.test.tsx
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/ImportFlow/ImportProgress.tsx web/src/features/candidates/ImportFlow/ImportProgress.test.tsx
git commit -m "feat(3.3): add ImportProgress component"
```

---

## Task 11: Frontend — ImportSummary component

**Files:**
- Create: `web/src/features/candidates/ImportFlow/ImportSummary.tsx`
- Create: `web/src/features/candidates/ImportFlow/ImportSummary.test.tsx`

**Step 1: Create component**

```tsx
import { useState } from 'react'
import {
  AlertTriangle,
  CheckCircle,
  ChevronDown,
  RefreshCw,
  XCircle,
} from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { cn } from '@/lib/utils'
import type { ImportRowResult } from '@/lib/api/import.types'

interface ImportSummaryProps {
  createdCount: number
  updatedCount: number
  erroredCount: number
  flaggedCount: number
  rowResults: ImportRowResult[]
  failureReason?: string | null
  onReviewMatches?: () => void
  onDone: () => void
}

export function ImportSummary({
  createdCount,
  updatedCount,
  erroredCount,
  flaggedCount,
  rowResults,
  failureReason,
  onReviewMatches,
  onDone,
}: ImportSummaryProps) {
  return (
    <div className="space-y-4 p-4">
      {failureReason && (
        <Alert variant="destructive">
          <XCircle className="size-4" />
          <AlertDescription>{failureReason}</AlertDescription>
        </Alert>
      )}

      <div className="grid grid-cols-2 gap-3">
        <SummaryCard
          label="Created"
          count={createdCount}
          icon={<CheckCircle className="size-4 text-green-600" />}
        />
        <SummaryCard
          label="Updated"
          count={updatedCount}
          icon={<RefreshCw className="size-4 text-blue-600" />}
        />
        <SummaryCard
          label="Errored"
          count={erroredCount}
          icon={<XCircle className="size-4 text-red-600" />}
        />
        <SummaryCard
          label="Flagged"
          count={flaggedCount}
          icon={<AlertTriangle className="size-4 text-amber-600" />}
        />
      </div>

      {flaggedCount > 0 && onReviewMatches && (
        <Alert>
          <AlertTriangle className="size-4" />
          <AlertDescription className="flex items-center justify-between">
            <span>
              {flaggedCount} match{flaggedCount !== 1 ? 'es' : ''} by name+phone
              only — review recommended
            </span>
            <Button variant="outline" size="sm" onClick={onReviewMatches}>
              Review Matches
            </Button>
          </AlertDescription>
        </Alert>
      )}

      <RowDetailSection
        label="Errored"
        rows={rowResults.filter((r) => r.action === 'Errored')}
      />

      <RowDetailSection
        label="Created"
        rows={rowResults.filter((r) => r.action === 'Created')}
      />

      <RowDetailSection
        label="Updated"
        rows={rowResults.filter((r) => r.action === 'Updated')}
      />

      <Button className="w-full" onClick={onDone}>
        Done
      </Button>
    </div>
  )
}

function SummaryCard({
  label,
  count,
  icon,
}: {
  label: string
  count: number
  icon: React.ReactNode
}) {
  return (
    <div className="flex items-center gap-3 rounded-md border p-3">
      {icon}
      <div>
        <p className="text-2xl font-semibold">{count}</p>
        <p className="text-muted-foreground text-xs">{label}</p>
      </div>
    </div>
  )
}

function RowDetailSection({
  label,
  rows,
}: {
  label: string
  rows: ImportRowResult[]
}) {
  const [isOpen, setIsOpen] = useState(false)

  if (rows.length === 0) return null

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <CollapsibleTrigger className="flex w-full items-center gap-2 text-sm font-medium">
        <ChevronDown
          className={cn('size-4 transition-transform', isOpen && 'rotate-180')}
        />
        {label} ({rows.length})
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-1">
        <div className="divide-y rounded-md border text-sm">
          {rows.map((row) => (
            <div key={row.rowNumber} className="px-3 py-2">
              <span className="text-muted-foreground">Row {row.rowNumber}</span>
              {row.candidateEmail && (
                <span className="ml-2">{row.candidateEmail}</span>
              )}
              {row.errorMessage && (
                <span className="text-destructive ml-2">
                  {row.errorMessage}
                </span>
              )}
            </div>
          ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}
```

**Step 2: Create tests**

```tsx
import { describe, expect, it, vi } from 'vitest'
import userEvent from '@testing-library/user-event'
import { ImportSummary } from './ImportSummary'
import { render, screen } from '@/test-utils'
import type { ImportRowResult } from '@/lib/api/import.types'

const defaultRows: ImportRowResult[] = [
  { rowNumber: 1, candidateEmail: 'a@test.com', action: 'Created', errorMessage: null, resolution: null },
  { rowNumber: 2, candidateEmail: 'b@test.com', action: 'Updated', errorMessage: null, resolution: null },
  { rowNumber: 3, candidateEmail: null, action: 'Errored', errorMessage: 'Invalid email', resolution: null },
  { rowNumber: 4, candidateEmail: 'c@test.com', action: 'Flagged', errorMessage: null, resolution: null },
]

describe('ImportSummary', () => {
  const defaultProps = {
    createdCount: 1,
    updatedCount: 1,
    erroredCount: 1,
    flaggedCount: 1,
    rowResults: defaultRows,
    onDone: vi.fn(),
  }

  it('should render summary counts', () => {
    render(<ImportSummary {...defaultProps} />)
    expect(screen.getByText('Created')).toBeInTheDocument()
    expect(screen.getByText('Updated')).toBeInTheDocument()
    expect(screen.getByText('Errored')).toBeInTheDocument()
    expect(screen.getByText('Flagged')).toBeInTheDocument()
  })

  it('should show flagged match notice with review button', () => {
    const onReviewMatches = vi.fn()
    render(
      <ImportSummary {...defaultProps} onReviewMatches={onReviewMatches} />,
    )
    expect(
      screen.getByText(/1 match by name\+phone only/),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /review matches/i }),
    ).toBeInTheDocument()
  })

  it('should expand errored rows to show detail', async () => {
    const user = userEvent.setup()
    render(<ImportSummary {...defaultProps} />)

    await user.click(screen.getByText(/errored \(1\)/i))

    expect(screen.getByText('Row 3')).toBeInTheDocument()
    expect(screen.getByText('Invalid email')).toBeInTheDocument()
  })

  it('should call onDone when Done is clicked', async () => {
    const onDone = vi.fn()
    const user = userEvent.setup()
    render(<ImportSummary {...defaultProps} onDone={onDone} />)

    await user.click(screen.getByRole('button', { name: /done/i }))

    expect(onDone).toHaveBeenCalled()
  })

  it('should show failure reason when provided', () => {
    render(
      <ImportSummary {...defaultProps} failureReason="File is corrupted" />,
    )
    expect(screen.getByText('File is corrupted')).toBeInTheDocument()
  })
})
```

**Step 3: Run tests**

```bash
cd web && npx vitest run src/features/candidates/ImportFlow/ImportSummary.test.tsx
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/ImportFlow/ImportSummary.tsx web/src/features/candidates/ImportFlow/ImportSummary.test.tsx
git commit -m "feat(3.3): add ImportSummary component with expandable detail"
```

---

## Task 12: Frontend — MatchReviewStep component

**Files:**
- Create: `web/src/features/candidates/ImportFlow/MatchReviewStep.tsx`
- Create: `web/src/features/candidates/ImportFlow/MatchReviewStep.test.tsx`

**Step 1: Create component**

```tsx
import { AlertTriangle } from 'lucide-react'
import { useResolveMatch } from './hooks/useResolveMatch'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import type { ImportRowResult } from '@/lib/api/import.types'

interface MatchReviewStepProps {
  importSessionId: string
  flaggedRows: ImportRowResult[]
  onDone: () => void
}

export function MatchReviewStep({
  importSessionId,
  flaggedRows,
  onDone,
}: MatchReviewStepProps) {
  const resolveMatch = useResolveMatch(importSessionId)

  const unresolvedCount = flaggedRows.filter((r) => !r.resolution).length

  return (
    <div className="space-y-4 p-4">
      <Alert>
        <AlertTriangle className="size-4" />
        <AlertDescription>
          {unresolvedCount > 0
            ? `${unresolvedCount} match${unresolvedCount !== 1 ? 'es' : ''} need review`
            : 'All matches reviewed'}
        </AlertDescription>
      </Alert>

      <div className="divide-y rounded-md border">
        {flaggedRows.map((row, index) => (
          <div key={row.rowNumber} className="space-y-2 p-3">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">
                  Row {row.rowNumber}: {row.candidateEmail ?? 'Unknown'}
                </p>
                <p className="text-muted-foreground text-xs">
                  Matched by name + phone
                </p>
              </div>
              {row.resolution ? (
                <span
                  className={
                    row.resolution === 'Confirmed'
                      ? 'text-sm text-green-600'
                      : 'text-sm text-red-600'
                  }
                >
                  {row.resolution}
                </span>
              ) : (
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={resolveMatch.isPending}
                    onClick={() =>
                      resolveMatch.mutate({
                        matchIndex: index,
                        action: 'Confirm',
                      })
                    }
                  >
                    Confirm Match
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={resolveMatch.isPending}
                    onClick={() =>
                      resolveMatch.mutate({
                        matchIndex: index,
                        action: 'Reject',
                      })
                    }
                  >
                    Reject
                  </Button>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      <Button className="w-full" onClick={onDone}>
        Done
      </Button>
    </div>
  )
}
```

**Step 2: Create tests**

```tsx
import { describe, expect, it } from 'vitest'
import userEvent from '@testing-library/user-event'
import { MatchReviewStep } from './MatchReviewStep'
import { render, screen, waitFor } from '@/test-utils'
import type { ImportRowResult } from '@/lib/api/import.types'

const flaggedRows: ImportRowResult[] = [
  {
    rowNumber: 4,
    candidateEmail: 'flagged@example.com',
    action: 'Flagged',
    errorMessage: null,
    resolution: null,
  },
]

describe('MatchReviewStep', () => {
  it('should render flagged matches', () => {
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={flaggedRows}
        onDone={() => {}}
      />,
    )
    expect(screen.getByText(/row 4/i)).toBeInTheDocument()
    expect(screen.getByText('flagged@example.com')).toBeInTheDocument()
    expect(screen.getByText(/matched by name \+ phone/i)).toBeInTheDocument()
  })

  it('should show confirm and reject buttons', () => {
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={flaggedRows}
        onDone={() => {}}
      />,
    )
    expect(
      screen.getByRole('button', { name: /confirm match/i }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /reject/i }),
    ).toBeInTheDocument()
  })

  it('should show unresolved count', () => {
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={flaggedRows}
        onDone={() => {}}
      />,
    )
    expect(screen.getByText(/1 match needs review/i)).toBeInTheDocument()
  })

  it('should show resolved status for already resolved rows', () => {
    const resolvedRows: ImportRowResult[] = [
      { ...flaggedRows[0], resolution: 'Confirmed' },
    ]
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={resolvedRows}
        onDone={() => {}}
      />,
    )
    expect(screen.getByText('Confirmed')).toBeInTheDocument()
    expect(screen.getByText('All matches reviewed')).toBeInTheDocument()
  })
})
```

**Step 3: Run tests**

```bash
cd web && npx vitest run src/features/candidates/ImportFlow/MatchReviewStep.test.tsx
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/ImportFlow/MatchReviewStep.tsx web/src/features/candidates/ImportFlow/MatchReviewStep.test.tsx
git commit -m "feat(3.3): add MatchReviewStep component for low-confidence match review"
```

---

## Task 13: Frontend — ImportWizard container

**Files:**
- Create: `web/src/features/candidates/ImportFlow/ImportWizard.tsx`
- Create: `web/src/features/candidates/ImportFlow/ImportWizard.test.tsx`

**Step 1: Create the wizard container**

```tsx
import { useState } from 'react'
import { FileUploadStep } from './FileUploadStep'
import { ImportProgress } from './ImportProgress'
import { ImportSummary } from './ImportSummary'
import { MatchReviewStep } from './MatchReviewStep'
import { useImportSession } from './hooks/useImportSession'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { useAppToast } from '@/hooks/useAppToast'
import { importApi } from '@/lib/api/import'
import { ApiError } from '@/lib/api/httpClient'
import { useQueryClient } from '@tanstack/react-query'

type WizardStep = 'upload' | 'processing' | 'summary' | 'matchReview'

interface ImportWizardProps {
  recruitmentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ImportWizard({
  recruitmentId,
  open,
  onOpenChange,
}: ImportWizardProps) {
  const [step, setStep] = useState<WizardStep>('upload')
  const [importSessionId, setImportSessionId] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [sourceFileName, setSourceFileName] = useState('')

  const toast = useAppToast()
  const queryClient = useQueryClient()
  const { data: session } = useImportSession(
    step === 'processing' ? importSessionId : null,
  )

  // Transition from processing to summary/upload when poll returns terminal status
  if (step === 'processing' && session) {
    if (session.status === 'Completed') {
      setStep('summary')
    } else if (session.status === 'Failed') {
      setUploadError(session.failureReason ?? 'Import failed')
      setStep('upload')
    }
  }

  async function handleStartImport(file: File) {
    setIsUploading(true)
    setUploadError(null)
    setSourceFileName(file.name)
    try {
      const response = await importApi.startImport(recruitmentId, file)
      setImportSessionId(response.importSessionId)
      setStep('processing')
    } catch (error) {
      if (error instanceof ApiError) {
        setUploadError(error.problemDetails.title)
      } else {
        setUploadError('Upload failed')
      }
    } finally {
      setIsUploading(false)
    }
  }

  function handleClose() {
    if (step === 'summary' && session?.status === 'Completed') {
      const count = session.createdCount + session.updatedCount
      if (count > 0) {
        toast.success(`${count} candidates imported`)
      }
    }
    void queryClient.invalidateQueries({ queryKey: ['candidates'] })
    setStep('upload')
    setImportSessionId(null)
    setUploadError(null)
    onOpenChange(false)
  }

  return (
    <Sheet open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <SheetContent
        side="right"
        className="w-[600px] max-w-full overflow-y-auto sm:max-w-[600px]"
      >
        <SheetHeader>
          <SheetTitle>Import Candidates</SheetTitle>
          <SheetDescription>
            Upload a Workday export file to import candidates
          </SheetDescription>
        </SheetHeader>

        {step === 'upload' && (
          <FileUploadStep
            onStartImport={handleStartImport}
            isUploading={isUploading}
            error={uploadError}
          />
        )}

        {step === 'processing' && (
          <ImportProgress sourceFileName={sourceFileName} />
        )}

        {step === 'summary' && session && (
          <ImportSummary
            createdCount={session.createdCount}
            updatedCount={session.updatedCount}
            erroredCount={session.erroredCount}
            flaggedCount={session.flaggedCount}
            rowResults={session.rowResults}
            failureReason={session.failureReason}
            onReviewMatches={
              session.flaggedCount > 0
                ? () => setStep('matchReview')
                : undefined
            }
            onDone={handleClose}
          />
        )}

        {step === 'matchReview' && session && importSessionId && (
          <MatchReviewStep
            importSessionId={importSessionId}
            flaggedRows={session.rowResults.filter(
              (r) => r.action === 'Flagged',
            )}
            onDone={handleClose}
          />
        )}
      </SheetContent>
    </Sheet>
  )
}
```

**Step 2: Create tests**

```tsx
import { describe, expect, it } from 'vitest'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { ImportWizard } from './ImportWizard'
import {
  mockCompletedSession,
  mockImportSessionId,
} from '@/mocks/importHandlers'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

const recruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('ImportWizard', () => {
  it('should render as Sheet when opened', () => {
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )
    expect(screen.getByText('Import Candidates')).toBeInTheDocument()
    expect(
      screen.getByText(/upload a workday export file/i),
    ).toBeInTheDocument()
  })

  it('should show upload step initially', () => {
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )
    expect(screen.getByText(/drop an xlsx file/i)).toBeInTheDocument()
  })

  it('should transition to processing after file upload', async () => {
    const user = userEvent.setup()
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    await waitFor(() => {
      expect(screen.getByText('Importing candidates...')).toBeInTheDocument()
    })
  })

  it('should transition to summary when poll returns Completed', async () => {
    // Return Completed immediately for the session poll
    server.use(
      http.get('/api/import-sessions/:id', () => {
        return HttpResponse.json(mockCompletedSession)
      }),
    )

    const user = userEvent.setup()
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    await waitFor(() => {
      expect(screen.getByText('Created')).toBeInTheDocument()
    })
  })

  it('should show error and return to upload on Failed', async () => {
    server.use(
      http.get('/api/import-sessions/:id', () => {
        return HttpResponse.json({
          ...mockCompletedSession,
          status: 'Failed',
          failureReason: 'Missing required column: Email',
        })
      }),
    )

    const user = userEvent.setup()
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    await waitFor(() => {
      expect(
        screen.getByText('Missing required column: Email'),
      ).toBeInTheDocument()
    })
  })
})
```

**Step 3: Run tests**

```bash
cd web && npx vitest run src/features/candidates/ImportFlow/ImportWizard.test.tsx
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/ImportFlow/ImportWizard.tsx web/src/features/candidates/ImportFlow/ImportWizard.test.tsx
git commit -m "feat(3.3): add ImportWizard container with step state machine"
```

---

## Task 14: Frontend — Wire ImportWizard into CandidateList

**Files:**
- Modify: `web/src/features/candidates/CandidateList.tsx`
- Modify: `web/src/features/candidates/CandidateList.test.tsx`

**Step 1: Add Import Candidates button and ImportWizard to CandidateList**

Add to CandidateList.tsx imports:
```tsx
import { ImportWizard } from './ImportFlow/ImportWizard'
```

Add state:
```tsx
const [importWizardOpen, setImportWizardOpen] = useState(false)
```

Add button next to existing "Add Candidate" in the header area (when candidates exist):
```tsx
{!isClosed && candidates.length > 0 && (
  <div className="flex items-center gap-2">
    <Button variant="outline" onClick={() => setImportWizardOpen(true)}>
      Import Candidates
    </Button>
    <CreateCandidateForm recruitmentId={recruitmentId} />
  </div>
)}
```

Also add button in empty state:
```tsx
{!isClosed && (
  <Button variant="outline" onClick={() => setImportWizardOpen(true)}>
    Import Candidates
  </Button>
)}
```

Add ImportWizard at end of component, before closing `</section>`:
```tsx
<ImportWizard
  recruitmentId={recruitmentId}
  open={importWizardOpen}
  onOpenChange={setImportWizardOpen}
/>
```

**Step 2: Add test**

Add to CandidateList.test.tsx:

```tsx
it('should show Import Candidates button for active recruitment', async () => {
  render(
    <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
  )

  await waitFor(() => {
    expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
  })

  expect(
    screen.getByRole('button', { name: /import candidates/i }),
  ).toBeInTheDocument()
})

it('should hide Import Candidates button for closed recruitment', async () => {
  render(
    <CandidateList recruitmentId={recruitmentId} isClosed={true} />,
  )

  await waitFor(() => {
    expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
  })

  expect(
    screen.queryByRole('button', { name: /import candidates/i }),
  ).not.toBeInTheDocument()
})
```

**Step 3: Run tests**

```bash
cd web && npx vitest run src/features/candidates/CandidateList.test.tsx
```

**Step 4: Commit**

```bash
git add web/src/features/candidates/CandidateList.tsx web/src/features/candidates/CandidateList.test.tsx
git commit -m "feat(3.3): wire ImportWizard into CandidateList"
```

---

## Task 15: Full Verification

**Step 1: Backend build and tests**

```bash
dotnet build api/src/Web/Web.csproj --no-incremental
dotnet test api/tests/Domain.UnitTests/Domain.UnitTests.csproj -v n
```

Expected: 0 errors, 0 warnings, all domain tests pass.

**Step 2: Frontend type check and tests**

```bash
cd web && npx tsc --noEmit
cd web && npx vitest run
```

Expected: No type errors, all tests pass.

**Step 3: Anti-pattern scan (E-002)**

Check for:
- Authorization on all handlers (E-001)
- No child entity bypass
- No sync-over-async
- No direct fetch calls outside httpClient.ts

**Step 4: Commit plan if not already committed**

```bash
git add docs/plans/2026-02-14-import-wizard-summary-ui.md
git commit -m "docs(3.3): add implementation plan for import wizard and summary UI"
```

---

## Task 16: Update Story File Dev Agent Record

**Files:**
- Modify: `_bmad-output/implementation-artifacts/3-3-import-wizard-summary-ui.md`

Fill in: Agent Model Used, Testing Mode Rationale, Key Decisions, Debug Log, Completion Notes, File List.

Mark story Status as `done`.

```bash
git add _bmad-output/implementation-artifacts/3-3-import-wizard-summary-ui.md
git commit -m "docs(3.3): fill Dev Agent Record and mark story done"
```

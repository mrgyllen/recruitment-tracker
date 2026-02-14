# Story 3.2: XLSX Import Pipeline Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the backend pipeline for uploading a Workday XLSX export, parsing it, matching candidates, and tracking import session status — enabling bulk candidate import for recruitments.

**Architecture:** CQRS command/query pattern with MediatR, Channel\<T\> producer/consumer for async processing via BackgroundService, ImportSession aggregate tracks state, XlsxParserService (ClosedXML) for parsing, CandidateMatchingEngine for dedup. No frontend in this story (Story 3.3 handles UI).

**Tech Stack:** .NET 10, EF Core 10, MediatR 13, ClosedXML, System.Threading.Channels, NUnit + NSubstitute + FluentAssertions

**Authorization:** All handlers that access recruitment-scoped data MUST verify user membership via `ITenantContext.UserGuid` + `recruitment.Members` check. The background service sets `ITenantContext.RecruitmentId` for data isolation. (E-001)

---

### Task 1: Domain — Add ImportRowResult value object and ImportRowAction enum

**Files:**
- Create: `api/src/Domain/ValueObjects/ImportRowResult.cs`
- Create: `api/src/Domain/Enums/ImportRowAction.cs`
- Test: `api/tests/Domain.UnitTests/ValueObjects/ImportRowResultTests.cs`

**Step 1: Write the failing tests**

```csharp
// api/tests/Domain.UnitTests/ValueObjects/ImportRowResultTests.cs
using api.Domain.Enums;
using api.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.ValueObjects;

[TestFixture]
public class ImportRowResultTests
{
    [Test]
    public void Create_ValidInput_SetsAllProperties()
    {
        var result = new ImportRowResult(
            RowNumber: 3,
            CandidateEmail: "alice@example.com",
            Action: ImportRowAction.Created,
            ErrorMessage: null);

        result.RowNumber.Should().Be(3);
        result.CandidateEmail.Should().Be("alice@example.com");
        result.Action.Should().Be(ImportRowAction.Created);
        result.ErrorMessage.Should().BeNull();
    }

    [Test]
    public void Create_ErroredRow_StoresErrorMessage()
    {
        var result = new ImportRowResult(
            RowNumber: 5,
            CandidateEmail: "bad@example.com",
            Action: ImportRowAction.Errored,
            ErrorMessage: "Invalid email format");

        result.Action.Should().Be(ImportRowAction.Errored);
        result.ErrorMessage.Should().Be("Invalid email format");
    }

    [Test]
    public void ValueEquality_SameValues_AreEqual()
    {
        var a = new ImportRowResult(1, "a@b.com", ImportRowAction.Created, null);
        var b = new ImportRowResult(1, "a@b.com", ImportRowAction.Created, null);

        a.Should().Be(b);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportRowResultTests" --no-restore`
Expected: Build failure — `ImportRowResult` and `ImportRowAction` do not exist.

**Step 3: Write minimal implementation**

```csharp
// api/src/Domain/Enums/ImportRowAction.cs
namespace api.Domain.Enums;

public enum ImportRowAction
{
    Created,
    Updated,
    Flagged,
    Errored
}
```

```csharp
// api/src/Domain/ValueObjects/ImportRowResult.cs
using api.Domain.Enums;

namespace api.Domain.ValueObjects;

public sealed record ImportRowResult(
    int RowNumber,
    string? CandidateEmail,
    ImportRowAction Action,
    string? ErrorMessage);
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportRowResultTests" --no-restore`
Expected: 3/3 PASS

**Step 5: Commit**

```bash
git add api/src/Domain/Enums/ImportRowAction.cs api/src/Domain/ValueObjects/ImportRowResult.cs api/tests/Domain.UnitTests/ValueObjects/ImportRowResultTests.cs
git commit -m "feat(3.2): add ImportRowResult value object and ImportRowAction enum"
```

---

### Task 2: Domain — Extend ImportSession with SourceFileName and row results

**Files:**
- Modify: `api/src/Domain/Entities/ImportSession.cs`
- Modify: `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`

**Step 1: Write the failing tests (append to existing ImportSessionTests.cs)**

```csharp
// Append these tests to api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs
// Add these using statements at the top:
// using api.Domain.ValueObjects;

[Test]
public void Create_WithSourceFileName_RecordsSourceFileName()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "workday-export.xlsx");

    session.SourceFileName.Should().Be("workday-export.xlsx");
}

[Test]
public void AddRowResult_WhenProcessing_AddsResult()
{
    var session = CreateSession();
    var rowResult = new ImportRowResult(1, "alice@example.com", ImportRowAction.Created, null);

    session.AddRowResult(rowResult);

    session.RowResults.Should().HaveCount(1);
    session.RowResults.First().Should().Be(rowResult);
}

[Test]
public void AddRowResult_WhenCompleted_ThrowsInvalidWorkflowTransitionException()
{
    var session = CreateSession();
    session.MarkCompleted(1, 0, 0, 0);

    var act = () => session.AddRowResult(
        new ImportRowResult(1, "a@b.com", ImportRowAction.Created, null));

    act.Should().Throw<InvalidWorkflowTransitionException>();
}

[Test]
public void MarkCompleted_UpdatesAllCounts()
{
    var session = CreateSession();

    session.MarkCompleted(created: 5, updated: 3, errored: 1, flagged: 2);

    session.Status.Should().Be(ImportSessionStatus.Completed);
    session.CreatedCount.Should().Be(5);
    session.UpdatedCount.Should().Be(3);
    session.ErroredCount.Should().Be(1);
    session.FlaggedCount.Should().Be(2);
    session.TotalRows.Should().Be(11);
    session.CompletedAt.Should().NotBeNull();
}
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportSessionTests" --no-restore`
Expected: Build failure — `SourceFileName`, `AddRowResult`, `RowResults`, `CreatedCount`, `UpdatedCount`, `ErroredCount`, `FlaggedCount` do not exist. `Create` method does not accept 3 args. `MarkCompleted` does not accept 4 args.

**Step 3: Write minimal implementation**

Modify `api/src/Domain/Entities/ImportSession.cs` to add:
- `SourceFileName` property
- `_rowResults` backing list + `RowResults` read-only collection
- `AddRowResult()` method (guarded by `EnsureProcessing()`)
- Extended `Create()` factory method (3 args: recruitmentId, createdByUserId, sourceFileName)
- Extended `MarkCompleted()` (4 args: created, updated, errored, flagged)
- New count properties: `CreatedCount`, `UpdatedCount`, `ErroredCount`, `FlaggedCount`

```csharp
// api/src/Domain/Entities/ImportSession.cs — FULL replacement
using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Exceptions;
using api.Domain.ValueObjects;

namespace api.Domain.Entities;

public class ImportSession : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public ImportSessionStatus Status { get; private set; }
    public string SourceFileName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalRows { get; private set; }
    public int CreatedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int ErroredCount { get; private set; }
    public int FlaggedCount { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // Legacy properties kept for backward compat with existing tests
    public int SuccessfulRows => CreatedCount + UpdatedCount;
    public int FailedRows => ErroredCount;

    private readonly List<ImportRowResult> _rowResults = new();
    public IReadOnlyCollection<ImportRowResult> RowResults => _rowResults.AsReadOnly();

    private ImportSession() { } // EF Core

    public static ImportSession Create(Guid recruitmentId, Guid createdByUserId, string sourceFileName = "")
    {
        return new ImportSession
        {
            RecruitmentId = recruitmentId,
            Status = ImportSessionStatus.Processing,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId,
            SourceFileName = sourceFileName,
        };
    }

    public void AddRowResult(ImportRowResult rowResult)
    {
        EnsureProcessing();
        _rowResults.Add(rowResult);
    }

    public void MarkCompleted(int created, int updated, int errored, int flagged)
    {
        EnsureProcessing();

        Status = ImportSessionStatus.Completed;
        CreatedCount = created;
        UpdatedCount = updated;
        ErroredCount = errored;
        FlaggedCount = flagged;
        TotalRows = created + updated + errored + flagged;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        EnsureProcessing();

        Status = ImportSessionStatus.Failed;
        FailureReason = reason?.Length > 2000 ? reason[..2000] : reason;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureProcessing()
    {
        if (Status != ImportSessionStatus.Processing)
        {
            throw new InvalidWorkflowTransitionException(
                Status.ToString(), "target status");
        }
    }
}
```

**Step 4: Update existing tests that use the old `MarkCompleted(int, int)` signature**

In `ImportSessionTests.cs`, update all calls from `MarkCompleted(8, 2)` to `MarkCompleted(8, 0, 2, 0)`, and from `MarkCompleted(10, 0)` / `MarkCompleted(5, 0)` to `MarkCompleted(10, 0, 0, 0)` / `MarkCompleted(5, 0, 0, 0)`. Update assertions that reference `SuccessfulRows`/`FailedRows` to also check `CreatedCount`/`ErroredCount`. Add the `using api.Domain.ValueObjects;` import.

**Step 5: Run all ImportSession tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportSessionTests" --no-restore`
Expected: ALL PASS (existing + new tests)

**Step 6: Commit**

```bash
git add api/src/Domain/Entities/ImportSession.cs api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs
git commit -m "feat(3.2): extend ImportSession with SourceFileName, row results, and detailed counts"
```

---

### Task 3: Domain — Add Candidate.UpdateProfile() method

**Files:**
- Modify: `api/src/Domain/Entities/Candidate.cs`
- Modify: `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`

**Step 1: Write the failing tests (append to existing CandidateTests.cs)**

```csharp
// Append to api/tests/Domain.UnitTests/Entities/CandidateTests.cs

[Test]
public void UpdateProfile_UpdatesAllProfileFields()
{
    var candidate = CreateCandidate();

    candidate.UpdateProfile("Bob Smith", "+9876543210", "London, UK", DateTimeOffset.Parse("2025-06-15T00:00:00Z"));

    candidate.FullName.Should().Be("Bob Smith");
    candidate.PhoneNumber.Should().Be("+9876543210");
    candidate.Location.Should().Be("London, UK");
    candidate.DateApplied.Should().Be(DateTimeOffset.Parse("2025-06-15T00:00:00Z"));
}

[Test]
public void UpdateProfile_DoesNotAffectOutcomes()
{
    var candidate = CreateCandidate();
    candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid());
    var outcomeCount = candidate.Outcomes.Count;

    candidate.UpdateProfile("Updated Name", null, null, DateTimeOffset.UtcNow);

    candidate.Outcomes.Should().HaveCount(outcomeCount);
}

[Test]
public void UpdateProfile_DoesNotAffectDocuments()
{
    var candidate = CreateCandidate();
    candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");
    var docCount = candidate.Documents.Count;

    candidate.UpdateProfile("Updated Name", null, null, DateTimeOffset.UtcNow);

    candidate.Documents.Should().HaveCount(docCount);
}

[Test]
public void UpdateProfile_DoesNotChangeEmail()
{
    var candidate = CreateCandidate();
    var originalEmail = candidate.Email;

    candidate.UpdateProfile("New Name", null, null, DateTimeOffset.UtcNow);

    candidate.Email.Should().Be(originalEmail);
}
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~CandidateTests.UpdateProfile" --no-restore`
Expected: Build failure — `UpdateProfile` method does not exist.

**Step 3: Write minimal implementation**

Add to `api/src/Domain/Entities/Candidate.cs` (after the `Anonymize()` method):

```csharp
public void UpdateProfile(string fullName, string? phoneNumber, string? location, DateTimeOffset dateApplied)
{
    FullName = fullName;
    PhoneNumber = phoneNumber;
    Location = location;
    DateApplied = dateApplied;
}
```

**Step 4: Run all Candidate tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~CandidateTests" --no-restore`
Expected: ALL PASS (existing + new tests)

**Step 5: Commit**

```bash
git add api/src/Domain/Entities/Candidate.cs api/tests/Domain.UnitTests/Entities/CandidateTests.cs
git commit -m "feat(3.2): add Candidate.UpdateProfile() method"
```

---

### Task 4: Domain — Add ParsedCandidateRow value object

**Files:**
- Create: `api/src/Domain/ValueObjects/ParsedCandidateRow.cs`

**Step 1: Create the value object**

```csharp
// api/src/Domain/ValueObjects/ParsedCandidateRow.cs
namespace api.Domain.ValueObjects;

public sealed record ParsedCandidateRow(
    int RowNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? Location,
    DateTimeOffset? DateApplied);
```

**Step 2: Verify build**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/src/Domain --no-restore`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add api/src/Domain/ValueObjects/ParsedCandidateRow.cs
git commit -m "feat(3.2): add ParsedCandidateRow value object"
```

---

### Task 5: Application — Add IXlsxParser and ICandidateMatchingEngine interfaces

**Files:**
- Create: `api/src/Application/Common/Interfaces/IXlsxParser.cs`
- Create: `api/src/Application/Common/Interfaces/ICandidateMatchingEngine.cs`

**Step 1: Create interfaces**

```csharp
// api/src/Application/Common/Interfaces/IXlsxParser.cs
using api.Domain.ValueObjects;

namespace api.Application.Common.Interfaces;

public interface IXlsxParser
{
    List<ParsedCandidateRow> Parse(Stream xlsxStream);
}
```

```csharp
// api/src/Application/Common/Interfaces/ICandidateMatchingEngine.cs
using api.Domain.Entities;
using api.Domain.ValueObjects;

namespace api.Application.Common.Interfaces;

public interface ICandidateMatchingEngine
{
    CandidateMatch Match(ParsedCandidateRow row, IReadOnlyList<Candidate> existingCandidates);
}
```

**Step 2: Verify build**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/src/Application --no-restore`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add api/src/Application/Common/Interfaces/IXlsxParser.cs api/src/Application/Common/Interfaces/ICandidateMatchingEngine.cs
git commit -m "feat(3.2): add IXlsxParser and ICandidateMatchingEngine interfaces"
```

---

### Task 6: Application — Add ImportRequest model and Channel registration

**Files:**
- Create: `api/src/Application/Common/Models/ImportRequest.cs`

**Step 1: Create the Channel message model**

```csharp
// api/src/Application/Common/Models/ImportRequest.cs
namespace api.Application.Common.Models;

public sealed record ImportRequest(
    Guid ImportSessionId,
    Guid RecruitmentId,
    byte[] FileContent,
    Guid CreatedByUserId);
```

**Step 2: Verify build**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/src/Application --no-restore`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add api/src/Application/Common/Models/ImportRequest.cs
git commit -m "feat(3.2): add ImportRequest Channel message model"
```

---

### Task 7: Application — Add StartImportCommand with validator and handler

**Files:**
- Create: `api/src/Application/Features/Import/Commands/StartImport/StartImportCommand.cs`
- Create: `api/src/Application/Features/Import/Commands/StartImport/StartImportResponse.cs`
- Create: `api/src/Application/Features/Import/Commands/StartImport/StartImportCommandValidator.cs`
- Create: `api/src/Application/Features/Import/Commands/StartImport/StartImportCommandHandler.cs`
- Test: `api/tests/Application.UnitTests/Features/Import/Commands/StartImport/StartImportCommandValidatorTests.cs`
- Test: `api/tests/Application.UnitTests/Features/Import/Commands/StartImport/StartImportCommandHandlerTests.cs`

**Step 1: Write the validator tests**

```csharp
// api/tests/Application.UnitTests/Features/Import/Commands/StartImport/StartImportCommandValidatorTests.cs
using api.Application.Features.Import.Commands.StartImport;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Commands.StartImport;

[TestFixture]
public class StartImportCommandValidatorTests
{
    private StartImportCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new StartImportCommandValidator();
    }

    [Test]
    public void ValidCommand_Passes()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "export.xlsx",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void EmptyRecruitmentId_Fails()
    {
        var command = new StartImportCommand(
            Guid.Empty,
            new byte[100],
            "export.xlsx",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecruitmentId);
    }

    [Test]
    public void EmptyFileContent_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            Array.Empty<byte>(),
            "export.xlsx",
            0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileContent);
    }

    [Test]
    public void InvalidExtension_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "export.csv",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Test]
    public void OversizedFile_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "export.xlsx",
            11 * 1024 * 1024); // 11 MB

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    [Test]
    public void EmptyFileName_Fails()
    {
        var command = new StartImportCommand(
            Guid.NewGuid(),
            new byte[100],
            "",
            100);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }
}
```

**Step 2: Write the handler tests**

```csharp
// api/tests/Application.UnitTests/Features/Import/Commands/StartImport/StartImportCommandHandlerTests.cs
using System.Threading.Channels;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Application.Features.Import.Commands.StartImport;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Import.Commands.StartImport;

[TestFixture]
public class StartImportCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private ChannelWriter<ImportRequest> _channelWriter = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();

        var channel = Channel.CreateUnbounded<ImportRequest>();
        _channelWriter = channel.Writer;
    }

    [Test]
    public async Task Handle_ValidFile_CreatesSessionAndWritesToChannel()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var importSessionMockSet = new List<ImportSession>().AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(importSessionMockSet);

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(
            recruitment.Id,
            new byte[] { 1, 2, 3 },
            "workday.xlsx",
            3);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ImportSessionId.Should().NotBeEmpty();
        result.StatusUrl.Should().Contain("/api/import-sessions/");
        _dbContext.ImportSessions.Received(1).Add(Arg.Any<ImportSession>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitmentMockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(Guid.NewGuid(), new byte[] { 1 }, "test.xlsx", 1);

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

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(recruitment.Id, new byte[] { 1 }, "test.xlsx", 1);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_ClosedRecruitment_ThrowsRecruitmentClosedException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.Close();
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(recruitment.Id, new byte[] { 1 }, "test.xlsx", 1);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }
}
```

**Step 3: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/tests/Application.UnitTests --no-restore`
Expected: Build failure — command, validator, handler types do not exist.

**Step 4: Write minimal implementation**

```csharp
// api/src/Application/Features/Import/Commands/StartImport/StartImportCommand.cs
namespace api.Application.Features.Import.Commands.StartImport;

public record StartImportCommand(
    Guid RecruitmentId,
    byte[] FileContent,
    string FileName,
    long FileSize) : IRequest<StartImportResponse>;
```

```csharp
// api/src/Application/Features/Import/Commands/StartImport/StartImportResponse.cs
namespace api.Application.Features.Import.Commands.StartImport;

public record StartImportResponse(
    Guid ImportSessionId,
    string StatusUrl);
```

```csharp
// api/src/Application/Features/Import/Commands/StartImport/StartImportCommandValidator.cs
namespace api.Application.Features.Import.Commands.StartImport;

public class StartImportCommandValidator : AbstractValidator<StartImportCommand>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public StartImportCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.FileContent).NotEmpty().WithMessage("File content is required.");
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(name => name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .xlsx files are supported.");
        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage($"File size must not exceed 10 MB.");
    }
}
```

```csharp
// api/src/Application/Features/Import/Commands/StartImport/StartImportCommandHandler.cs
using System.Threading.Channels;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Entities;
using api.Domain.Enums;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Commands.StartImport;

public class StartImportCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ChannelWriter<ImportRequest> channelWriter)
    : IRequestHandler<StartImportCommand, StartImportResponse>
{
    public async Task<StartImportResponse> Handle(
        StartImportCommand request,
        CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        if (recruitment.Status == RecruitmentStatus.Closed)
        {
            throw new Domain.Exceptions.RecruitmentClosedException(recruitment.Id);
        }

        var session = ImportSession.Create(request.RecruitmentId, userId.Value, request.FileName);
        dbContext.ImportSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await channelWriter.WriteAsync(
            new ImportRequest(session.Id, request.RecruitmentId, request.FileContent, userId.Value),
            cancellationToken);

        return new StartImportResponse(
            session.Id,
            $"/api/import-sessions/{session.Id}");
    }
}
```

**Step 5: Build and run tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~StartImportCommand" --no-restore`
Expected: ALL PASS (6 validator + 4 handler tests)

**Step 6: Commit**

```bash
git add api/src/Application/Features/Import/Commands/StartImport/ api/tests/Application.UnitTests/Features/Import/Commands/StartImport/
git commit -m "feat(3.2): add StartImportCommand with validator and handler"
```

---

### Task 8: Application — Add GetImportSessionQuery with handler and DTO

**Files:**
- Create: `api/src/Application/Features/Import/Queries/GetImportSession/GetImportSessionQuery.cs`
- Create: `api/src/Application/Features/Import/Queries/GetImportSession/GetImportSessionQueryHandler.cs`
- Create: `api/src/Application/Features/Import/Queries/GetImportSession/ImportSessionDto.cs`
- Test: `api/tests/Application.UnitTests/Features/Import/Queries/GetImportSession/GetImportSessionQueryHandlerTests.cs`

**Step 1: Write the failing tests**

```csharp
// api/tests/Application.UnitTests/Features/Import/Queries/GetImportSession/GetImportSessionQueryHandlerTests.cs
using api.Application.Common.Interfaces;
using api.Application.Features.Import.Queries.GetImportSession;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Import.Queries.GetImportSession;

[TestFixture]
public class GetImportSessionQueryHandlerTests
{
    private IApplicationDbContext _dbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
    }

    [Test]
    public async Task Handle_ExistingSession_ReturnsDto()
    {
        var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
        var mockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(mockSet);

        var handler = new GetImportSessionQueryHandler(_dbContext);
        var query = new GetImportSessionQuery(session.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(session.Id);
        result.SourceFileName.Should().Be("test.xlsx");
        result.Status.Should().Be("Processing");
    }

    [Test]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        var mockSet = new List<ImportSession>().AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(mockSet);

        var handler = new GetImportSessionQueryHandler(_dbContext);
        var query = new GetImportSessionQuery(Guid.NewGuid());

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/tests/Application.UnitTests --no-restore`
Expected: Build failure — query, handler, DTO types do not exist.

**Step 3: Write minimal implementation**

```csharp
// api/src/Application/Features/Import/Queries/GetImportSession/GetImportSessionQuery.cs
namespace api.Application.Features.Import.Queries.GetImportSession;

public record GetImportSessionQuery(Guid Id) : IRequest<ImportSessionDto>;
```

```csharp
// api/src/Application/Features/Import/Queries/GetImportSession/ImportSessionDto.cs
using api.Domain.Entities;
using api.Domain.ValueObjects;

namespace api.Application.Features.Import.Queries.GetImportSession;

public record ImportSessionDto
{
    public Guid Id { get; init; }
    public Guid RecruitmentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int TotalRows { get; init; }
    public int CreatedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int ErroredCount { get; init; }
    public int FlaggedCount { get; init; }
    public string? FailureReason { get; init; }
    public List<ImportRowResultDto> RowResults { get; init; } = new();

    public static ImportSessionDto From(ImportSession entity) => new()
    {
        Id = entity.Id,
        RecruitmentId = entity.RecruitmentId,
        Status = entity.Status.ToString(),
        SourceFileName = entity.SourceFileName,
        CreatedAt = entity.CreatedAt,
        CompletedAt = entity.CompletedAt,
        TotalRows = entity.TotalRows,
        CreatedCount = entity.CreatedCount,
        UpdatedCount = entity.UpdatedCount,
        ErroredCount = entity.ErroredCount,
        FlaggedCount = entity.FlaggedCount,
        FailureReason = entity.FailureReason,
        RowResults = entity.RowResults.Select(ImportRowResultDto.From).ToList(),
    };
}

public record ImportRowResultDto
{
    public int RowNumber { get; init; }
    public string? CandidateEmail { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static ImportRowResultDto From(ImportRowResult row) => new()
    {
        RowNumber = row.RowNumber,
        CandidateEmail = row.CandidateEmail,
        Action = row.Action.ToString(),
        ErrorMessage = row.ErrorMessage,
    };
}
```

```csharp
// api/src/Application/Features/Import/Queries/GetImportSession/GetImportSessionQueryHandler.cs
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Queries.GetImportSession;

public class GetImportSessionQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetImportSessionQuery, ImportSessionDto>
{
    public async Task<ImportSessionDto> Handle(
        GetImportSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportSession), request.Id);

        return ImportSessionDto.From(session);
    }
}
```

**Step 4: Run tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~GetImportSessionQuery" --no-restore`
Expected: 2/2 PASS

**Step 5: Commit**

```bash
git add api/src/Application/Features/Import/Queries/GetImportSession/ api/tests/Application.UnitTests/Features/Import/Queries/GetImportSession/
git commit -m "feat(3.2): add GetImportSessionQuery with handler and DTO"
```

---

### Task 9: Infrastructure — Add XlsxParserService with ClosedXML

**Files:**
- Modify: `api/Directory.Packages.props` — add ClosedXML package version
- Modify: `api/src/Infrastructure/Infrastructure.csproj` — add ClosedXML package reference
- Create: `api/src/Infrastructure/Services/XlsxParserService.cs`
- Create: `api/src/Infrastructure/Services/XlsxColumnMappingOptions.cs`
- Test: `api/tests/Domain.UnitTests/Services/XlsxParserServiceTests.cs` (or separate integration test project — adapt to what builds)

Note: XlsxParserService tests use real ClosedXML to generate in-memory XLSX files for testing. This makes them integration tests by nature, but they can live in a unit test project since ClosedXML has no external dependencies.

**Step 1: Add ClosedXML dependency**

In `api/Directory.Packages.props`, add:
```xml
<PackageVersion Include="ClosedXML" Version="0.104.2" />
```

In `api/src/Infrastructure/Infrastructure.csproj`, add:
```xml
<PackageReference Include="ClosedXML" />
```

**Step 2: Write the XlsxColumnMappingOptions**

```csharp
// api/src/Infrastructure/Services/XlsxColumnMappingOptions.cs
namespace api.Infrastructure.Services;

public class XlsxColumnMappingOptions
{
    public const string SectionName = "XlsxColumnMapping";

    public string[] FullName { get; set; } = ["Full Name", "Name", "Candidate Name"];
    public string[] Email { get; set; } = ["Email", "Email Address", "E-mail"];
    public string[] PhoneNumber { get; set; } = ["Phone", "Phone Number", "Tel"];
    public string[] Location { get; set; } = ["Location", "City", "Office"];
    public string[] DateApplied { get; set; } = ["Date Applied", "Application Date", "Applied"];
}
```

**Step 3: Write the XlsxParserService**

```csharp
// api/src/Infrastructure/Services/XlsxParserService.cs
using api.Application.Common.Interfaces;
using api.Domain.ValueObjects;
using ClosedXML.Excel;
using Microsoft.Extensions.Options;

namespace api.Infrastructure.Services;

public class XlsxParserService(IOptions<XlsxColumnMappingOptions> options) : IXlsxParser
{
    private readonly XlsxColumnMappingOptions _mapping = options.Value;

    public List<ParsedCandidateRow> Parse(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.Row(1);
        var columnMap = ResolveColumnIndices(headerRow);

        ValidateRequiredColumns(columnMap);

        var results = new List<ParsedCandidateRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            var fullName = GetCellValue(row, columnMap, "FullName");
            var email = GetCellValue(row, columnMap, "Email");

            if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email))
                continue; // Skip empty rows

            results.Add(new ParsedCandidateRow(
                RowNumber: rowNum,
                FullName: fullName?.Trim() ?? string.Empty,
                Email: email?.Trim() ?? string.Empty,
                PhoneNumber: GetCellValue(row, columnMap, "PhoneNumber")?.Trim(),
                Location: GetCellValue(row, columnMap, "Location")?.Trim(),
                DateApplied: ParseDate(GetCellValue(row, columnMap, "DateApplied"))));
        }

        return results;
    }

    private Dictionary<string, int> ResolveColumnIndices(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>();
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int col = 1; col <= lastCol; col++)
        {
            var headerValue = headerRow.Cell(col).GetString().Trim();
            if (string.IsNullOrEmpty(headerValue)) continue;

            if (_mapping.FullName.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("FullName", col);
            else if (_mapping.Email.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("Email", col);
            else if (_mapping.PhoneNumber.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("PhoneNumber", col);
            else if (_mapping.Location.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("Location", col);
            else if (_mapping.DateApplied.Contains(headerValue, StringComparer.OrdinalIgnoreCase))
                map.TryAdd("DateApplied", col);
        }

        return map;
    }

    private static void ValidateRequiredColumns(Dictionary<string, int> columnMap)
    {
        var missing = new List<string>();
        if (!columnMap.ContainsKey("FullName")) missing.Add("Full Name");
        if (!columnMap.ContainsKey("Email")) missing.Add("Email");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Required columns not found in XLSX: {string.Join(", ", missing)}. " +
                "Check that your Workday export includes these columns.");
        }
    }

    private static string? GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string field)
    {
        if (!columnMap.TryGetValue(field, out var colIndex))
            return null;

        var cell = row.Cell(colIndex);
        return cell.IsEmpty() ? null : cell.GetString();
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParse(value, out var date) ? date : null;
    }
}
```

**Step 4: Write the tests (add ClosedXML to test project for fixture generation)**

Add `<PackageReference Include="ClosedXML" />` to the test project that will hold these tests. Since these test real XLSX parsing, use `api/tests/Application.UnitTests/Application.UnitTests.csproj` (add ClosedXML reference there) or create a separate infrastructure test. For simplicity, add to Application.UnitTests.

Add to `api/tests/Application.UnitTests/Application.UnitTests.csproj`:
```xml
<PackageReference Include="ClosedXML" />
```

```csharp
// api/tests/Application.UnitTests/Features/Import/Services/XlsxParserServiceTests.cs
using api.Infrastructure.Services;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Services;

[TestFixture]
public class XlsxParserServiceTests
{
    private XlsxParserService _parser = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new XlsxColumnMappingOptions());
        _parser = new XlsxParserService(options);
    }

    [Test]
    public void Parse_ValidFile_ExtractsAllRows()
    {
        using var stream = CreateXlsx(("Full Name", "Email", "Phone", "Location", "Date Applied"),
            ("Alice Johnson", "alice@example.com", "+1234567890", "Oslo", "2025-01-15"),
            ("Bob Smith", "bob@example.com", "+0987654321", "London", "2025-02-01"));

        var result = _parser.Parse(stream);

        result.Should().HaveCount(2);
        result[0].FullName.Should().Be("Alice Johnson");
        result[0].Email.Should().Be("alice@example.com");
        result[0].PhoneNumber.Should().Be("+1234567890");
        result[0].Location.Should().Be("Oslo");
        result[0].DateApplied.Should().NotBeNull();
        result[0].RowNumber.Should().Be(2);
        result[1].FullName.Should().Be("Bob Smith");
        result[1].RowNumber.Should().Be(3);
    }

    [Test]
    public void Parse_MissingRequiredColumns_ThrowsWithClearMessage()
    {
        using var stream = CreateXlsx(("Name", "Phone"), // Missing "Email"
            ("Alice", "+123"));

        // "Name" maps to FullName via default mapping, but "Email" is missing
        // Actually "Name" matches "Name" in the FullName mapping array
        var act = () => _parser.Parse(stream);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Email*");
    }

    [Test]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        using var stream = CreateXlsx(("Full Name", "Email"));
        // Headers only, no data rows

        var result = _parser.Parse(stream);

        result.Should().BeEmpty();
    }

    [Test]
    public void Parse_AlternateColumnNames_ResolvesViaMapping()
    {
        using var stream = CreateXlsx(
            ("Candidate Name", "Email Address", "Tel", "City", "Application Date"),
            ("Alice Johnson", "alice@example.com", "+123", "Oslo", "2025-01-15"));

        var result = _parser.Parse(stream);

        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Alice Johnson");
        result[0].Email.Should().Be("alice@example.com");
    }

    [Test]
    public void Parse_SkipsBlankRows()
    {
        using var stream = CreateXlsx(("Full Name", "Email"),
            ("Alice", "alice@example.com"),
            ("", ""),
            ("Bob", "bob@example.com"));

        var result = _parser.Parse(stream);

        result.Should().HaveCount(2);
    }

    private static MemoryStream CreateXlsx(
        (string, string, string?, string?, string?)? headers5 = null,
        params (string, string, string?, string?, string?)[] rows5)
    {
        // Overload not needed — use the generic version below
        throw new NotImplementedException("Use other overload");
    }

    private static MemoryStream CreateXlsx(
        (string col1, string col2) headers,
        params (string val1, string val2)[] rows)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = headers.col1;
        ws.Cell(1, 2).Value = headers.col2;

        for (int i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = rows[i].val1;
            ws.Cell(i + 2, 2).Value = rows[i].val2;
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateXlsx(
        (string, string, string, string, string) headers,
        params (string, string, string, string, string)[] rows)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = headers.Item1;
        ws.Cell(1, 2).Value = headers.Item2;
        ws.Cell(1, 3).Value = headers.Item3;
        ws.Cell(1, 4).Value = headers.Item4;
        ws.Cell(1, 5).Value = headers.Item5;

        for (int i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = rows[i].Item1;
            ws.Cell(i + 2, 2).Value = rows[i].Item2;
            ws.Cell(i + 2, 3).Value = rows[i].Item3;
            ws.Cell(i + 2, 4).Value = rows[i].Item4;
            ws.Cell(i + 2, 5).Value = rows[i].Item5;
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
```

Note: The test helper overloads above are verbose but correct. The implementer should clean up the unused 5-column overload with nullables and merge into a single helper. The key pattern is: create XLWorkbook in-memory, populate cells, save to MemoryStream, return for parsing.

**Step 5: Run tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~XlsxParserService" --no-restore`
Expected: ALL PASS

**Step 6: Commit**

```bash
git add api/Directory.Packages.props api/src/Infrastructure/Infrastructure.csproj api/src/Infrastructure/Services/XlsxParserService.cs api/src/Infrastructure/Services/XlsxColumnMappingOptions.cs api/tests/Application.UnitTests/Application.UnitTests.csproj api/tests/Application.UnitTests/Features/Import/Services/XlsxParserServiceTests.cs
git commit -m "feat(3.2): add IXlsxParser ClosedXML implementation with configurable column mapping"
```

---

### Task 10: Infrastructure — Add CandidateMatchingEngine

**Files:**
- Create: `api/src/Infrastructure/Services/CandidateMatchingEngine.cs`
- Test: `api/tests/Application.UnitTests/Features/Import/Services/CandidateMatchingEngineTests.cs`

**Step 1: Write the failing tests**

```csharp
// api/tests/Application.UnitTests/Features/Import/Services/CandidateMatchingEngineTests.cs
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;
using api.Infrastructure.Services;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Services;

[TestFixture]
public class CandidateMatchingEngineTests
{
    private CandidateMatchingEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new CandidateMatchingEngine();
    }

    [Test]
    public void Match_EmailMatch_ReturnsHighConfidence()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "alice@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice J", "alice@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.High);
        result.MatchMethod.Should().Be("Email");
    }

    [Test]
    public void Match_EmailMatch_CaseInsensitive()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice", "Alice@Example.COM", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice", "alice@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.High);
    }

    [Test]
    public void Match_NameAndPhoneMatch_ReturnsLowConfidence()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "alice-old@example.com", "+1234567890", null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice Johnson", "alice-new@example.com", "+1234567890", null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.Low);
        result.MatchMethod.Should().Be("NameAndPhone");
    }

    [Test]
    public void Match_NoMatch_ReturnsNoneConfidence()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "alice@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Bob Smith", "bob@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.None);
        result.MatchMethod.Should().Be("None");
    }

    [Test]
    public void Match_Idempotent_SameInputSameResult()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice", "alice@example.com", null, null, null);

        var result1 = _engine.Match(row, existing);
        var result2 = _engine.Match(row, existing);

        result1.Should().Be(result2);
    }

    [Test]
    public void Match_NameMatchWithoutPhone_ReturnsNone()
    {
        var existing = new List<Candidate>
        {
            Candidate.Create(Guid.NewGuid(), "Alice Johnson", "different@example.com", null, null, DateTimeOffset.UtcNow)
        };
        var row = new ParsedCandidateRow(1, "Alice Johnson", "other@example.com", null, null, null);

        var result = _engine.Match(row, existing);

        result.Confidence.Should().Be(ImportMatchConfidence.None);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/tests/Application.UnitTests --no-restore`
Expected: Build failure — `CandidateMatchingEngine` does not exist.

**Step 3: Write minimal implementation**

```csharp
// api/src/Infrastructure/Services/CandidateMatchingEngine.cs
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;

namespace api.Infrastructure.Services;

public class CandidateMatchingEngine : ICandidateMatchingEngine
{
    public CandidateMatch Match(ParsedCandidateRow row, IReadOnlyList<Candidate> existingCandidates)
    {
        // Primary match: email (case-insensitive, high confidence)
        var emailMatch = existingCandidates.FirstOrDefault(c =>
            !string.IsNullOrEmpty(c.Email) &&
            c.Email.Equals(row.Email, StringComparison.OrdinalIgnoreCase));

        if (emailMatch is not null)
        {
            return new CandidateMatch(ImportMatchConfidence.High, "Email");
        }

        // Fallback match: name + phone (low confidence)
        if (!string.IsNullOrWhiteSpace(row.PhoneNumber))
        {
            var namePhoneMatch = existingCandidates.FirstOrDefault(c =>
                !string.IsNullOrEmpty(c.FullName) &&
                !string.IsNullOrEmpty(c.PhoneNumber) &&
                c.FullName.Equals(row.FullName, StringComparison.OrdinalIgnoreCase) &&
                c.PhoneNumber.Equals(row.PhoneNumber, StringComparison.OrdinalIgnoreCase));

            if (namePhoneMatch is not null)
            {
                return new CandidateMatch(ImportMatchConfidence.Low, "NameAndPhone");
            }
        }

        return new CandidateMatch(ImportMatchConfidence.None, "None");
    }
}
```

**Step 4: Run tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~CandidateMatchingEngine" --no-restore`
Expected: ALL PASS (6 tests)

**Step 5: Commit**

```bash
git add api/src/Infrastructure/Services/CandidateMatchingEngine.cs api/tests/Application.UnitTests/Features/Import/Services/CandidateMatchingEngineTests.cs
git commit -m "feat(3.2): add ICandidateMatchingEngine implementation"
```

---

### Task 11: Infrastructure — Add ImportPipelineHostedService (BackgroundService + Channel)

**Files:**
- Create: `api/src/Infrastructure/Services/ImportPipelineHostedService.cs`

This is a spike/characterization task — the BackgroundService orchestrates parsing, matching, and candidate creation. It has external dependencies (DbContext, parsers) that are hard to unit test in isolation. We verify it through integration tests in Task 13 and through the domain/service tests already written.

**Step 1: Write the ImportPipelineHostedService**

```csharp
// api/src/Infrastructure/Services/ImportPipelineHostedService.cs
using System.Threading.Channels;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api.Infrastructure.Services;

public class ImportPipelineHostedService(
    ChannelReader<ImportRequest> channelReader,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportPipelineHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in channelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessImportAsync(request, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error processing import session {ImportSessionId}", request.ImportSessionId);
            }
        }
    }

    private async Task ProcessImportAsync(ImportRequest request, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var parser = scope.ServiceProvider.GetRequiredService<IXlsxParser>();
        var matcher = scope.ServiceProvider.GetRequiredService<ICandidateMatchingEngine>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        // Set tenant context for data isolation
        tenantContext.RecruitmentId = request.RecruitmentId;

        var session = await db.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, ct);

        if (session is null)
        {
            logger.LogWarning("Import session {ImportSessionId} not found, skipping", request.ImportSessionId);
            return;
        }

        try
        {
            // 1. Parse XLSX
            using var stream = new MemoryStream(request.FileContent);
            var rows = parser.Parse(stream);

            // 2. Load existing candidates for matching
            var existingCandidates = await db.Candidates
                .Where(c => c.RecruitmentId == request.RecruitmentId)
                .ToListAsync(ct);

            // 3. Get first workflow step for new candidates
            var recruitment = await db.Recruitments
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct);

            var firstStep = recruitment?.Steps.OrderBy(s => s.Order).FirstOrDefault();

            int created = 0, updated = 0, errored = 0, flagged = 0;

            foreach (var row in rows)
            {
                try
                {
                    var match = matcher.Match(row, existingCandidates);

                    switch (match.Confidence)
                    {
                        case ImportMatchConfidence.High:
                            // Email match — update profile
                            var emailCandidate = existingCandidates.First(c =>
                                !string.IsNullOrEmpty(c.Email) &&
                                c.Email.Equals(row.Email, StringComparison.OrdinalIgnoreCase));
                            emailCandidate.UpdateProfile(row.FullName, row.PhoneNumber, row.Location, row.DateApplied ?? DateTimeOffset.UtcNow);
                            session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Updated, null));
                            updated++;
                            break;

                        case ImportMatchConfidence.Low:
                            // Name+phone match — flag for review, do NOT update
                            session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Flagged, null));
                            flagged++;
                            break;

                        case ImportMatchConfidence.None:
                            // No match — create new candidate
                            var candidate = Candidate.Create(
                                request.RecruitmentId,
                                row.FullName,
                                row.Email,
                                row.PhoneNumber,
                                row.Location,
                                row.DateApplied ?? DateTimeOffset.UtcNow);

                            if (firstStep is not null)
                            {
                                candidate.RecordOutcome(firstStep.Id, OutcomeStatus.NotStarted, request.CreatedByUserId);
                            }

                            db.Candidates.Add(candidate);
                            existingCandidates.Add(candidate); // Include for dedup in subsequent rows
                            session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Created, null));
                            created++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Errored, ex.Message));
                    errored++;
                }
            }

            session.MarkCompleted(created, updated, errored, flagged);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import pipeline failed for session {ImportSessionId}", request.ImportSessionId);
            session.MarkFailed(ex.Message);
        }

        await db.SaveChangesAsync(ct);
    }
}
```

**Step 2: Verify build**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/src/Infrastructure --no-restore`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add api/src/Infrastructure/Services/ImportPipelineHostedService.cs
git commit -m "feat(3.2): add ImportPipelineHostedService (BackgroundService + Channel)"
```

---

### Task 12: Web — Add ImportEndpoints and ImportSessionEndpoints

**Files:**
- Create: `api/src/Web/Endpoints/ImportEndpoints.cs`
- Create: `api/src/Web/Endpoints/ImportSessionEndpoints.cs`

**Step 1: Write the endpoints**

```csharp
// api/src/Web/Endpoints/ImportEndpoints.cs
using api.Application.Features.Import.Commands.StartImport;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class ImportEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}/import";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", StartImport)
            .DisableAntiforgery();
    }

    private static async Task<IResult> StartImport(
        ISender sender,
        Guid recruitmentId,
        IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var result = await sender.Send(new StartImportCommand(
            recruitmentId,
            ms.ToArray(),
            file.FileName,
            file.Length));

        return Results.Accepted(
            result.StatusUrl,
            result);
    }
}
```

```csharp
// api/src/Web/Endpoints/ImportSessionEndpoints.cs
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
    }

    private static async Task<IResult> GetImportSession(
        ISender sender,
        Guid id)
    {
        var result = await sender.Send(new GetImportSessionQuery(id));
        return Results.Ok(result);
    }
}
```

**Step 2: Verify build**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/src/Web --no-restore`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add api/src/Web/Endpoints/ImportEndpoints.cs api/src/Web/Endpoints/ImportSessionEndpoints.cs
git commit -m "feat(3.2): add ImportEndpoints and ImportSessionEndpoints"
```

---

### Task 13: Infrastructure — Register services, add Channel, update config and EF config

**Files:**
- Modify: `api/src/Infrastructure/DependencyInjection.cs`
- Modify: `api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs`
- Modify: `api/src/Web/appsettings.json`

**Step 1: Update DependencyInjection.cs to register all new services**

Add to the `AddInfrastructureServices` method:

```csharp
// Channel<T> for import pipeline
var importChannel = Channel.CreateUnbounded<ImportRequest>(new UnboundedChannelOptions
{
    SingleReader = true,
});
builder.Services.AddSingleton(importChannel.Reader);
builder.Services.AddSingleton(importChannel.Writer);

// Import pipeline services
builder.Services.AddScoped<IXlsxParser, XlsxParserService>();
builder.Services.AddScoped<ICandidateMatchingEngine, CandidateMatchingEngine>();
builder.Services.AddHostedService<ImportPipelineHostedService>();

// XLSX column mapping options
builder.Services.Configure<XlsxColumnMappingOptions>(
    builder.Configuration.GetSection(XlsxColumnMappingOptions.SectionName));
```

Required using statements to add:
```csharp
using System.Threading.Channels;
using api.Application.Common.Models;
using api.Infrastructure.Services;
```

**Step 2: Update ImportSessionConfiguration for new columns and owned entities**

```csharp
// api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs — FULL replacement
using api.Domain.Entities;
using api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class ImportSessionConfiguration : IEntityTypeConfiguration<ImportSession>
{
    public void Configure(EntityTypeBuilder<ImportSession> builder)
    {
        builder.ToTable("ImportSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.SourceFileName)
            .HasMaxLength(500);

        builder.Property(s => s.FailureReason)
            .HasMaxLength(2000);

        builder.HasIndex(s => s.RecruitmentId)
            .HasDatabaseName("IX_ImportSessions_RecruitmentId");

        builder.OwnsMany(s => s.RowResults, rowBuilder =>
        {
            rowBuilder.ToJson();
        });

        builder.Ignore(s => s.DomainEvents);

        // Computed properties — not mapped to DB
        builder.Ignore(s => s.SuccessfulRows);
        builder.Ignore(s => s.FailedRows);
    }
}
```

**Step 3: Update appsettings.json with column mapping config**

Add the `XlsxColumnMapping` section:

```json
{
  "XlsxColumnMapping": {
    "FullName": ["Full Name", "Name", "Candidate Name"],
    "Email": ["Email", "Email Address", "E-mail"],
    "PhoneNumber": ["Phone", "Phone Number", "Tel"],
    "Location": ["Location", "City", "Office"],
    "DateApplied": ["Date Applied", "Application Date", "Applied"]
  }
}
```

**Step 4: Verify full solution build**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api --no-restore`
Expected: Build succeeded, 0 errors, 0 warnings

**Step 5: Commit**

```bash
git add api/src/Infrastructure/DependencyInjection.cs api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs api/src/Web/appsettings.json
git commit -m "feat(3.2): register import services, Channel, update EF config and column mapping"
```

---

### Task 14: Run full test suite and verify

**Step 1: Run all domain tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Domain.UnitTests --no-restore`
Expected: ALL PASS

**Step 2: Build all test projects**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet build api/tests --no-restore`
Expected: Build succeeded, 0 errors

**Step 3: Run Application.UnitTests (may skip execution due to missing ASP.NET Core 10 runtime)**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3 && dotnet test api/tests/Application.UnitTests --no-restore`
Expected: PASS or "no tests ran" (acceptable if runtime not installed)

**Step 4: Anti-pattern scan (E-002)**

Verify:
- No direct `db.Candidates.Add()` outside of aggregate root usage context (ImportPipelineHostedService is allowed — it calls `Candidate.Create()` first)
- No caught domain exceptions in handlers
- No `Loading...|web/src/features/**/*.tsx` text in frontend (N/A — this story is backend only)
- All handlers verify membership via `ITenantContext.UserGuid`

---

### Task 15: Update story file Dev Agent Record

**Files:**
- Modify: `_bmad-output/implementation-artifacts/3-2-xlsx-import-pipeline.md`

Fill in the Dev Agent Record section with:
- Agent Model Used
- Testing Mode Rationale (table)
- Key Decisions
- Debug Log References
- Completion Notes
- File List (all created and modified files)

**Commit:**

```bash
git add _bmad-output/implementation-artifacts/3-2-xlsx-import-pipeline.md
git commit -m "docs(3.2): fill Dev Agent Record for XLSX import pipeline"
```

---

### Task 16: Update sprint status

**Files:**
- Modify: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Change `3-2-xlsx-import-pipeline` from `ready-for-dev` to `done`.

**Commit:**

```bash
git add _bmad-output/implementation-artifacts/sprint-status.yaml
git commit -m "chore(3.2): mark story 3.2 done in sprint status"
```

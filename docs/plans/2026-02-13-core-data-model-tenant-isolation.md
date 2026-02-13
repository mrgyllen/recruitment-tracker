# Core Data Model & Tenant Isolation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create all domain entities, EF Core configurations, global query filters for per-recruitment data isolation, audit pipeline, and TenantContext middleware — the data foundation for the entire application.

**Architecture:** Three aggregate roots (Recruitment, Candidate, ImportSession) plus standalone AuditEntry. Rich domain entities enforce invariants through aggregate root methods. EF Core global query filters on candidate-related entities provide defense-in-depth data isolation via ITenantContext. MediatR AuditBehaviour captures audit trail for all commands.

**Tech Stack:** .NET 10, EF Core, MediatR, NUnit, FluentAssertions, NSubstitute, Testcontainers (SQL Server)

**Key codebase facts discovered during planning:**
- `BaseEntity` uses `int Id` with `[NotMapped] DomainEvents` collection and `AddDomainEvent()` — our entities need `Guid` IDs, so we'll create a parallel base class
- `BaseAuditableEntity` has `Created`, `CreatedBy`, `LastModified`, `LastModifiedBy` — useful for audit timestamps
- `BaseEvent : INotification` already exists — domain events extend this
- `DispatchDomainEventsInterceptor` already publishes events on `SaveChangesAsync`
- `ApplicationDbContext` inherits `IdentityDbContext<ApplicationUser>` with template's TodoList/TodoItem DbSets
- `ITenantContext.UserId` is `string?` — filter expressions will compare string user IDs
- `Infrastructure.IntegrationTests` project exists with NUnit but no project references or test files
- ASP.NET Core runtime not installed locally — all backend tests must compile correctly; only Domain.UnitTests can execute locally. CI validates runtime execution.

---

## Task 1: Create GuidEntity base class (Spike)

**Testing mode: Spike** — Infrastructure concern. Our domain entities need Guid IDs but the template's BaseEntity uses int. Create a parallel base class rather than modifying the template's BaseEntity (which would break TodoItem/TodoList).

**Files:**
- Create: `api/src/Domain/Common/GuidEntity.cs`

**Step 1: Create GuidEntity base class**

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Domain.Common;

public abstract class GuidEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**Step 2: Verify build**

Run: `cd api && dotnet build`
Expected: Build succeeds.

**Step 3: Commit**

```bash
git add api/src/Domain/Common/GuidEntity.cs
git commit -m "feat(domain): add GuidEntity base class for Guid-keyed entities"
```

---

## Task 2: Create domain enums (Test-first)

**Testing mode: Test-first** — Tests prevent accidental renames/removals of enum values that are part of the domain vocabulary.

**Files:**
- Create: `api/src/Domain/Enums/OutcomeStatus.cs`
- Create: `api/src/Domain/Enums/ImportMatchConfidence.cs`
- Create: `api/src/Domain/Enums/RecruitmentStatus.cs`
- Create: `api/src/Domain/Enums/ImportSessionStatus.cs`
- Create: `api/tests/Domain.UnitTests/Enums/EnumValueTests.cs`

**Step 1: Write failing tests for enum values**

Create `api/tests/Domain.UnitTests/Enums/EnumValueTests.cs`:

```csharp
using api.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Enums;

public class EnumValueTests
{
    [Test]
    public void OutcomeStatus_HasExpectedValues()
    {
        Enum.GetNames<OutcomeStatus>().Should()
            .BeEquivalentTo("NotStarted", "Pass", "Fail", "Hold");
    }

    [Test]
    public void ImportMatchConfidence_HasExpectedValues()
    {
        Enum.GetNames<ImportMatchConfidence>().Should()
            .BeEquivalentTo("High", "Low", "None");
    }

    [Test]
    public void RecruitmentStatus_HasExpectedValues()
    {
        Enum.GetNames<RecruitmentStatus>().Should()
            .BeEquivalentTo("Active", "Closed");
    }

    [Test]
    public void ImportSessionStatus_HasExpectedValues()
    {
        Enum.GetNames<ImportSessionStatus>().Should()
            .BeEquivalentTo("Processing", "Completed", "Failed");
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `cd api && dotnet test tests/Domain.UnitTests/ --filter "FullyQualifiedName~EnumValueTests" --no-restore`
Expected: Compilation error — enum types don't exist yet.

**Step 3: Create the enums**

`api/src/Domain/Enums/OutcomeStatus.cs`:
```csharp
namespace api.Domain.Enums;

public enum OutcomeStatus
{
    NotStarted,
    Pass,
    Fail,
    Hold
}
```

`api/src/Domain/Enums/ImportMatchConfidence.cs`:
```csharp
namespace api.Domain.Enums;

public enum ImportMatchConfidence
{
    High,
    Low,
    None
}
```

`api/src/Domain/Enums/RecruitmentStatus.cs`:
```csharp
namespace api.Domain.Enums;

public enum RecruitmentStatus
{
    Active,
    Closed
}
```

`api/src/Domain/Enums/ImportSessionStatus.cs`:
```csharp
namespace api.Domain.Enums;

public enum ImportSessionStatus
{
    Processing,
    Completed,
    Failed
}
```

**NOTE:** Delete the existing template enum `api/src/Domain/Enums/PriorityLevel.cs` — it belongs to the Todo template and is no longer needed.

**Step 4: Run tests to verify they pass**

Run: `cd api && dotnet test tests/Domain.UnitTests/ --filter "FullyQualifiedName~EnumValueTests" --no-restore`
Expected: All 4 tests pass.

**Step 5: Commit**

```bash
git add api/src/Domain/Enums/ api/tests/Domain.UnitTests/Enums/
git commit -m "feat(domain): add domain enums for outcomes, imports, and recruitment status"
```

---

## Task 3: Create value objects (Test-first)

**Testing mode: Test-first** — Value objects require value-based equality.

**Files:**
- Create: `api/src/Domain/ValueObjects/CandidateMatch.cs`
- Create: `api/src/Domain/ValueObjects/AnonymizationResult.cs`
- Create: `api/tests/Domain.UnitTests/ValueObjects/CandidateMatchTests.cs`
- Create: `api/tests/Domain.UnitTests/ValueObjects/AnonymizationResultTests.cs`

**Step 1: Write failing tests for CandidateMatch**

Create `api/tests/Domain.UnitTests/ValueObjects/CandidateMatchTests.cs`:

```csharp
using api.Domain.Enums;
using api.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.ValueObjects;

public class CandidateMatchTests
{
    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new CandidateMatch(ImportMatchConfidence.High, "email");
        var b = new CandidateMatch(ImportMatchConfidence.High, "email");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new CandidateMatch(ImportMatchConfidence.High, "email");
        var b = new CandidateMatch(ImportMatchConfidence.Low, "name+phone");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        var match = new CandidateMatch(ImportMatchConfidence.High, "email");

        match.Confidence.Should().Be(ImportMatchConfidence.High);
        match.MatchMethod.Should().Be("email");
    }
}
```

**Step 2: Write failing tests for AnonymizationResult**

Create `api/tests/Domain.UnitTests/ValueObjects/AnonymizationResultTests.cs`:

```csharp
using api.Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.ValueObjects;

public class AnonymizationResultTests
{
    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new AnonymizationResult(10, 5);
        var b = new AnonymizationResult(10, 5);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new AnonymizationResult(10, 5);
        var b = new AnonymizationResult(20, 10);

        a.Should().NotBe(b);
    }

    [Test]
    public void Properties_AreReadOnly()
    {
        var result = new AnonymizationResult(10, 5);

        result.CandidatesAnonymized.Should().Be(10);
        result.DocumentsDeleted.Should().Be(5);
    }
}
```

**Step 3: Run tests to verify they fail**

Run: `cd api && dotnet test tests/Domain.UnitTests/ --filter "FullyQualifiedName~CandidateMatchTests|FullyQualifiedName~AnonymizationResultTests" --no-restore`
Expected: Compilation error — types don't exist.

**Step 4: Create the value objects**

`api/src/Domain/ValueObjects/CandidateMatch.cs`:
```csharp
using api.Domain.Enums;

namespace api.Domain.ValueObjects;

public sealed record CandidateMatch(ImportMatchConfidence Confidence, string MatchMethod);
```

`api/src/Domain/ValueObjects/AnonymizationResult.cs`:
```csharp
namespace api.Domain.ValueObjects;

public sealed record AnonymizationResult(int CandidatesAnonymized, int DocumentsDeleted);
```

**NOTE:** Delete `api/src/Domain/ValueObjects/Colour.cs` and `api/tests/Domain.UnitTests/ValueObjects/ColourTests.cs` — template artifacts no longer needed.

**Step 5: Run tests to verify they pass**

Run: `cd api && dotnet test tests/Domain.UnitTests/ --filter "FullyQualifiedName~CandidateMatchTests|FullyQualifiedName~AnonymizationResultTests" --no-restore`
Expected: All 6 tests pass.

**Step 6: Commit**

```bash
git add api/src/Domain/ValueObjects/ api/tests/Domain.UnitTests/ValueObjects/
git commit -m "feat(domain): add CandidateMatch and AnonymizationResult value objects"
```

---

## Task 4: Create domain events and exceptions (Spike)

**Testing mode: Spike** — Events are data records with no logic. Exceptions are simple types tested indirectly via aggregate tests in Task 6.

**Files:**
- Create: `api/src/Domain/Events/CandidateImportedEvent.cs`
- Create: `api/src/Domain/Events/OutcomeRecordedEvent.cs`
- Create: `api/src/Domain/Events/DocumentUploadedEvent.cs`
- Create: `api/src/Domain/Events/RecruitmentCreatedEvent.cs`
- Create: `api/src/Domain/Events/RecruitmentClosedEvent.cs`
- Create: `api/src/Domain/Events/MembershipChangedEvent.cs`
- Create: `api/src/Domain/Exceptions/RecruitmentClosedException.cs`
- Create: `api/src/Domain/Exceptions/DuplicateCandidateException.cs`
- Create: `api/src/Domain/Exceptions/DuplicateStepNameException.cs`
- Create: `api/src/Domain/Exceptions/InvalidWorkflowTransitionException.cs`
- Create: `api/src/Domain/Exceptions/StepHasOutcomesException.cs`

**Step 1: Create domain events**

All events carry only IDs — no PII. Each extends `BaseEvent` (which implements `INotification`).

`api/src/Domain/Events/RecruitmentCreatedEvent.cs`:
```csharp
namespace api.Domain.Events;

public class RecruitmentCreatedEvent : BaseEvent
{
    public Guid RecruitmentId { get; }

    public RecruitmentCreatedEvent(Guid recruitmentId)
    {
        RecruitmentId = recruitmentId;
    }
}
```

`api/src/Domain/Events/RecruitmentClosedEvent.cs`:
```csharp
namespace api.Domain.Events;

public class RecruitmentClosedEvent : BaseEvent
{
    public Guid RecruitmentId { get; }

    public RecruitmentClosedEvent(Guid recruitmentId)
    {
        RecruitmentId = recruitmentId;
    }
}
```

`api/src/Domain/Events/MembershipChangedEvent.cs`:
```csharp
namespace api.Domain.Events;

public class MembershipChangedEvent : BaseEvent
{
    public Guid RecruitmentId { get; }
    public Guid UserId { get; }
    public string ChangeType { get; }

    public MembershipChangedEvent(Guid recruitmentId, Guid userId, string changeType)
    {
        RecruitmentId = recruitmentId;
        UserId = userId;
        ChangeType = changeType;
    }
}
```

`api/src/Domain/Events/CandidateImportedEvent.cs`:
```csharp
namespace api.Domain.Events;

public class CandidateImportedEvent : BaseEvent
{
    public Guid CandidateId { get; }
    public Guid RecruitmentId { get; }

    public CandidateImportedEvent(Guid candidateId, Guid recruitmentId)
    {
        CandidateId = candidateId;
        RecruitmentId = recruitmentId;
    }
}
```

`api/src/Domain/Events/OutcomeRecordedEvent.cs`:
```csharp
namespace api.Domain.Events;

public class OutcomeRecordedEvent : BaseEvent
{
    public Guid CandidateId { get; }
    public Guid WorkflowStepId { get; }

    public OutcomeRecordedEvent(Guid candidateId, Guid workflowStepId)
    {
        CandidateId = candidateId;
        WorkflowStepId = workflowStepId;
    }
}
```

`api/src/Domain/Events/DocumentUploadedEvent.cs`:
```csharp
namespace api.Domain.Events;

public class DocumentUploadedEvent : BaseEvent
{
    public Guid CandidateId { get; }
    public Guid DocumentId { get; }

    public DocumentUploadedEvent(Guid candidateId, Guid documentId)
    {
        CandidateId = candidateId;
        DocumentId = documentId;
    }
}
```

**Step 2: Create domain exceptions**

`api/src/Domain/Exceptions/RecruitmentClosedException.cs`:
```csharp
namespace api.Domain.Exceptions;

public class RecruitmentClosedException : Exception
{
    public RecruitmentClosedException(Guid recruitmentId)
        : base($"Recruitment {recruitmentId} is closed and cannot be modified.")
    {
    }
}
```

`api/src/Domain/Exceptions/DuplicateCandidateException.cs`:
```csharp
namespace api.Domain.Exceptions;

public class DuplicateCandidateException : Exception
{
    public DuplicateCandidateException(string email, Guid recruitmentId)
        : base($"A candidate with email '{email}' already exists in recruitment {recruitmentId}.")
    {
    }
}
```

`api/src/Domain/Exceptions/DuplicateStepNameException.cs`:
```csharp
namespace api.Domain.Exceptions;

public class DuplicateStepNameException : Exception
{
    public DuplicateStepNameException(string stepName)
        : base($"A workflow step named '{stepName}' already exists in this recruitment.")
    {
    }
}
```

`api/src/Domain/Exceptions/InvalidWorkflowTransitionException.cs`:
```csharp
namespace api.Domain.Exceptions;

public class InvalidWorkflowTransitionException : Exception
{
    public InvalidWorkflowTransitionException(string from, string to)
        : base($"Invalid status transition from '{from}' to '{to}'.")
    {
    }
}
```

`api/src/Domain/Exceptions/StepHasOutcomesException.cs`:
```csharp
namespace api.Domain.Exceptions;

public class StepHasOutcomesException : Exception
{
    public StepHasOutcomesException(Guid stepId)
        : base($"Workflow step {stepId} cannot be removed because it has recorded outcomes.")
    {
    }
}
```

**NOTE:** Delete template events `api/src/Domain/Events/TodoItemCompletedEvent.cs`, `TodoItemCreatedEvent.cs`, `TodoItemDeletedEvent.cs` and template exception `api/src/Domain/Exceptions/UnsupportedColourException.cs`.

**Step 3: Verify build**

Run: `cd api && dotnet build`
Expected: Build succeeds with zero errors. (There may be warnings about unused template code — that's expected and will be cleaned up as template artifacts are removed.)

**Step 4: Commit**

```bash
git add api/src/Domain/Events/ api/src/Domain/Exceptions/
git commit -m "feat(domain): add domain events (IDs only, no PII) and domain exceptions"
```

---

## Task 5: Create domain entity tests (Test-first — write tests BEFORE entities)

**Testing mode: Test-first** — Domain logic is the highest-value test target. Write ALL tests first, then implement entities in Task 6.

**Files:**
- Create: `api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`
- Create: `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`
- Create: `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`

**Step 1: Write Recruitment aggregate tests**

Create `api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`:

```csharp
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Events;
using api.Domain.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

public class RecruitmentTests
{
    private Recruitment CreateRecruitment(string title = "Test Recruitment")
    {
        return Recruitment.Create(title, null, Guid.NewGuid());
    }

    [Test]
    public void Create_ValidInput_CreatesRecruitmentWithCreatorAsMember()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Senior Dev", null, creatorId);

        recruitment.Title.Should().Be("Senior Dev");
        recruitment.Status.Should().Be(RecruitmentStatus.Active);
        recruitment.Members.Should().HaveCount(1);
        recruitment.Members.First().UserId.Should().Be(creatorId);
        recruitment.Members.First().Role.Should().Be("Recruiting Leader");
    }

    [Test]
    public void Create_RaisesRecruitmentCreatedEvent()
    {
        var recruitment = CreateRecruitment();

        recruitment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecruitmentCreatedEvent>();
    }

    [Test]
    public void AddStep_ValidName_AddsStep()
    {
        var recruitment = CreateRecruitment();
        recruitment.ClearDomainEvents();

        recruitment.AddStep("Screening", 1);

        recruitment.Steps.Should().HaveCount(1);
        recruitment.Steps.First().Name.Should().Be("Screening");
        recruitment.Steps.First().Order.Should().Be(1);
    }

    [Test]
    public void AddStep_DuplicateName_ThrowsDuplicateStepNameException()
    {
        var recruitment = CreateRecruitment();
        recruitment.AddStep("Screening", 1);

        var act = () => recruitment.AddStep("Screening", 2);

        act.Should().Throw<DuplicateStepNameException>();
    }

    [Test]
    public void AddStep_WhenClosed_ThrowsRecruitmentClosedException()
    {
        var recruitment = CreateRecruitment();
        recruitment.Close();

        var act = () => recruitment.AddStep("Screening", 1);

        act.Should().Throw<RecruitmentClosedException>();
    }

    [Test]
    public void RemoveStep_ValidStep_RemovesStep()
    {
        var recruitment = CreateRecruitment();
        recruitment.AddStep("Screening", 1);
        var stepId = recruitment.Steps.First().Id;

        recruitment.RemoveStep(stepId);

        recruitment.Steps.Should().BeEmpty();
    }

    [Test]
    public void RemoveStep_StepWithOutcomes_ThrowsStepHasOutcomesException()
    {
        var recruitment = CreateRecruitment();
        recruitment.AddStep("Screening", 1);
        var stepId = recruitment.Steps.First().Id;

        // Mark the step as having outcomes via the recruitment method
        recruitment.MarkStepHasOutcomes(stepId);

        var act = () => recruitment.RemoveStep(stepId);

        act.Should().Throw<StepHasOutcomesException>();
    }

    [Test]
    public void AddMember_ValidUser_AddsMember()
    {
        var recruitment = CreateRecruitment();
        var userId = Guid.NewGuid();

        recruitment.AddMember(userId, "SME/Collaborator");

        recruitment.Members.Should().HaveCount(2); // creator + new member
    }

    [Test]
    public void RemoveMember_ValidMember_RemovesMember()
    {
        var recruitment = CreateRecruitment();
        var userId = Guid.NewGuid();
        recruitment.AddMember(userId, "SME/Collaborator");
        var memberId = recruitment.Members.First(m => m.UserId == userId).Id;

        recruitment.RemoveMember(memberId);

        recruitment.Members.Should().HaveCount(1); // only creator remains
    }

    [Test]
    public void RemoveMember_LastLeader_ThrowsInvalidOperationException()
    {
        var recruitment = CreateRecruitment();
        var creatorMemberId = recruitment.Members.First().Id;

        var act = () => recruitment.RemoveMember(creatorMemberId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one Recruiting Leader*");
    }

    [Test]
    public void RemoveMember_RaisesMembershipChangedEvent()
    {
        var recruitment = CreateRecruitment();
        var userId = Guid.NewGuid();
        recruitment.AddMember(userId, "SME/Collaborator");
        recruitment.ClearDomainEvents();
        var memberId = recruitment.Members.First(m => m.UserId == userId).Id;

        recruitment.RemoveMember(memberId);

        recruitment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MembershipChangedEvent>();
    }

    [Test]
    public void Close_ActiveRecruitment_ClosesSuccessfully()
    {
        var recruitment = CreateRecruitment();
        recruitment.ClearDomainEvents();

        recruitment.Close();

        recruitment.Status.Should().Be(RecruitmentStatus.Closed);
        recruitment.ClosedAt.Should().NotBeNull();
        recruitment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RecruitmentClosedEvent>();
    }

    [Test]
    public void Close_AlreadyClosed_ThrowsRecruitmentClosedException()
    {
        var recruitment = CreateRecruitment();
        recruitment.Close();

        var act = () => recruitment.Close();

        act.Should().Throw<RecruitmentClosedException>();
    }
}
```

**Step 2: Write Candidate aggregate tests**

Create `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`:

```csharp
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Events;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

public class CandidateTests
{
    private Candidate CreateCandidate()
    {
        return Candidate.Create(
            recruitmentId: Guid.NewGuid(),
            fullName: "Alice Johnson",
            email: "alice@example.com",
            phoneNumber: "+1234567890",
            location: "Oslo, Norway",
            dateApplied: DateTimeOffset.UtcNow);
    }

    [Test]
    public void RecordOutcome_ValidInput_AddsOutcome()
    {
        var candidate = CreateCandidate();
        var stepId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        candidate.RecordOutcome(stepId, OutcomeStatus.Pass, userId);

        candidate.Outcomes.Should().HaveCount(1);
        candidate.Outcomes.First().WorkflowStepId.Should().Be(stepId);
        candidate.Outcomes.First().Status.Should().Be(OutcomeStatus.Pass);
    }

    [Test]
    public void RecordOutcome_RaisesOutcomeRecordedEvent()
    {
        var candidate = CreateCandidate();
        candidate.ClearDomainEvents();

        candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid());

        candidate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OutcomeRecordedEvent>();
    }

    [Test]
    public void AttachDocument_ValidInput_AddsDocument()
    {
        var candidate = CreateCandidate();

        candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");

        candidate.Documents.Should().HaveCount(1);
        candidate.Documents.First().DocumentType.Should().Be("CV");
    }

    [Test]
    public void AttachDocument_DuplicateType_ThrowsInvalidOperationException()
    {
        var candidate = CreateCandidate();
        candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");

        var act = () => candidate.AttachDocument("CV", "https://blob.storage/cv2.pdf");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*document of type*already exists*");
    }

    [Test]
    public void AttachDocument_RaisesDocumentUploadedEvent()
    {
        var candidate = CreateCandidate();
        candidate.ClearDomainEvents();

        candidate.AttachDocument("CV", "https://blob.storage/cv.pdf");

        candidate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DocumentUploadedEvent>();
    }

    [Test]
    public void Anonymize_ClearsPiiFields()
    {
        var candidate = CreateCandidate();
        var originalId = candidate.Id;
        var originalRecruitmentId = candidate.RecruitmentId;
        var originalDateApplied = candidate.DateApplied;
        candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid());

        candidate.Anonymize();

        candidate.FullName.Should().BeNull();
        candidate.Email.Should().BeNull();
        candidate.PhoneNumber.Should().BeNull();
        candidate.Location.Should().BeNull();
        // Preserved fields
        candidate.Id.Should().Be(originalId);
        candidate.RecruitmentId.Should().Be(originalRecruitmentId);
        candidate.DateApplied.Should().Be(originalDateApplied);
        candidate.Outcomes.Should().HaveCount(1);
    }
}
```

**Step 3: Write ImportSession aggregate tests**

Create `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`:

```csharp
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

public class ImportSessionTests
{
    private ImportSession CreateSession()
    {
        return ImportSession.Create(Guid.NewGuid(), Guid.NewGuid());
    }

    [Test]
    public void Create_SetsProcessingStatus()
    {
        var session = CreateSession();

        session.Status.Should().Be(ImportSessionStatus.Processing);
    }

    [Test]
    public void MarkCompleted_SetsCompletedStatusAndCounts()
    {
        var session = CreateSession();

        session.MarkCompleted(8, 2);

        session.Status.Should().Be(ImportSessionStatus.Completed);
        session.TotalRows.Should().Be(10);
        session.SuccessfulRows.Should().Be(8);
        session.FailedRows.Should().Be(2);
        session.CompletedAt.Should().NotBeNull();
    }

    [Test]
    public void MarkFailed_SetsFailedStatusAndReason()
    {
        var session = CreateSession();

        session.MarkFailed("Invalid file format");

        session.Status.Should().Be(ImportSessionStatus.Failed);
        session.FailureReason.Should().Be("Invalid file format");
        session.CompletedAt.Should().NotBeNull();
    }

    [Test]
    public void MarkCompleted_WhenAlreadyCompleted_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkCompleted(10, 0);

        var act = () => session.MarkCompleted(5, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkCompleted_WhenFailed_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkFailed("error");

        var act = () => session.MarkCompleted(5, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkFailed_WhenAlreadyCompleted_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkCompleted(10, 0);

        var act = () => session.MarkFailed("error");

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkFailed_WhenAlreadyFailed_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkFailed("error 1");

        var act = () => session.MarkFailed("error 2");

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }
}
```

**Step 4: Verify tests fail to compile (entities don't exist yet)**

Run: `cd api && dotnet build tests/Domain.UnitTests/`
Expected: Compilation errors — entity types don't exist.

**Step 5: Commit test files (they won't compile yet — that's expected)**

```bash
git add api/tests/Domain.UnitTests/Entities/
git commit -m "test(domain): add failing tests for Recruitment, Candidate, and ImportSession aggregates"
```

---

## Task 6: Create aggregate root entities with invariants (Test-first — make tests pass)

**Testing mode: Test-first** — Tests from Task 5 define expected behavior. Implement minimal code to pass them.

**Files:**
- Create: `api/src/Domain/Entities/Recruitment.cs`
- Create: `api/src/Domain/Entities/WorkflowStep.cs`
- Create: `api/src/Domain/Entities/RecruitmentMember.cs`
- Create: `api/src/Domain/Entities/Candidate.cs`
- Create: `api/src/Domain/Entities/CandidateOutcome.cs`
- Create: `api/src/Domain/Entities/CandidateDocument.cs`
- Create: `api/src/Domain/Entities/ImportSession.cs`
- Create: `api/src/Domain/Entities/AuditEntry.cs`

**Step 1: Create Recruitment aggregate root + children**

`api/src/Domain/Entities/Recruitment.cs`:
```csharp
using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Events;
using api.Domain.Exceptions;

namespace api.Domain.Entities;

public class Recruitment : GuidEntity
{
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? JobRequisitionId { get; private set; }
    public RecruitmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private readonly List<WorkflowStep> _steps = new();
    public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

    private readonly List<RecruitmentMember> _members = new();
    public IReadOnlyCollection<RecruitmentMember> Members => _members.AsReadOnly();

    // Track which steps have outcomes (set by application layer when outcomes exist)
    private readonly HashSet<Guid> _stepsWithOutcomes = new();

    private Recruitment() { } // EF Core

    public static Recruitment Create(string title, string? description, Guid createdByUserId)
    {
        var recruitment = new Recruitment
        {
            Title = title,
            Description = description,
            Status = RecruitmentStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId,
        };

        recruitment._members.Add(RecruitmentMember.Create(
            recruitment.Id, createdByUserId, "Recruiting Leader"));

        recruitment.AddDomainEvent(new RecruitmentCreatedEvent(recruitment.Id));
        return recruitment;
    }

    public void AddStep(string name, int order)
    {
        EnsureNotClosed();

        if (_steps.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateStepNameException(name);
        }

        _steps.Add(WorkflowStep.Create(Id, name, order));
    }

    public void RemoveStep(Guid stepId)
    {
        EnsureNotClosed();

        if (_stepsWithOutcomes.Contains(stepId))
        {
            throw new StepHasOutcomesException(stepId);
        }

        var step = _steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found.");

        _steps.Remove(step);
    }

    public void MarkStepHasOutcomes(Guid stepId)
    {
        _stepsWithOutcomes.Add(stepId);
    }

    public void AddMember(Guid userId, string role)
    {
        EnsureNotClosed();

        if (_members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException($"User {userId} is already a member.");
        }

        var member = RecruitmentMember.Create(Id, userId, role);
        _members.Add(member);
        AddDomainEvent(new MembershipChangedEvent(Id, userId, "Added"));
    }

    public void RemoveMember(Guid memberId)
    {
        EnsureNotClosed();

        var member = _members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException($"Member {memberId} not found.");

        // Cannot remove the last Recruiting Leader
        if (member.Role == "Recruiting Leader" &&
            _members.Count(m => m.Role == "Recruiting Leader") <= 1)
        {
            throw new InvalidOperationException("Cannot remove the last member — at least one Recruiting Leader must exist.");
        }

        _members.Remove(member);
        AddDomainEvent(new MembershipChangedEvent(Id, member.UserId, "Removed"));
    }

    public void Close()
    {
        EnsureNotClosed();

        Status = RecruitmentStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RecruitmentClosedEvent(Id));
    }

    private void EnsureNotClosed()
    {
        if (Status == RecruitmentStatus.Closed)
        {
            throw new RecruitmentClosedException(Id);
        }
    }
}
```

`api/src/Domain/Entities/WorkflowStep.cs`:
```csharp
using api.Domain.Common;

namespace api.Domain.Entities;

public class WorkflowStep : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Order { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkflowStep() { } // EF Core

    internal static WorkflowStep Create(Guid recruitmentId, string name, int order)
    {
        return new WorkflowStep
        {
            RecruitmentId = recruitmentId,
            Name = name,
            Order = order,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
```

`api/src/Domain/Entities/RecruitmentMember.cs`:
```csharp
using api.Domain.Common;

namespace api.Domain.Entities;

public class RecruitmentMember : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;
    public DateTimeOffset InvitedAt { get; private set; }

    private RecruitmentMember() { } // EF Core

    internal static RecruitmentMember Create(Guid recruitmentId, Guid userId, string role)
    {
        return new RecruitmentMember
        {
            RecruitmentId = recruitmentId,
            UserId = userId,
            Role = role,
            InvitedAt = DateTimeOffset.UtcNow,
        };
    }
}
```

**Step 2: Create Candidate aggregate root + children**

`api/src/Domain/Entities/Candidate.cs`:
```csharp
using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Events;

namespace api.Domain.Entities;

public class Candidate : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Location { get; private set; }
    public DateTimeOffset DateApplied { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<CandidateOutcome> _outcomes = new();
    public IReadOnlyCollection<CandidateOutcome> Outcomes => _outcomes.AsReadOnly();

    private readonly List<CandidateDocument> _documents = new();
    public IReadOnlyCollection<CandidateDocument> Documents => _documents.AsReadOnly();

    private Candidate() { } // EF Core

    public static Candidate Create(
        Guid recruitmentId,
        string fullName,
        string email,
        string? phoneNumber,
        string? location,
        DateTimeOffset dateApplied)
    {
        var candidate = new Candidate
        {
            RecruitmentId = recruitmentId,
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            Location = location,
            DateApplied = dateApplied,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        candidate.AddDomainEvent(new CandidateImportedEvent(candidate.Id, recruitmentId));
        return candidate;
    }

    public void RecordOutcome(Guid workflowStepId, OutcomeStatus status, Guid recordedByUserId)
    {
        var outcome = CandidateOutcome.Create(Id, workflowStepId, status, recordedByUserId);
        _outcomes.Add(outcome);
        AddDomainEvent(new OutcomeRecordedEvent(Id, workflowStepId));
    }

    public void AttachDocument(string documentType, string blobStorageUrl)
    {
        if (_documents.Any(d => d.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A document of type '{documentType}' already exists for this candidate.");
        }

        var document = CandidateDocument.Create(Id, documentType, blobStorageUrl);
        _documents.Add(document);
        AddDomainEvent(new DocumentUploadedEvent(Id, document.Id));
    }

    public void Anonymize()
    {
        FullName = null;
        Email = null;
        PhoneNumber = null;
        Location = null;
    }
}
```

`api/src/Domain/Entities/CandidateOutcome.cs`:
```csharp
using api.Domain.Common;
using api.Domain.Enums;

namespace api.Domain.Entities;

public class CandidateOutcome : GuidEntity
{
    public Guid CandidateId { get; private set; }
    public Guid WorkflowStepId { get; private set; }
    public OutcomeStatus Status { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public Guid RecordedByUserId { get; private set; }

    private CandidateOutcome() { } // EF Core

    internal static CandidateOutcome Create(
        Guid candidateId, Guid workflowStepId, OutcomeStatus status, Guid recordedByUserId)
    {
        return new CandidateOutcome
        {
            CandidateId = candidateId,
            WorkflowStepId = workflowStepId,
            Status = status,
            RecordedAt = DateTimeOffset.UtcNow,
            RecordedByUserId = recordedByUserId,
        };
    }
}
```

`api/src/Domain/Entities/CandidateDocument.cs`:
```csharp
using api.Domain.Common;

namespace api.Domain.Entities;

public class CandidateDocument : GuidEntity
{
    public Guid CandidateId { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public string BlobStorageUrl { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    private CandidateDocument() { } // EF Core

    internal static CandidateDocument Create(Guid candidateId, string documentType, string blobStorageUrl)
    {
        return new CandidateDocument
        {
            CandidateId = candidateId,
            DocumentType = documentType,
            BlobStorageUrl = blobStorageUrl,
            UploadedAt = DateTimeOffset.UtcNow,
        };
    }
}
```

**Step 3: Create ImportSession aggregate root**

`api/src/Domain/Entities/ImportSession.cs`:
```csharp
using api.Domain.Common;
using api.Domain.Enums;
using api.Domain.Exceptions;

namespace api.Domain.Entities;

public class ImportSession : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public ImportSessionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalRows { get; private set; }
    public int SuccessfulRows { get; private set; }
    public int FailedRows { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private ImportSession() { } // EF Core

    public static ImportSession Create(Guid recruitmentId, Guid createdByUserId)
    {
        return new ImportSession
        {
            RecruitmentId = recruitmentId,
            Status = ImportSessionStatus.Processing,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId,
        };
    }

    public void MarkCompleted(int successCount, int failCount)
    {
        EnsureProcessing();

        Status = ImportSessionStatus.Completed;
        SuccessfulRows = successCount;
        FailedRows = failCount;
        TotalRows = successCount + failCount;
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

**Step 4: Create AuditEntry standalone entity**

`api/src/Domain/Entities/AuditEntry.cs`:
```csharp
using api.Domain.Common;

namespace api.Domain.Entities;

public class AuditEntry : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public Guid? EntityId { get; private set; }
    public string EntityType { get; private set; } = null!;
    public string ActionType { get; private set; } = null!;
    public Guid PerformedBy { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }
    public string? Context { get; private set; }

    private AuditEntry() { } // EF Core

    public static AuditEntry Create(
        Guid recruitmentId,
        Guid? entityId,
        string entityType,
        string actionType,
        Guid performedBy,
        string? context)
    {
        return new AuditEntry
        {
            RecruitmentId = recruitmentId,
            EntityId = entityId,
            EntityType = entityType,
            ActionType = actionType,
            PerformedBy = performedBy,
            PerformedAt = DateTimeOffset.UtcNow,
            Context = context,
        };
    }
}
```

**Step 5: Delete template entities**

Delete `api/src/Domain/Entities/TodoItem.cs` and `api/src/Domain/Entities/TodoList.cs`. These are template artifacts. Removing them will cause compilation errors in template code that references them — we'll clean those up after the entity build is verified.

**Step 6: Run domain unit tests**

Run: `cd api && dotnet test tests/Domain.UnitTests/ --filter "FullyQualifiedName~RecruitmentTests|FullyQualifiedName~CandidateTests|FullyQualifiedName~ImportSessionTests" --no-restore`
Expected: All entity tests pass (compilation may fail due to template references — if so, temporarily comment out template code in `ApplicationDbContext.cs` and `IApplicationDbContext.cs` to unblock tests).

**Step 7: Commit**

```bash
git add api/src/Domain/Entities/ api/tests/Domain.UnitTests/Entities/
git commit -m "feat(domain): add Recruitment, Candidate, ImportSession aggregates with invariants"
```

---

## Task 7: Clean up template code and update DbContext interfaces (Spike)

**Testing mode: Spike** — Template cleanup so project builds cleanly with our new entities.

**Files:**
- Modify: `api/src/Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `api/src/Infrastructure/Data/ApplicationDbContext.cs`
- Delete: Template TodoItem/TodoList references across Application and Tests

**Step 1: Remove all template Todo-related code**

Delete the following files/directories if they exist:
- `api/src/Domain/Entities/TodoItem.cs`
- `api/src/Domain/Entities/TodoList.cs`
- `api/src/Domain/Enums/PriorityLevel.cs`
- `api/src/Domain/ValueObjects/Colour.cs`
- `api/src/Domain/Events/TodoItemCompletedEvent.cs`
- `api/src/Domain/Events/TodoItemCreatedEvent.cs`
- `api/src/Domain/Events/TodoItemDeletedEvent.cs`
- `api/src/Domain/Exceptions/UnsupportedColourException.cs`
- `api/src/Application/TodoItems/` (entire directory)
- `api/src/Application/TodoLists/` (entire directory)
- `api/src/Application/WeatherForecasts/` (entire directory)
- `api/src/Web/Endpoints/TodoItems.cs`
- `api/src/Web/Endpoints/TodoLists.cs`
- `api/src/Web/Endpoints/WeatherForecasts.cs`
- `api/src/Infrastructure/Data/Configurations/TodoItemConfiguration.cs`
- `api/src/Infrastructure/Data/Configurations/TodoListConfiguration.cs`
- `api/tests/Domain.UnitTests/ValueObjects/ColourTests.cs`
- `api/tests/Application.FunctionalTests/TodoItems/` (entire directory)
- `api/tests/Application.FunctionalTests/TodoLists/` (entire directory)

**Step 2: Update IApplicationDbContext**

```csharp
using api.Domain.Entities;

namespace api.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Recruitment> Recruitments { get; }
    DbSet<Candidate> Candidates { get; }
    DbSet<ImportSession> ImportSessions { get; }
    DbSet<AuditEntry> AuditEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

**Step 3: Update ApplicationDbContext** (minimal — full filter logic in Task 9)

```csharp
using System.Reflection;
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Recruitment> Recruitments => Set<Recruitment>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<ImportSession> ImportSessions => Set<ImportSession>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

**Step 4: Clean up any remaining template references**

Check for compilation errors and fix them. Key files that may reference template types:
- `api/tests/Application.FunctionalTests/Testing.cs` — may reference TodoLists
- `api/tests/Application.FunctionalTests/BaseTestFixture.cs` — may reference template
- `api/tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs` — may reference template types
- `api/src/Application/Common/Models/LookupDto.cs` — may reference template types

For each, either update or remove. The goal is `dotnet build` succeeds with zero errors.

**Step 5: Verify build**

Run: `cd api && dotnet build`
Expected: Build succeeds with zero errors.

**Step 6: Run all existing tests**

Run: `cd api && dotnet test tests/Domain.UnitTests/ --no-restore`
Expected: All tests pass (enum tests, value object tests, entity tests).

**Step 7: Commit**

```bash
git add -A api/
git commit -m "refactor(api): remove template Todo/Weather code and update DbContext for domain entities"
```

---

## Task 8: Create EF Core configurations (Spike)

**Testing mode: Spike** — Configuration correctness verified via migration generation and integration tests.

**Files:**
- Create: `api/src/Infrastructure/Data/Configurations/RecruitmentConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/WorkflowStepConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/CandidateDocumentConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/AuditEntryConfiguration.cs`

**Step 1: Create all EF Core configurations**

All use Fluent API only. Zero data annotations on domain entities.

`api/src/Infrastructure/Data/Configurations/RecruitmentConfiguration.cs`:
```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class RecruitmentConfiguration : IEntityTypeConfiguration<Recruitment>
{
    public void Configure(EntityTypeBuilder<Recruitment> builder)
    {
        builder.ToTable("Recruitments");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.JobRequisitionId)
            .HasMaxLength(100);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RecruitmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Members)
            .WithOne()
            .HasForeignKey(m => m.RecruitmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.DomainEvents);

        // Use backing field for collections
        builder.Navigation(r => r.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

`api/src/Infrastructure/Data/Configurations/WorkflowStepConfiguration.cs`:
```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.RecruitmentId)
            .HasDatabaseName("IX_WorkflowSteps_RecruitmentId");

        builder.HasIndex(s => new { s.RecruitmentId, s.Name })
            .IsUnique()
            .HasDatabaseName("UQ_WorkflowSteps_RecruitmentId_Name");

        builder.Ignore(s => s.DomainEvents);
    }
}
```

`api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs`:
```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class RecruitmentMemberConfiguration : IEntityTypeConfiguration<RecruitmentMember>
{
    public void Configure(EntityTypeBuilder<RecruitmentMember> builder)
    {
        builder.ToTable("RecruitmentMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_RecruitmentMembers_UserId");

        builder.HasIndex(m => new { m.RecruitmentId, m.UserId })
            .IsUnique()
            .HasDatabaseName("UQ_RecruitmentMembers_RecruitmentId_UserId");

        builder.Ignore(m => m.DomainEvents);
    }
}
```

`api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs`:
```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("Candidates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .HasMaxLength(254);

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(c => c.Location)
            .HasMaxLength(200);

        // Shadow navigation for global query filter (NOT on the domain entity)
        builder.HasOne<Recruitment>()
            .WithMany()
            .HasForeignKey(c => c.RecruitmentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(c => c.RecruitmentId)
            .HasDatabaseName("IX_Candidates_RecruitmentId");

        builder.HasIndex(c => new { c.RecruitmentId, c.Email })
            .IsUnique()
            .HasDatabaseName("UQ_Candidates_RecruitmentId_Email")
            .HasFilter("[Email] IS NOT NULL"); // Allow multiple anonymized candidates

        builder.HasMany(c => c.Outcomes)
            .WithOne()
            .HasForeignKey(o => o.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Documents)
            .WithOne()
            .HasForeignKey(d => d.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);

        builder.Navigation(c => c.Outcomes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

`api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs`:
```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class CandidateOutcomeConfiguration : IEntityTypeConfiguration<CandidateOutcome>
{
    public void Configure(EntityTypeBuilder<CandidateOutcome> builder)
    {
        builder.ToTable("CandidateOutcomes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(o => new { o.CandidateId, o.WorkflowStepId })
            .HasDatabaseName("IX_CandidateOutcomes_CandidateId_WorkflowStepId");

        builder.Ignore(o => o.DomainEvents);
    }
}
```

`api/src/Infrastructure/Data/Configurations/CandidateDocumentConfiguration.cs`:
```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class CandidateDocumentConfiguration : IEntityTypeConfiguration<CandidateDocument>
{
    public void Configure(EntityTypeBuilder<CandidateDocument> builder)
    {
        builder.ToTable("CandidateDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.BlobStorageUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.HasIndex(d => d.CandidateId)
            .HasDatabaseName("IX_CandidateDocuments_CandidateId");

        builder.Ignore(d => d.DomainEvents);
    }
}
```

`api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs`:
```csharp
using api.Domain.Entities;
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

        builder.Property(s => s.FailureReason)
            .HasMaxLength(2000);

        builder.HasIndex(s => s.RecruitmentId)
            .HasDatabaseName("IX_ImportSessions_RecruitmentId");

        builder.Ignore(s => s.DomainEvents);
    }
}
```

`api/src/Infrastructure/Data/Configurations/AuditEntryConfiguration.cs`:
```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ActionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Context)
            .HasMaxLength(4000);

        builder.HasIndex(a => new { a.RecruitmentId, a.PerformedAt })
            .HasDatabaseName("IX_AuditEntries_RecruitmentId_PerformedAt");

        builder.Ignore(a => a.DomainEvents);
    }
}
```

**Step 2: Delete template configurations**

Delete `api/src/Infrastructure/Data/Configurations/TodoItemConfiguration.cs` and `TodoListConfiguration.cs`.

**Step 3: Verify build**

Run: `cd api && dotnet build`
Expected: Build succeeds.

**Step 4: Commit**

```bash
git add api/src/Infrastructure/Data/Configurations/
git commit -m "feat(infra): add EF Core Fluent API configurations for all domain entities"
```

---

## Task 9: Configure global query filters and TenantContext middleware (Test-first)

**Testing mode: Test-first** — Global query filter is the security boundary.

**Files:**
- Modify: `api/src/Infrastructure/Data/ApplicationDbContext.cs` (add ITenantContext + filters)
- Create: `api/src/Web/Middleware/TenantContextMiddleware.cs`
- Modify: `api/src/Web/Program.cs` (register middleware)

**Step 1: Update ApplicationDbContext with global query filters**

The filter requires a shadow navigation from Candidate to Recruitment (for membership check). This is configured in `CandidateConfiguration` already. The DbContext uses `ITenantContext` fields directly in the filter expression.

```csharp
using System.Reflection;
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Recruitment> Recruitments => Set<Recruitment>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<ImportSession> ImportSessions => Set<ImportSession>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter on Candidate — the security boundary
        builder.Entity<Candidate>().HasQueryFilter(c =>
            // Service context bypasses all filters (GDPR job)
            _tenantContext.IsServiceContext ||
            // Import service scoped to specific recruitment
            (_tenantContext.RecruitmentId != null && c.RecruitmentId == _tenantContext.RecruitmentId) ||
            // Web user: only candidates in recruitments where user is a member
            (_tenantContext.UserId != null &&
             EF.Property<Recruitment>(c, "Recruitment").Members
                .Any(m => m.UserId.ToString() == _tenantContext.UserId))
        );
    }
}
```

**NOTE:** The filter uses `EF.Property<Recruitment>(c, "Recruitment")` to access the shadow navigation from Candidate to Recruitment. The shadow navigation was configured in `CandidateConfiguration` via `builder.HasOne<Recruitment>().WithMany().HasForeignKey(c => c.RecruitmentId)`. `m.UserId.ToString()` converts Guid to string to compare with `ITenantContext.UserId` which is `string?`.

If `EF.Property` doesn't work for this pattern, an alternative is to add a navigation property name in the configuration and reference it. The implementation will need to verify this compiles and works at runtime.

**Step 2: Create TenantContextMiddleware**

`api/src/Web/Middleware/TenantContextMiddleware.cs`:
```csharp
using System.Security.Claims;
using api.Application.Common.Interfaces;

namespace api.Web.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // ITenantContext.UserId is already populated via ICurrentUserService (lazy from DI).
        // This middleware ensures the context is resolved early in the pipeline.
        // No additional action needed — the scoped DI resolution handles it.
        await _next(context);
    }
}
```

**Step 3: Register middleware in Program.cs**

Add after `app.UseAuthorization()` and before `app.UseMiddleware<NoindexMiddleware>()`:

```csharp
app.UseMiddleware<TenantContextMiddleware>();
```

**Step 4: Verify build**

Run: `cd api && dotnet build`
Expected: Build succeeds. The global query filter compiles.

**Step 5: Commit**

```bash
git add api/src/Infrastructure/Data/ApplicationDbContext.cs api/src/Web/Middleware/TenantContextMiddleware.cs api/src/Web/Program.cs
git commit -m "feat(infra): add global query filters for tenant isolation and TenantContext middleware"
```

---

## Task 10: Create AuditBehaviour pipeline (Test-first)

**Testing mode: Test-first** — Cross-cutting audit concern must be reliable.

**Files:**
- Create: `api/src/Application/Common/Behaviours/AuditBehaviour.cs`
- Create: `api/tests/Application.UnitTests/Common/Behaviours/AuditBehaviourTests.cs`
- Modify: `api/src/Application/DependencyInjection.cs` (register behaviour)

**Step 1: Write failing tests for AuditBehaviour**

Create `api/tests/Application.UnitTests/Common/Behaviours/AuditBehaviourTests.cs`:

```csharp
using api.Application.Common.Behaviours;
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Common.Behaviours;

// Test command/query types
public record TestCommand : IRequest<string>;
public record TestQuery : IRequest<string>;

public class AuditBehaviourTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.UserId.Returns("user-123");

        // Mock DbSet<AuditEntry>
        var auditEntries = Substitute.For<DbSet<AuditEntry>>();
        _dbContext.AuditEntries.Returns(auditEntries);
    }

    [Test]
    public async Task Handle_Command_CreatesAuditEntry()
    {
        var behaviour = new AuditBehaviour<TestCommand, string>(_dbContext, _tenantContext);
        var request = new TestCommand();

        await behaviour.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        _dbContext.AuditEntries.Received(1).Add(Arg.Any<AuditEntry>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_Query_DoesNotCreateAuditEntry()
    {
        var behaviour = new AuditBehaviour<TestQuery, string>(_dbContext, _tenantContext);
        var request = new TestQuery();

        await behaviour.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        _dbContext.AuditEntries.DidNotReceive().Add(Arg.Any<AuditEntry>());
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `cd api && dotnet build tests/Application.UnitTests/`
Expected: Compilation error — AuditBehaviour doesn't exist.

**Step 3: Create AuditBehaviour**

`api/src/Application/Common/Behaviours/AuditBehaviour.cs`:
```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;

namespace api.Application.Common.Behaviours;

public class AuditBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public AuditBehaviour(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Only audit commands (convention: class name ends with "Command")
        if (!IsCommand(request))
        {
            return response;
        }

        var userId = Guid.TryParse(_tenantContext.UserId, out var parsed) ? parsed : Guid.Empty;

        var auditEntry = AuditEntry.Create(
            recruitmentId: Guid.Empty, // Set by specific command handlers in future stories
            entityId: null,
            entityType: typeof(TRequest).Name.Replace("Command", ""),
            actionType: "Command",
            performedBy: userId,
            context: null); // No PII — context populated by specific handlers

        _dbContext.AuditEntries.Add(auditEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static bool IsCommand(TRequest request)
    {
        return typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal);
    }
}
```

**Step 4: Register AuditBehaviour in DI**

Add to `api/src/Application/DependencyInjection.cs` after `ValidationBehaviour`:

```csharp
cfg.AddOpenBehavior(typeof(AuditBehaviour<,>));
```

**Step 5: Verify build and tests**

Run: `cd api && dotnet build && dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~AuditBehaviourTests" --no-restore`
Expected: Both tests pass.

**Step 6: Commit**

```bash
git add api/src/Application/Common/Behaviours/AuditBehaviour.cs api/src/Application/DependencyInjection.cs api/tests/Application.UnitTests/Common/Behaviours/AuditBehaviourTests.cs
git commit -m "feat(app): add AuditBehaviour MediatR pipeline for command audit trail"
```

---

## Task 11: Write integration test infrastructure for tenant isolation (Test-first)

**Testing mode: Test-first** — Security isolation tests define the contract.

**IMPORTANT:** These tests require Testcontainers with SQL Server and a full ASP.NET Core runtime. They will NOT run locally (no ASP.NET Core runtime installed). They must compile correctly and will be validated in CI.

**Files:**
- Modify: `api/tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj` (add references)
- Create: `api/tests/Infrastructure.IntegrationTests/Data/TenantContextFilterTests.cs`

**Step 1: Update Infrastructure.IntegrationTests project references**

Add project references and Testcontainers package to the csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <RootNamespace>api.Infrastructure.IntegrationTests</RootNamespace>
        <AssemblyName>api.Infrastructure.IntegrationTests</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="NUnit" />
        <PackageReference Include="NUnit3TestAdapter" />
        <PackageReference Include="NUnit.Analyzers">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="coverlet.collector">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="FluentAssertions" />
        <PackageReference Include="NSubstitute" />
        <PackageReference Include="Testcontainers.MsSql" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Infrastructure\Infrastructure.csproj" />
    </ItemGroup>

</Project>
```

**NOTE:** The `Testcontainers.MsSql` and `Microsoft.EntityFrameworkCore.SqlServer` packages need to be added to `api/Directory.Packages.props` if not already present. Check and add versions as needed.

**Step 2: Create TenantContextFilterTests**

Create `api/tests/Infrastructure.IntegrationTests/Data/TenantContextFilterTests.cs`:

```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using Testcontainers.MsSql;

namespace api.Infrastructure.IntegrationTests.Data;

[TestFixture]
public class TenantContextFilterTests
{
    private MsSqlContainer _sqlContainer = null!;
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _sqlContainer.StartAsync();
        _connectionString = _sqlContainer.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _sqlContainer.DisposeAsync();
    }

    private ApplicationDbContext CreateDbContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new ApplicationDbContext(options, tenantContext);
    }

    private async Task SeedDatabase()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsServiceContext.Returns(true); // Bypass filter for seeding

        using var ctx = CreateDbContext(tenantContext);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();

        // Create Recruitment A with User A as member
        var recruitmentA = Recruitment.Create("Recruitment A", null, Guid.Parse("aaaa0000-0000-0000-0000-000000000001"));
        ctx.Recruitments.Add(recruitmentA);

        // Create Recruitment B with User B as member
        var recruitmentB = Recruitment.Create("Recruitment B", null, Guid.Parse("bbbb0000-0000-0000-0000-000000000002"));
        ctx.Recruitments.Add(recruitmentB);

        await ctx.SaveChangesAsync(CancellationToken.None);

        // Add candidates to each recruitment (bypass filter via service context)
        var candidateA = Candidate.Create(recruitmentA.Id, "Alice", "alice@a.com", null, null, DateTimeOffset.UtcNow);
        var candidateB = Candidate.Create(recruitmentB.Id, "Bob", "bob@b.com", null, null, DateTimeOffset.UtcNow);
        ctx.Candidates.Add(candidateA);
        ctx.Candidates.Add(candidateB);

        await ctx.SaveChangesAsync(CancellationToken.None);
    }

    [SetUp]
    public async Task SetUp()
    {
        await SeedDatabase();
    }

    [Test]
    public async Task UserInRecruitmentA_CannotSeeCandidatesFromRecruitmentB()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.UserId.Returns("aaaa0000-0000-0000-0000-000000000001");
        tenantContext.IsServiceContext.Returns(false);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().OnlyContain(c => c.Email == "alice@a.com");
    }

    [Test]
    public async Task ImportService_ScopedToRecruitment()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        var serviceCtx = Substitute.For<ITenantContext>();
        serviceCtx.IsServiceContext.Returns(true);

        // Get recruitment A's ID
        using var seedCtx = CreateDbContext(serviceCtx);
        var recruitmentA = await seedCtx.Recruitments.FirstAsync(r => r.Title == "Recruitment A");

        tenantContext.RecruitmentId.Returns(recruitmentA.Id);
        tenantContext.IsServiceContext.Returns(false);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().OnlyContain(c => c.Email == "alice@a.com");
    }

    [Test]
    public async Task GdprService_CanQueryAllRecruitments()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsServiceContext.Returns(true);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().HaveCount(2);
    }

    [Test]
    public async Task MisconfiguredContext_ReturnsZeroResults()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.UserId.Returns((string?)null);
        tenantContext.RecruitmentId.Returns((Guid?)null);
        tenantContext.IsServiceContext.Returns(false);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().BeEmpty();
    }
}
```

**Step 3: Add missing packages to Directory.Packages.props**

Check `api/Directory.Packages.props` and add if missing:
- `Testcontainers.MsSql` (version: `4.3.0` or latest)

**Step 4: Verify build**

Run: `cd api && dotnet build tests/Infrastructure.IntegrationTests/`
Expected: Build succeeds. Tests compile but will not execute locally.

**Step 5: Commit**

```bash
git add api/tests/Infrastructure.IntegrationTests/ api/Directory.Packages.props
git commit -m "test(infra): add Testcontainers-based tenant isolation integration tests"
```

---

## Task 12: Full verification

**Step 1: Verify backend build**

Run: `cd api && dotnet build`
Expected: Build succeeds, 0 warnings, 0 errors.

**Step 2: Run all Domain.UnitTests**

Run: `cd api && dotnet test tests/Domain.UnitTests/ --no-restore`
Expected: All tests pass (enum tests + value object tests + entity tests).

**Step 3: Run backend format check**

Run: `cd api && dotnet format --verify-no-changes`
Expected: No formatting issues.

**Step 4: Verify frontend still works (no changes expected)**

Run: `cd web && npx vitest run && npm run lint && npm run format:check && npx tsc -b`
Expected: All 19 tests pass, lint clean, format clean, TypeScript build clean.

**Step 5: AC checklist**

- [ ] AC1 (Domain entities): All 8 entity types created. Aggregate boundaries enforced (child entities via root methods only). Cross-aggregate refs are IDs only.
- [ ] AC2 (EF Core config): 8 Fluent API configuration files. Zero data annotations. Global query filters via ITenantContext.
- [ ] AC3 (TenantContext middleware): TenantContextMiddleware registered in pipeline.
- [ ] AC4 (Cross-recruitment isolation): Integration tests verify User A sees only Recruitment A candidates.
- [ ] AC5 (Import service scoping): Integration test verifies RecruitmentId scoping.
- [ ] AC6 (GDPR service bypass): Integration test verifies IsServiceContext bypasses filter.
- [ ] AC7 (Default empty context safety): Integration test verifies zero results with no context.
- [ ] AC8 (Audit pipeline): AuditBehaviour registered in MediatR pipeline. Unit tests verify command/query distinction.

**Step 6: Commit verification results and update sprint status**

```bash
# Update sprint-status.yaml: 1-3-core-data-model-tenant-isolation -> done
git add _bmad-output/implementation-artifacts/sprint-status.yaml
git commit -m "chore: mark Story 1.3 Core Data Model & Tenant Isolation as done"
```

---

## Summary

| Task | Testing Mode | Key Files |
|------|-------------|-----------|
| 1. GuidEntity base | Spike | `GuidEntity.cs` |
| 2. Domain enums | Test-first | `OutcomeStatus.cs`, `ImportMatchConfidence.cs`, `RecruitmentStatus.cs`, `ImportSessionStatus.cs` |
| 3. Value objects | Test-first | `CandidateMatch.cs`, `AnonymizationResult.cs` |
| 4. Events + exceptions | Spike | 6 events, 5 exceptions |
| 5. Entity tests | Test-first | `RecruitmentTests.cs`, `CandidateTests.cs`, `ImportSessionTests.cs` |
| 6. Entity implementation | Test-first (pass) | 8 entities |
| 7. Template cleanup | Spike | Remove Todo/Weather, update DbContext |
| 8. EF Core configs | Spike | 8 configuration files |
| 9. Global query filters | Test-first | `ApplicationDbContext.cs`, `TenantContextMiddleware.cs` |
| 10. AuditBehaviour | Test-first | `AuditBehaviour.cs`, `AuditBehaviourTests.cs` |
| 11. Integration tests | Test-first | `TenantContextFilterTests.cs` |
| 12. Full verification | N/A | All |

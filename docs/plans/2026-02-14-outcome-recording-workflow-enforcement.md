# Outcome Recording & Workflow Enforcement Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable users to record Pass/Fail/Hold outcomes for candidates at their current workflow step, with auto-advance on Pass, re-recording support, and outcome history display.

**Architecture:** Full-stack CQRS. Domain logic in Candidate aggregate (RecordOutcome with workflow enforcement). Backend exposes ScreeningEndpoints (POST outcome, GET history). Frontend adds OutcomeForm and OutcomeHistory components in `features/screening/`.

**Tech Stack:** .NET 10, EF Core 10, MediatR 13, FluentValidation, NUnit/NSubstitute/FluentAssertions (backend); React 19, TypeScript, TanStack Query 5, Tailwind 4, Vitest/Testing Library/MSW (frontend).

---

### Task 1: Extend CandidateOutcome entity with Reason property

**TDD Mode:** Test-first (domain entity change)

**Files:**
- Modify: `api/src/Domain/Entities/CandidateOutcome.cs`
- Modify: `api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs`

**Step 1: Add Reason property to CandidateOutcome**

Add `Reason` (string?) property and extend the `Create` factory to accept it with a default of `null`:

```csharp
// api/src/Domain/Entities/CandidateOutcome.cs
public string? Reason { get; private set; }

// Extend Create factory — add `string? reason = null` parameter
internal static CandidateOutcome Create(
    Guid candidateId, Guid workflowStepId, OutcomeStatus status,
    Guid recordedByUserId, string? reason = null)
{
    return new CandidateOutcome
    {
        CandidateId = candidateId,
        WorkflowStepId = workflowStepId,
        Status = status,
        Reason = reason,
        RecordedAt = DateTimeOffset.UtcNow,
        RecordedByUserId = recordedByUserId,
    };
}
```

The default `reason = null` preserves backward compatibility with existing callers (`Candidate.RecordOutcome` and `CreateCandidateCommandHandler` which pass no reason).

**Step 2: Add EF Core configuration for Reason**

```csharp
// api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs
// Add inside Configure():
builder.Property(o => o.Reason).HasMaxLength(500);
```

**Step 3: Verify build passes**

Run: `dotnet build api/src/Domain/ && dotnet build api/src/Infrastructure/`
Expected: Build success (no breaking changes due to default parameter).

**Step 4: Commit**

```bash
git add api/src/Domain/Entities/CandidateOutcome.cs api/src/Infrastructure/Data/Configurations/CandidateOutcomeConfiguration.cs
git commit -m "feat(4.3): extend CandidateOutcome with Reason property + EF config"
```

---

### Task 2: Enhance Candidate entity with workflow enforcement

**TDD Mode:** Test-first (core domain logic)

**Files:**
- Modify: `api/src/Domain/Entities/Candidate.cs`
- Modify: `api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs`
- Modify: `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`

**Step 1: Write domain tests for enhanced RecordOutcome**

Add tests to `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`. These tests need a helper that creates a candidate with `CurrentWorkflowStepId` set and workflow steps available. Since `CurrentWorkflowStepId` is a new property with private setter, we need `AssignToWorkflowStep()`.

Add these test methods (all should FAIL initially because the enhanced domain logic doesn't exist yet):

```csharp
// Helper: create workflow steps for testing
private static List<WorkflowStep> CreateOrderedSteps(Guid recruitmentId)
{
    // Use reflection or internal Create to build steps with known IDs
    var step1 = WorkflowStep.Create(recruitmentId, "Screening", 1);
    var step2 = WorkflowStep.Create(recruitmentId, "Interview", 2);
    var step3 = WorkflowStep.Create(recruitmentId, "Final", 3);
    return [step1, step2, step3];
}
```

NOTE: `WorkflowStep.Create` is `internal` — the Domain.UnitTests project must have `InternalsVisibleTo` or the tests need to access it. Check `api/src/Domain/` for `InternalsVisibleTo` attribute. If not present, use `Recruitment.AddStep()` and extract steps from `recruitment.Steps`.

Better approach: use `Recruitment` to create steps naturally:

```csharp
private static (Candidate candidate, Recruitment recruitment) CreateCandidateWithWorkflow()
{
    var userId = Guid.NewGuid();
    var recruitment = Recruitment.Create("Test", null, userId);
    recruitment.AddStep("Screening", 1);
    recruitment.AddStep("Interview", 2);
    recruitment.AddStep("Final", 3);
    var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();

    var candidate = Candidate.Create(
        recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
    candidate.AssignToWorkflowStep(orderedSteps[0].Id);

    return (candidate, recruitment);
}
```

Test list:

```csharp
[Test]
public void RecordOutcome_ValidStep_CreatesOutcomeWithReason()
{
    var (candidate, recruitment) = CreateCandidateWithWorkflow();
    var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

    candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), "Strong candidate", steps);

    candidate.Outcomes.Should().HaveCount(1);
    var outcome = candidate.Outcomes.First();
    outcome.WorkflowStepId.Should().Be(steps[0].Id);
    outcome.Status.Should().Be(OutcomeStatus.Pass);
    outcome.Reason.Should().Be("Strong candidate");
}

[Test]
public void RecordOutcome_WrongStep_ThrowsInvalidWorkflowTransitionException()
{
    var (candidate, recruitment) = CreateCandidateWithWorkflow();
    var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
    // Candidate is at step[0], try to record at step[2]

    var act = () => candidate.RecordOutcome(steps[2].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

    act.Should().Throw<InvalidWorkflowTransitionException>();
}

[Test]
public void RecordOutcome_PassNotLastStep_AdvancesToNextStep()
{
    var (candidate, recruitment) = CreateCandidateWithWorkflow();
    var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

    candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

    candidate.CurrentWorkflowStepId.Should().Be(steps[1].Id);
    candidate.IsCompleted.Should().BeFalse();
}

[Test]
public void RecordOutcome_PassLastStep_SetsIsCompleted()
{
    var (candidate, recruitment) = CreateCandidateWithWorkflow();
    var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
    // Advance to last step
    candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);
    candidate.RecordOutcome(steps[1].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

    candidate.RecordOutcome(steps[2].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

    candidate.IsCompleted.Should().BeTrue();
    candidate.CurrentWorkflowStepId.Should().Be(steps[2].Id); // stays at last step
}

[Test]
public void RecordOutcome_FailOrHold_StaysAtCurrentStep()
{
    var (candidate, recruitment) = CreateCandidateWithWorkflow();
    var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
    var originalStepId = candidate.CurrentWorkflowStepId;

    candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Fail, Guid.NewGuid(), "Not qualified", steps);

    candidate.CurrentWorkflowStepId.Should().Be(originalStepId);
    candidate.IsCompleted.Should().BeFalse();
}

[Test]
public void RecordOutcome_ReRecord_ReplacesExistingOutcome()
{
    var (candidate, recruitment) = CreateCandidateWithWorkflow();
    var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
    var userId = Guid.NewGuid();
    candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Fail, userId, "Initial", steps);

    candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, userId, "Reconsidered", steps);

    candidate.Outcomes.Where(o => o.WorkflowStepId == steps[0].Id).Should().HaveCount(1);
    candidate.Outcomes.First(o => o.WorkflowStepId == steps[0].Id).Status.Should().Be(OutcomeStatus.Pass);
    candidate.Outcomes.First(o => o.WorkflowStepId == steps[0].Id).Reason.Should().Be("Reconsidered");
}

[Test]
public void RecordOutcome_NoCurrentStep_ThrowsInvalidOperationException()
{
    var candidate = Candidate.Create(
        Guid.NewGuid(), "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
    // No AssignToWorkflowStep called

    var act = () => candidate.RecordOutcome(Guid.NewGuid(), OutcomeStatus.Pass, Guid.NewGuid(), null, new List<WorkflowStep>());

    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*not been assigned*");
}

[Test]
public void RecordOutcome_RaisesOutcomeRecordedEvent_WithEnhancedMethod()
{
    var (candidate, recruitment) = CreateCandidateWithWorkflow();
    var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
    candidate.ClearDomainEvents();

    candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, Guid.NewGuid(), null, steps);

    candidate.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<OutcomeRecordedEvent>();
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "ClassName~CandidateTests"`
Expected: New tests FAIL (methods/properties don't exist yet).

**Step 3: Implement enhanced Candidate entity**

Modify `api/src/Domain/Entities/Candidate.cs`:

1. Add new properties:
```csharp
public Guid? CurrentWorkflowStepId { get; private set; }
public bool IsCompleted { get; private set; }
```

2. Add `AssignToWorkflowStep` method:
```csharp
public void AssignToWorkflowStep(Guid firstStepId)
{
    CurrentWorkflowStepId = firstStepId;
}
```

3. Replace the existing `RecordOutcome` method (lines 48-53) with the enhanced version. Keep the old 3-parameter overload working by delegating to the new method, OR update all callers. The existing callers are:
   - `CreateCandidateCommandHandler` (line 56): `candidate.RecordOutcome(firstStep.Id, OutcomeStatus.NotStarted, userId.Value)` — This sets the initial "NotStarted" outcome. After this story, initial placement uses `AssignToWorkflowStep()` instead, but the old call still needs to work for backward compatibility. Best approach: keep a backward-compatible overload for NotStarted placement.
   - `CandidateTests.cs` existing tests: Use the simple overload.

Actually, looking at `CreateCandidateCommandHandler` line 56, it calls `RecordOutcome(firstStep.Id, OutcomeStatus.NotStarted, userId.Value)` to place the candidate. This is the "initial placement" scenario. With the new workflow enforcement, this would fail because `CurrentWorkflowStepId` is null.

**Decision:** Keep the old simple `RecordOutcome` for backward compatibility (it does NOT enforce workflow, used only for initial NotStarted placement). Add the new enhanced overload with 5 parameters that DOES enforce workflow. Update `CreateCandidateCommandHandler` to also call `AssignToWorkflowStep()` before or instead of the RecordOutcome call.

Better yet: The existing `CreateCandidateCommandHandler` should call `candidate.AssignToWorkflowStep(firstStep.Id)` instead of recording a NotStarted outcome. But that's a change to another handler and may break tests. Safer: keep old overload as-is, add new overload.

```csharp
// KEEP existing simple overload (for initial placement / backward compat)
public void RecordOutcome(Guid workflowStepId, OutcomeStatus status, Guid recordedByUserId)
{
    var outcome = CandidateOutcome.Create(Id, workflowStepId, status, recordedByUserId);
    _outcomes.Add(outcome);
    AddDomainEvent(new OutcomeRecordedEvent(Id, workflowStepId));
}

// NEW: Enhanced overload with workflow enforcement
public void RecordOutcome(
    Guid workflowStepId,
    OutcomeStatus status,
    Guid recordedByUserId,
    string? reason,
    IReadOnlyList<WorkflowStep> orderedSteps)
{
    if (CurrentWorkflowStepId is null)
        throw new InvalidOperationException("Candidate has not been assigned to a workflow step.");

    if (workflowStepId != CurrentWorkflowStepId)
        throw new InvalidWorkflowTransitionException(
            CurrentWorkflowStepId.ToString()!,
            workflowStepId.ToString());

    // Remove existing outcome at this step (re-record support)
    var existing = _outcomes.FirstOrDefault(o => o.WorkflowStepId == workflowStepId);
    if (existing is not null)
        _outcomes.Remove(existing);

    var outcome = CandidateOutcome.Create(Id, workflowStepId, status, recordedByUserId, reason);
    _outcomes.Add(outcome);

    // Handle advancement on Pass
    if (status == OutcomeStatus.Pass)
    {
        var currentStep = orderedSteps.First(s => s.Id == workflowStepId);
        var nextStep = orderedSteps
            .Where(s => s.Order > currentStep.Order)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        if (nextStep is not null)
        {
            CurrentWorkflowStepId = nextStep.Id;
        }
        else
        {
            IsCompleted = true;
        }
    }

    AddDomainEvent(new OutcomeRecordedEvent(Id, workflowStepId));
}
```

4. Update EF configuration for new properties:

```csharp
// api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs
builder.Property(c => c.CurrentWorkflowStepId);
builder.Property(c => c.IsCompleted).HasDefaultValue(false);
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "ClassName~CandidateTests"`
Expected: ALL tests PASS (old tests still work with simple overload, new tests use enhanced overload).

**Step 5: Commit**

```bash
git add api/src/Domain/Entities/Candidate.cs api/src/Infrastructure/Data/Configurations/CandidateConfiguration.cs api/tests/Domain.UnitTests/Entities/CandidateTests.cs
git commit -m "feat(4.3): enhance Candidate.RecordOutcome with workflow enforcement + domain tests"
```

---

### Task 3: Add InvalidWorkflowTransitionException to CustomExceptionHandler

**TDD Mode:** Characterization (infrastructure wiring)

**Files:**
- Modify: `api/src/Web/Infrastructure/CustomExceptionHandler.cs`

**Step 1: Register exception handler**

The `InvalidWorkflowTransitionException` is NOT currently registered in `CustomExceptionHandler`. Add it to map to 400 Bad Request:

```csharp
// In the constructor's _exceptionHandlers dictionary, add:
{ typeof(InvalidWorkflowTransitionException), HandleInvalidWorkflowTransitionException },

// Add handler method:
private async Task HandleInvalidWorkflowTransitionException(HttpContext httpContext, Exception ex)
{
    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Invalid workflow transition",
        Detail = ex.Message,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    });
}
```

**Step 2: Verify build**

Run: `dotnet build api/src/Web/`
Expected: Build success.

**Step 3: Commit**

```bash
git add api/src/Web/Infrastructure/CustomExceptionHandler.cs
git commit -m "feat(4.3): register InvalidWorkflowTransitionException in exception handler"
```

---

### Task 4: Create RecordOutcomeCommand with handler, validator, and DTO

**TDD Mode:** Test-first (command handler with security checks)

**Files:**
- Create: `api/src/Application/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommand.cs`
- Create: `api/src/Application/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommandValidator.cs`
- Create: `api/src/Application/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommandHandler.cs`
- Create: `api/src/Application/Features/Screening/Commands/RecordOutcome/OutcomeResultDto.cs`
- Create: `api/tests/Application.UnitTests/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommandHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommandValidatorTests.cs`

**Step 1: Write validator tests**

```csharp
// api/tests/Application.UnitTests/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommandValidatorTests.cs
[TestFixture]
public class RecordOutcomeCommandValidatorTests
{
    private RecordOutcomeCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RecordOutcomeCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_Passes()
    {
        var command = new RecordOutcomeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, "Good candidate");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_NotStartedOutcome_Fails()
    {
        var command = new RecordOutcomeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.NotStarted, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Outcome");
    }

    [Test]
    public void Validate_EmptyRecruitmentId_Fails()
    {
        var command = new RecordOutcomeCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecruitmentId");
    }

    [Test]
    public void Validate_EmptyCandidateId_Fails()
    {
        var command = new RecordOutcomeCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), OutcomeStatus.Pass, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_EmptyWorkflowStepId_Fails()
    {
        var command = new RecordOutcomeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, OutcomeStatus.Pass, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ReasonExceeds500Chars_Fails()
    {
        var command = new RecordOutcomeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, new string('A', 501));
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Test]
    public void Validate_NullReason_Passes()
    {
        var command = new RecordOutcomeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Hold, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
```

**Step 2: Write handler tests**

```csharp
// api/tests/Application.UnitTests/Features/Screening/Commands/RecordOutcome/RecordOutcomeCommandHandlerTests.cs
[TestFixture]
public class RecordOutcomeCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
    }

    // Helper to set up a recruitment with steps and a candidate at step 1
    private (Recruitment recruitment, Candidate candidate, List<WorkflowStep> orderedSteps) SetupTestData(Guid userId)
    {
        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        candidate.AssignToWorkflowStep(orderedSteps[0].Id);

        return (recruitment, candidate, orderedSteps);
    }

    [Test]
    public async Task Handle_ValidOutcome_RecordsAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var (recruitment, candidate, steps) = SetupTestData(userId);

        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());
        _dbContext.Candidates.Returns(new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet());

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(recruitment.Id, candidate.Id, steps[0].Id, OutcomeStatus.Pass, "Good");

        var result = await handler.Handle(command, CancellationToken.None);

        result.CandidateId.Should().Be(candidate.Id);
        result.Outcome.Should().Be(OutcomeStatus.Pass);
        result.Reason.Should().Be("Good");
        result.NewCurrentStepId.Should().Be(steps[1].Id); // advanced
        result.IsCompleted.Should().BeFalse();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ClosedRecruitment_ThrowsRecruitmentClosedException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var (recruitment, candidate, steps) = SetupTestData(userId);
        recruitment.Close();

        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(recruitment.Id, candidate.Id, steps[0].Id, OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }

    [Test]
    public async Task Handle_NonMemberUser_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);
        var (recruitment, candidate, steps) = SetupTestData(creatorId);

        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(recruitment.Id, candidate.Id, steps[0].Id, OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        _dbContext.Recruitments.Returns(new List<Recruitment>().AsQueryable().BuildMockDbSet());

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_StepNotInRecruitment_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var (recruitment, candidate, _) = SetupTestData(userId);

        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(recruitment.Id, candidate.Id, Guid.NewGuid(), OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_CandidateNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var (recruitment, _, steps) = SetupTestData(userId);

        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());
        _dbContext.Candidates.Returns(new List<Candidate>().AsQueryable().BuildMockDbSet());

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(recruitment.Id, Guid.NewGuid(), steps[0].Id, OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

**Step 3: Run tests to verify they fail**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "ClassName~RecordOutcomeCommand"`
Expected: FAIL (classes don't exist yet).

**Step 4: Implement command, validator, handler, and DTO**

Create `RecordOutcomeCommand.cs`:
```csharp
public record RecordOutcomeCommand(
    Guid RecruitmentId,
    Guid CandidateId,
    Guid WorkflowStepId,
    OutcomeStatus Outcome,
    string? Reason
) : IRequest<OutcomeResultDto>;
```

Create `RecordOutcomeCommandValidator.cs`:
```csharp
public class RecordOutcomeCommandValidator : AbstractValidator<RecordOutcomeCommand>
{
    public RecordOutcomeCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.WorkflowStepId).NotEmpty();
        RuleFor(x => x.Outcome)
            .IsInEnum()
            .Must(o => o != OutcomeStatus.NotStarted)
            .WithMessage("Outcome must be Pass, Fail, or Hold.");
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => x.Reason is not null);
    }
}
```

Create `RecordOutcomeCommandHandler.cs`:
```csharp
public class RecordOutcomeCommandHandler(
    IApplicationDbContext context,
    ITenantContext tenantContext)
    : IRequestHandler<RecordOutcomeCommand, OutcomeResultDto>
{
    public async Task<OutcomeResultDto> Handle(RecordOutcomeCommand request, CancellationToken ct)
    {
        var recruitment = await context.Recruitments
            .Include(r => r.Members)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        if (!recruitment.Members.Any(m => m.UserId == tenantContext.UserGuid))
            throw new ForbiddenAccessException();

        if (recruitment.Status == RecruitmentStatus.Closed)
            throw new RecruitmentClosedException(recruitment.Id);

        if (!recruitment.Steps.Any(s => s.Id == request.WorkflowStepId))
            throw new NotFoundException(nameof(WorkflowStep), request.WorkflowStepId);

        var candidate = await context.Candidates
            .Include(c => c.Outcomes)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        candidate.RecordOutcome(
            request.WorkflowStepId,
            request.Outcome,
            tenantContext.UserGuid!.Value,
            request.Reason,
            orderedSteps);

        await context.SaveChangesAsync(ct);

        return OutcomeResultDto.From(candidate, request.WorkflowStepId);
    }
}
```

Create `OutcomeResultDto.cs`:
```csharp
public record OutcomeResultDto
{
    public Guid OutcomeId { get; init; }
    public Guid CandidateId { get; init; }
    public Guid WorkflowStepId { get; init; }
    public OutcomeStatus Outcome { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedBy { get; init; }
    public Guid? NewCurrentStepId { get; init; }
    public bool IsCompleted { get; init; }

    public static OutcomeResultDto From(Candidate candidate, Guid stepId)
    {
        var outcome = candidate.Outcomes.First(o => o.WorkflowStepId == stepId);
        return new OutcomeResultDto
        {
            OutcomeId = outcome.Id,
            CandidateId = candidate.Id,
            WorkflowStepId = stepId,
            Outcome = outcome.Status,
            Reason = outcome.Reason,
            RecordedAt = outcome.RecordedAt,
            RecordedBy = outcome.RecordedByUserId,
            NewCurrentStepId = candidate.CurrentWorkflowStepId,
            IsCompleted = candidate.IsCompleted,
        };
    }
}
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "ClassName~RecordOutcomeCommand"`
Expected: ALL PASS.

**Step 6: Commit**

```bash
git add api/src/Application/Features/Screening/ api/tests/Application.UnitTests/Features/Screening/
git commit -m "feat(4.3): add RecordOutcomeCommand with handler + validator + unit tests"
```

---

### Task 5: Create GetCandidateOutcomeHistoryQuery with handler

**TDD Mode:** Test-first (query handler with authorization)

**Files:**
- Create: `api/src/Application/Features/Screening/Queries/GetCandidateOutcomeHistory/GetCandidateOutcomeHistoryQuery.cs`
- Create: `api/src/Application/Features/Screening/Queries/GetCandidateOutcomeHistory/GetCandidateOutcomeHistoryQueryHandler.cs`
- Create: `api/src/Application/Features/Screening/Queries/GetCandidateOutcomeHistory/OutcomeHistoryDto.cs`
- Create: `api/tests/Application.UnitTests/Features/Screening/Queries/GetCandidateOutcomeHistory/GetCandidateOutcomeHistoryQueryHandlerTests.cs`

**Step 1: Write handler tests**

```csharp
[TestFixture]
public class GetCandidateOutcomeHistoryQueryHandlerTests
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
    public async Task Handle_ValidRequest_ReturnsOrderedHistory()
    {
        // Setup recruitment with 2 steps, candidate with outcome at step 1
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate = Candidate.Create(recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        candidate.AssignToWorkflowStep(steps[0].Id);
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, userId, "Good", steps);

        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());
        _dbContext.Candidates.Returns(new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet());

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, candidate.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].WorkflowStepName.Should().Be("Screening");
        result[0].Outcome.Should().Be(OutcomeStatus.Pass);
        result[0].Reason.Should().Be("Good");
    }

    [Test]
    public async Task Handle_NonMemberUser_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(Guid.NewGuid()); // non-member
        var recruitment = Recruitment.Create("Test", null, creatorId);
        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, Guid.NewGuid());

        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_CandidateNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var recruitment = Recruitment.Create("Test", null, userId);
        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());
        _dbContext.Candidates.Returns(new List<Candidate>().AsQueryable().BuildMockDbSet());

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, Guid.NewGuid());

        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NoOutcomes_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var recruitment = Recruitment.Create("Test", null, userId);
        var candidate = Candidate.Create(recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        _dbContext.Recruitments.Returns(new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet());
        _dbContext.Candidates.Returns(new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet());

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, candidate.Id);

        var result = await handler.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "ClassName~GetCandidateOutcomeHistory"`
Expected: FAIL.

**Step 3: Implement query, handler, and DTO**

Create `GetCandidateOutcomeHistoryQuery.cs`:
```csharp
public record GetCandidateOutcomeHistoryQuery(
    Guid RecruitmentId,
    Guid CandidateId
) : IRequest<List<OutcomeHistoryDto>>;
```

Create `OutcomeHistoryDto.cs`:
```csharp
public record OutcomeHistoryDto
{
    public Guid WorkflowStepId { get; init; }
    public string WorkflowStepName { get; init; } = null!;
    public int StepOrder { get; init; }
    public OutcomeStatus Outcome { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedByUserId { get; init; }
}
```

Create `GetCandidateOutcomeHistoryQueryHandler.cs`:
```csharp
public class GetCandidateOutcomeHistoryQueryHandler(
    IApplicationDbContext context,
    ITenantContext tenantContext)
    : IRequestHandler<GetCandidateOutcomeHistoryQuery, List<OutcomeHistoryDto>>
{
    public async Task<List<OutcomeHistoryDto>> Handle(
        GetCandidateOutcomeHistoryQuery request, CancellationToken ct)
    {
        var recruitment = await context.Recruitments
            .Include(r => r.Members)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        if (!recruitment.Members.Any(m => m.UserId == tenantContext.UserGuid))
            throw new ForbiddenAccessException();

        var candidate = await context.Candidates
            .Include(c => c.Outcomes)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        var stepLookup = recruitment.Steps.ToDictionary(s => s.Id);

        return candidate.Outcomes
            .Where(o => o.Status != OutcomeStatus.NotStarted)
            .Select(o =>
            {
                stepLookup.TryGetValue(o.WorkflowStepId, out var step);
                return new OutcomeHistoryDto
                {
                    WorkflowStepId = o.WorkflowStepId,
                    WorkflowStepName = step?.Name ?? "Unknown",
                    StepOrder = step?.Order ?? 0,
                    Outcome = o.Status,
                    Reason = o.Reason,
                    RecordedAt = o.RecordedAt,
                    RecordedByUserId = o.RecordedByUserId,
                };
            })
            .OrderBy(h => h.StepOrder)
            .ToList();
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "ClassName~GetCandidateOutcomeHistory"`
Expected: ALL PASS.

**Step 5: Update AuthorizationArchitectureTests exempt list if needed**

Check if the new handlers need to be added. Both inject `ITenantContext`, so no exemption needed. Verify:

Run: `dotnet test api/tests/Application.UnitTests/ --filter "ClassName~AuthorizationArchitectureTests"`
Expected: PASS (new handlers inject ITenantContext).

**Step 6: Commit**

```bash
git add api/src/Application/Features/Screening/Queries/ api/tests/Application.UnitTests/Features/Screening/Queries/
git commit -m "feat(4.3): add GetCandidateOutcomeHistoryQuery with handler + unit tests"
```

---

### Task 6: Create ScreeningEndpoints

**TDD Mode:** Characterization (endpoint wiring, tested via handler unit tests)

**Files:**
- Create: `api/src/Web/Endpoints/ScreeningEndpoints.cs`

**Step 1: Create endpoint class**

```csharp
// api/src/Web/Endpoints/ScreeningEndpoints.cs
using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;
using api.Domain.Enums;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class ScreeningEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}/candidates/{candidateId:guid}/screening";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/outcome", RecordOutcome);
        group.MapGet("/history", GetOutcomeHistory);
    }

    private static async Task<IResult> RecordOutcome(
        Guid recruitmentId,
        Guid candidateId,
        RecordOutcomeRequest request,
        ISender sender)
    {
        var command = new RecordOutcomeCommand(
            recruitmentId, candidateId,
            request.WorkflowStepId, request.Outcome, request.Reason);
        var result = await sender.Send(command);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetOutcomeHistory(
        Guid recruitmentId,
        Guid candidateId,
        ISender sender)
    {
        var query = new GetCandidateOutcomeHistoryQuery(recruitmentId, candidateId);
        var result = await sender.Send(query);
        return Results.Ok(result);
    }
}

public record RecordOutcomeRequest(
    Guid WorkflowStepId,
    OutcomeStatus Outcome,
    string? Reason);
```

**Step 2: Verify full backend build**

Run: `dotnet build api/`
Expected: Build success.

**Step 3: Run all backend tests**

Run: `dotnet test api/`
Expected: ALL PASS.

**Step 4: Commit**

```bash
git add api/src/Web/Endpoints/ScreeningEndpoints.cs
git commit -m "feat(4.3): add ScreeningEndpoints with POST outcome and GET history"
```

---

### Task 7: Create EF Core migration

**TDD Mode:** N/A (infrastructure)

**Files:**
- Create: New migration in `api/src/Infrastructure/Data/Migrations/`

**Step 1: Add migration**

Run: `dotnet ef migrations add AddOutcomeWorkflowEnforcement --project api/src/Infrastructure/ --startup-project api/src/Web/`

This should generate a migration adding:
- `CurrentWorkflowStepId` (nullable Guid) column to `Candidates`
- `IsCompleted` (bool, default false) column to `Candidates`
- `Reason` (nvarchar(500), nullable) column to `CandidateOutcomes`

**Step 2: Review migration file**

Verify the migration contains the expected `AddColumn` calls. Remove any spurious changes.

**Step 3: Commit**

```bash
git add api/src/Infrastructure/Data/Migrations/
git commit -m "feat(4.3): add EF migration for workflow enforcement columns"
```

---

### Task 8: Frontend -- Screening API client and types

**TDD Mode:** Characterization (thin wrapper, tested via component tests)

**Files:**
- Create: `web/src/lib/api/screening.types.ts`
- Create: `web/src/lib/api/screening.ts`

**Step 1: Create screening types**

```typescript
// web/src/lib/api/screening.types.ts
export type OutcomeStatus = 'NotStarted' | 'Pass' | 'Fail' | 'Hold'

export interface RecordOutcomeRequest {
  workflowStepId: string
  outcome: OutcomeStatus
  reason?: string
}

export interface OutcomeResultDto {
  outcomeId: string
  candidateId: string
  workflowStepId: string
  outcome: OutcomeStatus
  reason: string | null
  recordedAt: string
  recordedBy: string
  newCurrentStepId: string | null
  isCompleted: boolean
}

export interface OutcomeHistoryDto {
  workflowStepId: string
  workflowStepName: string
  stepOrder: number
  outcome: OutcomeStatus
  reason: string | null
  recordedAt: string
  recordedByUserId: string
}
```

**Step 2: Create screening API client**

```typescript
// web/src/lib/api/screening.ts
import { apiGet, apiPost } from './httpClient'
import type {
  RecordOutcomeRequest,
  OutcomeResultDto,
  OutcomeHistoryDto,
} from './screening.types'

export const screeningApi = {
  recordOutcome: (
    recruitmentId: string,
    candidateId: string,
    data: RecordOutcomeRequest,
  ) =>
    apiPost<OutcomeResultDto>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/screening/outcome`,
      data,
    ),

  getOutcomeHistory: (recruitmentId: string, candidateId: string) =>
    apiGet<OutcomeHistoryDto[]>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/screening/history`,
    ),
}
```

**Step 3: Verify TypeScript compiles**

Run: `cd web && npx tsc --noEmit`
Expected: No errors.

**Step 4: Commit**

```bash
git add web/src/lib/api/screening.types.ts web/src/lib/api/screening.ts
git commit -m "feat(4.3): add screening API client types and methods"
```

---

### Task 9: Frontend -- MSW handlers and fixtures

**TDD Mode:** Characterization (test infrastructure)

**Files:**
- Create: `web/src/mocks/fixtures/screening.ts`
- Create: `web/src/mocks/screeningHandlers.ts`
- Modify: `web/src/mocks/handlers.ts`

**Step 1: Create screening fixtures**

```typescript
// web/src/mocks/fixtures/screening.ts
import type {
  OutcomeResultDto,
  OutcomeHistoryDto,
} from '@/lib/api/screening.types'
import { mockCandidateId1, mockStepId1, mockStepId2 } from './candidates'

export const mockOutcomeResult: OutcomeResultDto = {
  outcomeId: 'outcome-1111-1111-1111-111111111111',
  candidateId: mockCandidateId1,
  workflowStepId: mockStepId1,
  outcome: 'Pass',
  reason: 'Strong technical skills',
  recordedAt: '2026-02-14T14:00:00Z',
  recordedBy: 'user-1111-1111-1111-111111111111',
  newCurrentStepId: mockStepId2,
  isCompleted: false,
}

export const mockOutcomeHistoryList: OutcomeHistoryDto[] = [
  {
    workflowStepId: mockStepId1,
    workflowStepName: 'Screening',
    stepOrder: 1,
    outcome: 'Pass',
    reason: 'Strong technical skills',
    recordedAt: '2026-02-14T14:00:00Z',
    recordedByUserId: 'user-1111-1111-1111-111111111111',
  },
]
```

**Step 2: Create screening MSW handlers**

```typescript
// web/src/mocks/screeningHandlers.ts
import { http, HttpResponse } from 'msw'
import type { RecordOutcomeRequest } from '@/lib/api/screening.types'
import { mockOutcomeResult, mockOutcomeHistoryList } from './fixtures/screening'

export const screeningHandlers = [
  http.post(
    '/api/recruitments/:recruitmentId/candidates/:candidateId/screening/outcome',
    async ({ request }) => {
      const body = (await request.json()) as RecordOutcomeRequest
      return HttpResponse.json({
        ...mockOutcomeResult,
        outcome: body.outcome,
        reason: body.reason ?? null,
        workflowStepId: body.workflowStepId,
      })
    },
  ),

  http.get(
    '/api/recruitments/:recruitmentId/candidates/:candidateId/screening/history',
    () => {
      return HttpResponse.json(mockOutcomeHistoryList)
    },
  ),
]
```

**Step 3: Register in handlers.ts**

Add import and spread `screeningHandlers` into the handlers array.

**Step 4: Verify TypeScript compiles**

Run: `cd web && npx tsc --noEmit`
Expected: No errors.

**Step 5: Commit**

```bash
git add web/src/mocks/fixtures/screening.ts web/src/mocks/screeningHandlers.ts web/src/mocks/handlers.ts
git commit -m "feat(4.3): add MSW screening handlers and fixtures"
```

---

### Task 10: Frontend -- OutcomeForm component with useRecordOutcome hook

**TDD Mode:** Test-first (user-facing form with mutation)

**Files:**
- Create: `web/src/features/screening/hooks/useRecordOutcome.ts`
- Create: `web/src/features/screening/OutcomeForm.tsx`
- Create: `web/src/features/screening/OutcomeForm.test.tsx`

**Step 1: Write OutcomeForm tests**

```typescript
// web/src/features/screening/OutcomeForm.test.tsx
import { render, screen, waitFor } from '@/test-utils'
import userEvent from '@testing-library/user-event'
import { OutcomeForm } from './OutcomeForm'
import { server } from '@/mocks/server'
import { http, HttpResponse } from 'msw'

const defaultProps = {
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  candidateId: 'cand-1111-1111-1111-111111111111',
  currentStepId: 'step-1111-1111-1111-111111111111',
  currentStepName: 'Screening',
  existingOutcome: null,
  isClosed: false,
  onOutcomeRecorded: vi.fn(),
}

describe('OutcomeForm', () => {
  it('should display Pass, Fail, Hold buttons', () => {
    render(<OutcomeForm {...defaultProps} />)
    expect(screen.getByRole('button', { name: /pass/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /fail/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /hold/i })).toBeInTheDocument()
  })

  it('should show reason textarea always visible', () => {
    render(<OutcomeForm {...defaultProps} />)
    expect(screen.getByRole('textbox', { name: /reason/i })).toBeInTheDocument()
  })

  it('should disable confirm button when no outcome selected', () => {
    render(<OutcomeForm {...defaultProps} />)
    expect(screen.getByRole('button', { name: /confirm/i })).toBeDisabled()
  })

  it('should enable confirm button when outcome selected', async () => {
    const user = userEvent.setup()
    render(<OutcomeForm {...defaultProps} />)
    await user.click(screen.getByRole('button', { name: /pass/i }))
    expect(screen.getByRole('button', { name: /confirm/i })).toBeEnabled()
  })

  it('should call API with correct parameters on confirm', async () => {
    const user = userEvent.setup()
    const onOutcomeRecorded = vi.fn()
    render(<OutcomeForm {...defaultProps} onOutcomeRecorded={onOutcomeRecorded} />)

    await user.click(screen.getByRole('button', { name: /pass/i }))
    await user.type(screen.getByRole('textbox', { name: /reason/i }), 'Strong candidate')
    await user.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => {
      expect(onOutcomeRecorded).toHaveBeenCalled()
    })
  })

  it('should pre-fill form with existing outcome', () => {
    render(
      <OutcomeForm
        {...defaultProps}
        existingOutcome={{
          workflowStepId: 'step-1111-1111-1111-111111111111',
          workflowStepName: 'Screening',
          stepOrder: 1,
          outcome: 'Fail',
          reason: 'Lacking experience',
          recordedAt: '2026-02-14T14:00:00Z',
          recordedByUserId: 'user-1',
        }}
      />,
    )
    // Fail button should be visually selected (aria-pressed or similar)
    expect(screen.getByRole('button', { name: /fail/i })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('textbox', { name: /reason/i })).toHaveValue('Lacking experience')
  })

  it('should disable all controls when recruitment is closed', () => {
    render(<OutcomeForm {...defaultProps} isClosed={true} />)
    expect(screen.getByRole('button', { name: /pass/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /fail/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /hold/i })).toBeDisabled()
    expect(screen.getByRole('textbox', { name: /reason/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /confirm/i })).toBeDisabled()
  })

  it('should show success feedback after recording', async () => {
    const user = userEvent.setup()
    render(<OutcomeForm {...defaultProps} />)
    await user.click(screen.getByRole('button', { name: /pass/i }))
    await user.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => {
      expect(screen.getByText(/pass recorded/i)).toBeInTheDocument()
    })
  })
})
```

**Step 2: Run tests to verify they fail**

Run: `cd web && npx vitest run src/features/screening/OutcomeForm.test.tsx`
Expected: FAIL (files don't exist).

**Step 3: Implement useRecordOutcome hook**

```typescript
// web/src/features/screening/hooks/useRecordOutcome.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { screeningApi } from '@/lib/api/screening'
import type { RecordOutcomeRequest } from '@/lib/api/screening.types'

export function useRecordOutcome() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      recruitmentId,
      candidateId,
      data,
    }: {
      recruitmentId: string
      candidateId: string
      data: RecordOutcomeRequest
    }) => screeningApi.recordOutcome(recruitmentId, candidateId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: ['screening', 'history', variables.candidateId],
      })
      queryClient.invalidateQueries({
        queryKey: ['candidates', variables.recruitmentId],
      })
    },
  })
}
```

**Step 4: Implement OutcomeForm component**

```tsx
// web/src/features/screening/OutcomeForm.tsx
import { useState } from 'react'
import { useAppToast } from '@/components/Toast/useAppToast'
import { useRecordOutcome } from './hooks/useRecordOutcome'
import { cn } from '@/lib/utils'
import type { OutcomeHistoryDto, OutcomeResultDto, OutcomeStatus } from '@/lib/api/screening.types'

interface OutcomeFormProps {
  recruitmentId: string
  candidateId: string
  currentStepId: string
  currentStepName: string
  existingOutcome: OutcomeHistoryDto | null
  isClosed: boolean
  onOutcomeRecorded?: (result: OutcomeResultDto) => void
}

const outcomeOptions: { value: OutcomeStatus; label: string; className: string; selectedClassName: string }[] = [
  { value: 'Pass', label: 'Pass', className: 'border-green-300 text-green-700 hover:bg-green-50', selectedClassName: 'bg-green-600 text-white border-green-600' },
  { value: 'Fail', label: 'Fail', className: 'border-red-300 text-red-700 hover:bg-red-50', selectedClassName: 'bg-red-600 text-white border-red-600' },
  { value: 'Hold', label: 'Hold', className: 'border-amber-300 text-amber-700 hover:bg-amber-50', selectedClassName: 'bg-amber-500 text-white border-amber-500' },
]

export function OutcomeForm({
  recruitmentId,
  candidateId,
  currentStepId,
  currentStepName,
  existingOutcome,
  isClosed,
  onOutcomeRecorded,
}: OutcomeFormProps) {
  const [selectedOutcome, setSelectedOutcome] = useState<OutcomeStatus | null>(
    existingOutcome?.outcome ?? null,
  )
  const [reason, setReason] = useState(existingOutcome?.reason ?? '')
  const { toast } = useAppToast()
  const recordOutcome = useRecordOutcome()

  const handleConfirm = () => {
    if (!selectedOutcome) return
    recordOutcome.mutate(
      {
        recruitmentId,
        candidateId,
        data: {
          workflowStepId: currentStepId,
          outcome: selectedOutcome,
          reason: reason || undefined,
        },
      },
      {
        onSuccess: (result) => {
          toast({ description: `${selectedOutcome} recorded`, variant: 'success' })
          onOutcomeRecorded?.(result)
        },
      },
    )
  }

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-medium">
        Outcome for: {currentStepName}
      </h3>
      <div className="flex gap-2" role="group" aria-label="Outcome selection">
        {outcomeOptions.map((option) => (
          <button
            key={option.value}
            type="button"
            role="button"
            aria-pressed={selectedOutcome === option.value}
            disabled={isClosed}
            onClick={() => setSelectedOutcome(option.value)}
            className={cn(
              'rounded-md border px-4 py-2 text-sm font-medium transition-colors',
              selectedOutcome === option.value ? option.selectedClassName : option.className,
              isClosed && 'cursor-not-allowed opacity-50',
            )}
          >
            {option.label}
          </button>
        ))}
      </div>
      <div>
        <label htmlFor="outcome-reason" className="text-sm font-medium">
          Reason
        </label>
        <textarea
          id="outcome-reason"
          aria-label="Reason"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          disabled={isClosed}
          maxLength={500}
          placeholder="Optional reason for this decision..."
          className="mt-1 block w-full rounded-md border px-3 py-2 text-sm disabled:cursor-not-allowed disabled:opacity-50"
          rows={3}
        />
      </div>
      <button
        type="button"
        disabled={!selectedOutcome || isClosed || recordOutcome.isPending}
        onClick={handleConfirm}
        className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground disabled:cursor-not-allowed disabled:opacity-50"
      >
        {recordOutcome.isPending ? 'Recording...' : 'Confirm'}
      </button>
    </div>
  )
}
```

**Step 5: Run tests to verify they pass**

Run: `cd web && npx vitest run src/features/screening/OutcomeForm.test.tsx`
Expected: ALL PASS.

**Step 6: Commit**

```bash
git add web/src/features/screening/hooks/useRecordOutcome.ts web/src/features/screening/OutcomeForm.tsx web/src/features/screening/OutcomeForm.test.tsx
git commit -m "feat(4.3): add OutcomeForm component with useRecordOutcome hook + tests"
```

---

### Task 11: Frontend -- OutcomeHistory component with useOutcomeHistory hook

**TDD Mode:** Test-first (data display)

**Files:**
- Create: `web/src/features/screening/hooks/useOutcomeHistory.ts`
- Create: `web/src/features/screening/OutcomeHistory.tsx`
- Create: `web/src/features/screening/OutcomeHistory.test.tsx`

**Step 1: Write OutcomeHistory tests**

```typescript
// web/src/features/screening/OutcomeHistory.test.tsx
import { render, screen } from '@/test-utils'
import { OutcomeHistory } from './OutcomeHistory'
import type { OutcomeHistoryDto } from '@/lib/api/screening.types'

const mockHistory: OutcomeHistoryDto[] = [
  {
    workflowStepId: 'step-1',
    workflowStepName: 'Screening',
    stepOrder: 1,
    outcome: 'Pass',
    reason: 'Strong skills',
    recordedAt: '2026-02-14T14:00:00Z',
    recordedByUserId: 'user-1',
  },
  {
    workflowStepId: 'step-2',
    workflowStepName: 'Interview',
    stepOrder: 2,
    outcome: 'Fail',
    reason: null,
    recordedAt: '2026-02-14T15:00:00Z',
    recordedByUserId: 'user-2',
  },
]

describe('OutcomeHistory', () => {
  it('should display outcome history ordered by step', () => {
    render(<OutcomeHistory history={mockHistory} />)
    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(2)
    expect(items[0]).toHaveTextContent('Screening')
    expect(items[1]).toHaveTextContent('Interview')
  })

  it('should show reason text when provided', () => {
    render(<OutcomeHistory history={mockHistory} />)
    expect(screen.getByText('Strong skills')).toBeInTheDocument()
  })

  it('should display empty state when no outcomes recorded', () => {
    render(<OutcomeHistory history={[]} />)
    expect(screen.getByText(/no outcomes recorded/i)).toBeInTheDocument()
  })

  it('should show status badge with correct variant for each outcome', () => {
    render(<OutcomeHistory history={mockHistory} />)
    expect(screen.getByText('Pass')).toBeInTheDocument()
    expect(screen.getByText('Fail')).toBeInTheDocument()
  })

  it('should display recorded date', () => {
    render(<OutcomeHistory history={mockHistory} />)
    // Check dates are rendered (format may vary by locale)
    const items = screen.getAllByRole('listitem')
    expect(items[0]).toHaveTextContent(/2026/)
  })
})
```

**Step 2: Run tests to verify they fail**

Run: `cd web && npx vitest run src/features/screening/OutcomeHistory.test.tsx`
Expected: FAIL.

**Step 3: Implement useOutcomeHistory hook**

```typescript
// web/src/features/screening/hooks/useOutcomeHistory.ts
import { useQuery } from '@tanstack/react-query'
import { screeningApi } from '@/lib/api/screening'

export function useOutcomeHistory(recruitmentId: string, candidateId: string) {
  return useQuery({
    queryKey: ['screening', 'history', candidateId],
    queryFn: () => screeningApi.getOutcomeHistory(recruitmentId, candidateId),
    enabled: !!recruitmentId && !!candidateId,
  })
}
```

**Step 4: Implement OutcomeHistory component**

```tsx
// web/src/features/screening/OutcomeHistory.tsx
import { StatusBadge } from '@/components/StatusBadge'
import type { OutcomeHistoryDto } from '@/lib/api/screening.types'
import type { StatusVariant } from '@/components/StatusBadge.types'

interface OutcomeHistoryProps {
  history: OutcomeHistoryDto[]
}

function toStatusVariant(outcome: string): StatusVariant {
  switch (outcome) {
    case 'Pass':
      return 'pass'
    case 'Fail':
      return 'fail'
    case 'Hold':
      return 'hold'
    default:
      return 'not-started'
  }
}

export function OutcomeHistory({ history }: OutcomeHistoryProps) {
  if (history.length === 0) {
    return (
      <p className="text-muted-foreground text-sm">No outcomes recorded yet.</p>
    )
  }

  const sorted = [...history].sort((a, b) => a.stepOrder - b.stepOrder)

  return (
    <ul className="space-y-3" role="list">
      {sorted.map((entry) => (
        <li key={entry.workflowStepId} className="rounded-md border p-3">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">{entry.workflowStepName}</span>
            <StatusBadge status={toStatusVariant(entry.outcome)} />
          </div>
          {entry.reason && (
            <p className="text-muted-foreground mt-1 text-sm">{entry.reason}</p>
          )}
          <p className="text-muted-foreground mt-1 text-xs">
            {new Intl.DateTimeFormat(undefined, {
              dateStyle: 'medium',
              timeStyle: 'short',
            }).format(new Date(entry.recordedAt))}
          </p>
        </li>
      ))}
    </ul>
  )
}
```

**Step 5: Run tests to verify they pass**

Run: `cd web && npx vitest run src/features/screening/OutcomeHistory.test.tsx`
Expected: ALL PASS.

**Step 6: Commit**

```bash
git add web/src/features/screening/hooks/useOutcomeHistory.ts web/src/features/screening/OutcomeHistory.tsx web/src/features/screening/OutcomeHistory.test.tsx
git commit -m "feat(4.3): add OutcomeHistory component with useOutcomeHistory hook + tests"
```

---

### Task 12: Verification and Story Completion

**TDD Mode:** N/A (verification)

**Step 1: Run full backend test suite**

Run: `dotnet test api/`
Expected: ALL PASS.

**Step 2: Run full frontend test suite**

Run: `cd web && npx vitest run`
Expected: ALL PASS.

**Step 3: TypeScript check**

Run: `cd web && npx tsc --noEmit`
Expected: No errors.

**Step 4: Production build check**

Run: `cd web && npm run build`
Expected: Build success.

**Step 5: Update sprint status**

Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: change `4-3-outcome-recording-workflow-enforcement` from `ready-for-dev` to `done`.

**Step 6: Update story artifact with Dev Agent Record**

Update `_bmad-output/implementation-artifacts/4-3-outcome-recording-workflow-enforcement.md`:
- Set Status to `done`
- Fill in Dev Agent Record section

**Step 7: Commit**

```bash
git add _bmad-output/
git commit -m "docs(4.3): add Dev Agent Record and mark story done"
```

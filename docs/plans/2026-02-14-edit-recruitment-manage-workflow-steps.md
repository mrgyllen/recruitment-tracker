# Story 2.3: Edit Recruitment & Manage Workflow Steps — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable editing recruitment details and managing workflow steps (add/remove/reorder) on active recruitments, with read-only mode for closed recruitments.

**Architecture:** Full-stack CQRS. Backend: domain methods on Recruitment aggregate + MediatR command handlers + Minimal API endpoints. Frontend: inline editing form + extended WorkflowStepEditor with per-action API mutations. Domain exceptions (StepHasOutcomesException, RecruitmentClosedException, DuplicateStepNameException) flow through CustomExceptionHandler to Problem Details responses.

**Tech Stack:** .NET 10 / EF Core / MediatR / FluentValidation / NUnit / NSubstitute / FluentAssertions | React 19 / TypeScript / TanStack Query v5 / react-hook-form / zod / shadcn/ui / Vitest / MSW

---

## Task 1: Domain — UpdateDetails + ReorderSteps methods

Adds `UpdateDetails()` and `ReorderSteps()` to the Recruitment aggregate, plus `UpdateOrder()` internal method on WorkflowStep.

**Files:**
- Modify: `api/src/Domain/Entities/Recruitment.cs`
- Modify: `api/src/Domain/Entities/WorkflowStep.cs`
- Modify: `api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`

**Testing mode:** Test-first — core aggregate invariants.

### Step 1: Write failing domain tests

Add these tests to `api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`:

```csharp
[Test]
public void UpdateDetails_ValidInput_UpdatesProperties()
{
    var recruitment = CreateRecruitment();

    recruitment.UpdateDetails("New Title", "New Desc", "REQ-001");

    recruitment.Title.Should().Be("New Title");
    recruitment.Description.Should().Be("New Desc");
    recruitment.JobRequisitionId.Should().Be("REQ-001");
}

[Test]
public void UpdateDetails_WhenClosed_ThrowsRecruitmentClosedException()
{
    var recruitment = CreateRecruitment();
    recruitment.Close();

    var act = () => recruitment.UpdateDetails("New Title", null, null);

    act.Should().Throw<RecruitmentClosedException>();
}

[Test]
public void ReorderSteps_ValidReorder_UpdatesStepOrders()
{
    var recruitment = CreateRecruitment();
    recruitment.AddStep("Screening", 1);
    recruitment.AddStep("Interview", 2);
    var step1 = recruitment.Steps.First(s => s.Name == "Screening");
    var step2 = recruitment.Steps.First(s => s.Name == "Interview");

    recruitment.ReorderSteps(new List<(Guid StepId, int NewOrder)>
    {
        (step2.Id, 1),
        (step1.Id, 2),
    });

    recruitment.Steps.First(s => s.Name == "Interview").Order.Should().Be(1);
    recruitment.Steps.First(s => s.Name == "Screening").Order.Should().Be(2);
}

[Test]
public void ReorderSteps_UnknownStepId_ThrowsInvalidOperationException()
{
    var recruitment = CreateRecruitment();
    recruitment.AddStep("Screening", 1);

    var act = () => recruitment.ReorderSteps(new List<(Guid StepId, int NewOrder)>
    {
        (Guid.NewGuid(), 1),
    });

    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*not found*");
}

[Test]
public void ReorderSteps_NonContiguousOrders_ThrowsArgumentException()
{
    var recruitment = CreateRecruitment();
    recruitment.AddStep("Screening", 1);
    recruitment.AddStep("Interview", 2);
    var step1 = recruitment.Steps.First(s => s.Name == "Screening");
    var step2 = recruitment.Steps.First(s => s.Name == "Interview");

    var act = () => recruitment.ReorderSteps(new List<(Guid StepId, int NewOrder)>
    {
        (step1.Id, 1),
        (step2.Id, 5),
    });

    act.Should().Throw<ArgumentException>()
        .WithMessage("*contiguous*");
}

[Test]
public void ReorderSteps_WhenClosed_ThrowsRecruitmentClosedException()
{
    var recruitment = CreateRecruitment();
    recruitment.AddStep("Screening", 1);
    recruitment.Close();

    var act = () => recruitment.ReorderSteps(new List<(Guid StepId, int NewOrder)>
    {
        (recruitment.Steps.First().Id, 1),
    });

    act.Should().Throw<RecruitmentClosedException>();
}
```

### Step 2: Run tests to verify they fail

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/api && dotnet test tests/Domain.UnitTests --filter "UpdateDetails|ReorderSteps" --verbosity quiet`
Expected: FAIL — methods don't exist.

### Step 3: Implement domain methods

Add to `api/src/Domain/Entities/WorkflowStep.cs`:

```csharp
internal void UpdateOrder(int newOrder)
{
    Order = newOrder;
}
```

Add to `api/src/Domain/Entities/Recruitment.cs`:

```csharp
public void UpdateDetails(string title, string? description, string? jobRequisitionId)
{
    EnsureNotClosed();
    Title = title;
    Description = description;
    JobRequisitionId = jobRequisitionId;
}

public void ReorderSteps(List<(Guid StepId, int NewOrder)> reordering)
{
    EnsureNotClosed();

    var orders = reordering.Select(r => r.NewOrder).OrderBy(o => o).ToList();
    if (!orders.SequenceEqual(Enumerable.Range(1, reordering.Count)))
    {
        throw new ArgumentException("Step orders must be contiguous starting from 1.");
    }

    foreach (var (stepId, newOrder) in reordering)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found.");
        step.UpdateOrder(newOrder);
    }
}
```

### Step 4: Run tests to verify they pass

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/api && dotnet test tests/Domain.UnitTests --verbosity quiet`
Expected: All tests PASS.

### Step 5: Commit

```bash
git add api/src/Domain/Entities/Recruitment.cs api/src/Domain/Entities/WorkflowStep.cs api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs
git commit -m "feat(story-2.3): domain UpdateDetails + ReorderSteps methods"
```

---

## Task 2: Backend — UpdateRecruitment command + handler + validator

**Files:**
- Create: `api/src/Application/Features/Recruitments/Commands/UpdateRecruitment/UpdateRecruitmentCommand.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/UpdateRecruitment/UpdateRecruitmentCommandValidator.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/UpdateRecruitment/UpdateRecruitmentCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/UpdateRecruitment/UpdateRecruitmentCommandHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/UpdateRecruitment/UpdateRecruitmentCommandValidatorTests.cs`

**Testing mode:** Test-first — business logic orchestration.

### Step 1: Write command + validator

`UpdateRecruitmentCommand.cs`:
```csharp
namespace api.Application.Features.Recruitments.Commands.UpdateRecruitment;

public record UpdateRecruitmentCommand : IRequest
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? JobRequisitionId { get; init; }
}
```

`UpdateRecruitmentCommandValidator.cs`:
```csharp
namespace api.Application.Features.Recruitments.Commands.UpdateRecruitment;

public class UpdateRecruitmentCommandValidator : AbstractValidator<UpdateRecruitmentCommand>
{
    public UpdateRecruitmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.JobRequisitionId).MaximumLength(100);
    }
}
```

### Step 2: Write failing validator tests

`UpdateRecruitmentCommandValidatorTests.cs`:
```csharp
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands.UpdateRecruitment;

public class UpdateRecruitmentCommandValidatorTests
{
    private UpdateRecruitmentCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateRecruitmentCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_Passes()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = "Senior Dev",
            Description = "A description",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyTitle_Fails()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = "",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public void Validate_TitleTooLong_Fails()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = new string('x', 201),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public void Validate_EmptyId_Fails()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.Empty,
            Title = "Valid Title",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
```

### Step 3: Write failing handler tests

`UpdateRecruitmentCommandHandlerTests.cs`:
```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.UpdateRecruitment;

public class UpdateRecruitmentCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private UpdateRecruitmentCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new UpdateRecruitmentCommandHandler(_dbContext);
    }

    [Test]
    public async Task Handle_ValidCommand_UpdatesRecruitment()
    {
        var recruitment = Recruitment.Create("Old Title", "Old Desc", Guid.NewGuid());
        var recruitments = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitments);

        var command = new UpdateRecruitmentCommand
        {
            Id = recruitment.Id,
            Title = "New Title",
            Description = "New Desc",
            JobRequisitionId = "REQ-001",
        };

        await _handler.Handle(command, CancellationToken.None);

        recruitment.Title.Should().Be("New Title");
        recruitment.Description.Should().Be("New Desc");
        recruitment.JobRequisitionId.Should().Be("REQ-001");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        var recruitments = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitments);

        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = "New Title",
        };

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

**Note:** The tests use `BuildMockDbSet()` helper. Check if this exists in the test project — if not, create a `MockDbSetExtensions.cs` helper in the test project's root that wraps a `List<T>` into an `IQueryable` + mock `DbSet<T>` via NSubstitute, supporting `FirstOrDefaultAsync` via `AsyncQueryableExtensions`. This is a common pattern used across all handler tests.

### Step 4: Write handler implementation

`UpdateRecruitmentCommandHandler.cs`:
```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.UpdateRecruitment;

public class UpdateRecruitmentCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateRecruitmentCommand>
{
    public async Task Handle(UpdateRecruitmentCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.Id);

        recruitment.UpdateDetails(request.Title, request.Description, request.JobRequisitionId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### Step 5: Run tests

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/api && dotnet test tests/Application.UnitTests --filter "UpdateRecruitment" --verbosity quiet`
Expected: All PASS.

### Step 6: Commit

```bash
git add api/src/Application/Features/Recruitments/Commands/UpdateRecruitment/
git add api/tests/Application.UnitTests/Features/Recruitments/Commands/UpdateRecruitment/
git commit -m "feat(story-2.3): UpdateRecruitment command + handler + validator"
```

---

## Task 3: Backend — AddWorkflowStep command + handler + validator

**Files:**
- Create: `api/src/Application/Features/Recruitments/Commands/AddWorkflowStep/AddWorkflowStepCommand.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/AddWorkflowStep/AddWorkflowStepCommandValidator.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/AddWorkflowStep/AddWorkflowStepCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/AddWorkflowStep/AddWorkflowStepCommandHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/AddWorkflowStep/AddWorkflowStepCommandValidatorTests.cs`

**Testing mode:** Test-first.

### Step 1: Write command + validator

`AddWorkflowStepCommand.cs`:
```csharp
namespace api.Application.Features.Recruitments.Commands.AddWorkflowStep;

public record AddWorkflowStepCommand : IRequest<WorkflowStepDetailDto>
{
    public Guid RecruitmentId { get; init; }
    public string Name { get; init; } = null!;
    public int Order { get; init; }
}
```

Note: Returns `WorkflowStepDetailDto` from the existing `GetRecruitmentById` query folder.

`AddWorkflowStepCommandValidator.cs`:
```csharp
namespace api.Application.Features.Recruitments.Commands.AddWorkflowStep;

public class AddWorkflowStepCommandValidator : AbstractValidator<AddWorkflowStepCommand>
{
    public AddWorkflowStepCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Step name is required.").MaximumLength(100);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}
```

### Step 2: Write failing tests (validator + handler)

Validator tests validate: valid passes, empty name fails, empty recruitment ID fails.

Handler tests validate:
- Step added successfully, returns DTO with new step details, `SaveChangesAsync` called
- Recruitment not found throws `NotFoundException`

### Step 3: Write handler

`AddWorkflowStepCommandHandler.cs`:
```csharp
using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.AddWorkflowStep;

public class AddWorkflowStepCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<AddWorkflowStepCommand, WorkflowStepDetailDto>
{
    public async Task<WorkflowStepDetailDto> Handle(
        AddWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        recruitment.AddStep(request.Name, request.Order);

        await dbContext.SaveChangesAsync(cancellationToken);

        var newStep = recruitment.Steps.First(s => s.Name == request.Name);
        return new WorkflowStepDetailDto
        {
            Id = newStep.Id,
            Name = newStep.Name,
            Order = newStep.Order,
        };
    }
}
```

### Step 4: Run tests, verify pass

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/api && dotnet test tests/Application.UnitTests --filter "AddWorkflowStep" --verbosity quiet`

### Step 5: Commit

```bash
git add api/src/Application/Features/Recruitments/Commands/AddWorkflowStep/
git add api/tests/Application.UnitTests/Features/Recruitments/Commands/AddWorkflowStep/
git commit -m "feat(story-2.3): AddWorkflowStep command + handler + validator"
```

---

## Task 4: Backend — RemoveWorkflowStep command + handler

**Files:**
- Create: `api/src/Application/Features/Recruitments/Commands/RemoveWorkflowStep/RemoveWorkflowStepCommand.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/RemoveWorkflowStep/RemoveWorkflowStepCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/RemoveWorkflowStep/RemoveWorkflowStepCommandHandlerTests.cs`

**Testing mode:** Test-first — complex outcome-checking logic.

**Important:** The story spec says the handler should check `CandidateOutcomes` for step outcomes. However, `IApplicationDbContext` does NOT have a `CandidateOutcomes` DbSet. Since `CandidateOutcome` is a child entity of the `Candidate` aggregate (via `_outcomes`), we must query through `Candidates`:

```csharp
var hasOutcomes = await dbContext.Candidates
    .AnyAsync(c => c.Outcomes.Any(o => o.WorkflowStepId == request.StepId), cancellationToken);
```

This uses EF Core's ability to navigate through collection properties in LINQ queries.

### Step 1: Write command

`RemoveWorkflowStepCommand.cs`:
```csharp
namespace api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;

public record RemoveWorkflowStepCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
    public Guid StepId { get; init; }
}
```

### Step 2: Write failing handler tests

Tests:
- Step removed when no outcomes — `SaveChangesAsync` called, step gone from aggregate
- Step with outcomes — `StepHasOutcomesException` propagated (NOT caught)
- Recruitment not found — `NotFoundException`

### Step 3: Write handler

```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;

public class RemoveWorkflowStepCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<RemoveWorkflowStepCommand>
{
    public async Task Handle(
        RemoveWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var hasOutcomes = await dbContext.Candidates
            .AnyAsync(c => c.Outcomes.Any(o => o.WorkflowStepId == request.StepId), cancellationToken);

        if (hasOutcomes)
            recruitment.MarkStepHasOutcomes(request.StepId);

        recruitment.RemoveStep(request.StepId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### Step 4: Run tests, verify pass

### Step 5: Commit

```bash
git add api/src/Application/Features/Recruitments/Commands/RemoveWorkflowStep/
git add api/tests/Application.UnitTests/Features/Recruitments/Commands/RemoveWorkflowStep/
git commit -m "feat(story-2.3): RemoveWorkflowStep command + handler"
```

---

## Task 5: Backend — ReorderWorkflowSteps command + handler + validator

**Files:**
- Create: `api/src/Application/Features/Recruitments/Commands/ReorderWorkflowSteps/ReorderWorkflowStepsCommand.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/ReorderWorkflowSteps/ReorderWorkflowStepsCommandValidator.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/ReorderWorkflowSteps/ReorderWorkflowStepsCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/ReorderWorkflowSteps/ReorderWorkflowStepsCommandHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/ReorderWorkflowSteps/ReorderWorkflowStepsCommandValidatorTests.cs`

**Testing mode:** Test-first.

### Step 1: Write command + validator

`ReorderWorkflowStepsCommand.cs`:
```csharp
namespace api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;

public record ReorderWorkflowStepsCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
    public List<StepOrderDto> Steps { get; init; } = [];
}

public record StepOrderDto
{
    public Guid StepId { get; init; }
    public int Order { get; init; }
}
```

`ReorderWorkflowStepsCommandValidator.cs`:
```csharp
namespace api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;

public class ReorderWorkflowStepsCommandValidator : AbstractValidator<ReorderWorkflowStepsCommand>
{
    public ReorderWorkflowStepsCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.Steps).NotEmpty().WithMessage("Steps list cannot be empty.");
        RuleFor(x => x.Steps)
            .Must(steps =>
            {
                var orders = steps.Select(s => s.Order).OrderBy(o => o).ToList();
                return orders.SequenceEqual(Enumerable.Range(1, steps.Count));
            })
            .When(x => x.Steps.Count > 0)
            .WithMessage("Step orders must be contiguous starting from 1.");
    }
}
```

### Step 2: Write failing tests

Validator: valid passes, empty list fails, non-contiguous orders fail.
Handler: valid reorder updates steps, recruitment not found throws.

### Step 3: Write handler

```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;

public class ReorderWorkflowStepsCommandHandler(
    IApplicationDbContext dbContext)
    : IRequestHandler<ReorderWorkflowStepsCommand>
{
    public async Task Handle(
        ReorderWorkflowStepsCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var reordering = request.Steps
            .Select(s => (s.StepId, s.Order))
            .ToList();

        recruitment.ReorderSteps(reordering);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### Step 4: Run tests, verify pass

### Step 5: Commit

```bash
git add api/src/Application/Features/Recruitments/Commands/ReorderWorkflowSteps/
git add api/tests/Application.UnitTests/Features/Recruitments/Commands/ReorderWorkflowSteps/
git commit -m "feat(story-2.3): ReorderWorkflowSteps command + handler + validator"
```

---

## Task 6: Backend — Exception handler mapping + API endpoints

Register domain exceptions in `CustomExceptionHandler` and add new endpoints to `RecruitmentEndpoints.cs`.

**Files:**
- Modify: `api/src/Web/Infrastructure/CustomExceptionHandler.cs`
- Modify: `api/src/Web/Endpoints/RecruitmentEndpoints.cs`

**Testing mode:** Test-first for exception mapping; endpoint tests are integration-level (deferred to functional tests if WebApplicationFactory available; otherwise verified manually).

### Step 1: Add exception mappings

Add to `CustomExceptionHandler.cs` constructor dictionary:

```csharp
{ typeof(api.Domain.Exceptions.RecruitmentClosedException), HandleRecruitmentClosedException },
{ typeof(api.Domain.Exceptions.StepHasOutcomesException), HandleStepHasOutcomesException },
{ typeof(api.Domain.Exceptions.DuplicateStepNameException), HandleDuplicateStepNameException },
```

Add handler methods:

```csharp
private async Task HandleRecruitmentClosedException(HttpContext httpContext, Exception ex)
{
    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Recruitment is closed",
        Detail = ex.Message,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    });
}

private async Task HandleStepHasOutcomesException(HttpContext httpContext, Exception ex)
{
    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Cannot remove -- outcomes recorded at this step",
        Detail = ex.Message,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
    });
}

private async Task HandleDuplicateStepNameException(HttpContext httpContext, Exception ex)
{
    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Duplicate step name",
        Detail = ex.Message,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
    });
}
```

### Step 2: Add API endpoints

Add to `RecruitmentEndpoints.cs` `Map()` method and create endpoint methods:

```csharp
group.MapPut("/{id:guid}", UpdateRecruitment);
group.MapPost("/{id:guid}/steps", AddWorkflowStep);
group.MapDelete("/{id:guid}/steps/{stepId:guid}", RemoveWorkflowStep);
group.MapPut("/{id:guid}/steps/reorder", ReorderWorkflowSteps);
```

Endpoint methods:
```csharp
private static async Task<IResult> UpdateRecruitment(
    ISender sender, Guid id, UpdateRecruitmentCommand command)
{
    await sender.Send(command with { Id = id });
    return Results.NoContent();
}

private static async Task<IResult> AddWorkflowStep(
    ISender sender, Guid id, AddWorkflowStepCommand command)
{
    var result = await sender.Send(command with { RecruitmentId = id });
    return Results.Ok(result);
}

private static async Task<IResult> RemoveWorkflowStep(
    ISender sender, Guid id, Guid stepId)
{
    await sender.Send(new RemoveWorkflowStepCommand { RecruitmentId = id, StepId = stepId });
    return Results.NoContent();
}

private static async Task<IResult> ReorderWorkflowSteps(
    ISender sender, Guid id, ReorderWorkflowStepsCommand command)
{
    await sender.Send(command with { RecruitmentId = id });
    return Results.NoContent();
}
```

### Step 3: Verify backend builds and all tests pass

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/api && dotnet build --verbosity quiet && dotnet test tests/Domain.UnitTests --verbosity quiet`

### Step 4: Commit

```bash
git add api/src/Web/Infrastructure/CustomExceptionHandler.cs api/src/Web/Endpoints/RecruitmentEndpoints.cs
git commit -m "feat(story-2.3): exception mappings + API endpoints for edit/step management"
```

---

## Task 7: Frontend — API types + client methods + mutation hooks

Extends existing API layer with update, step CRUD, and reorder operations.

**Files:**
- Modify: `web/src/lib/api/recruitments.types.ts`
- Modify: `web/src/lib/api/recruitments.ts`
- Create: `web/src/features/recruitments/hooks/useRecruitmentMutations.ts`

**Testing mode:** Characterization — thin wrappers tested via component tests.

### Step 1: Add types

Add to `web/src/lib/api/recruitments.types.ts`:

```typescript
export interface UpdateRecruitmentRequest {
  title: string
  description?: string | null
  jobRequisitionId?: string | null
}

export interface AddWorkflowStepRequest {
  name: string
  order: number
}

export interface ReorderStepsRequest {
  steps: { stepId: string; order: number }[]
}
```

### Step 2: Add API methods

Update `web/src/lib/api/recruitments.ts` to import new types and add methods:

```typescript
import { apiGet, apiPost, apiPut, apiDelete } from './httpClient'
// ... existing imports + new type imports

export const recruitmentApi = {
  // ... existing methods
  update: (id: string, data: UpdateRecruitmentRequest) =>
    apiPut<void>(`/recruitments/${id}`, data),

  addStep: (recruitmentId: string, data: AddWorkflowStepRequest) =>
    apiPost<WorkflowStepDto>(`/recruitments/${recruitmentId}/steps`, data),

  removeStep: (recruitmentId: string, stepId: string) =>
    apiDelete(`/recruitments/${recruitmentId}/steps/${stepId}`),

  reorderSteps: (recruitmentId: string, data: ReorderStepsRequest) =>
    apiPut<void>(`/recruitments/${recruitmentId}/steps/reorder`, data),
}
```

### Step 3: Create mutation hooks

Create `web/src/features/recruitments/hooks/useRecruitmentMutations.ts`:

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { recruitmentApi } from '@/lib/api/recruitments'
import type {
  AddWorkflowStepRequest,
  ReorderStepsRequest,
  UpdateRecruitmentRequest,
} from '@/lib/api/recruitments.types'

export function useUpdateRecruitment(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateRecruitmentRequest) =>
      recruitmentApi.update(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitment', id] })
      void queryClient.invalidateQueries({ queryKey: ['recruitments'] })
    },
  })
}

export function useAddWorkflowStep(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AddWorkflowStepRequest) =>
      recruitmentApi.addStep(recruitmentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitment', recruitmentId] })
    },
  })
}

export function useRemoveWorkflowStep(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (stepId: string) =>
      recruitmentApi.removeStep(recruitmentId, stepId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitment', recruitmentId] })
    },
  })
}

export function useReorderWorkflowSteps(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ReorderStepsRequest) =>
      recruitmentApi.reorderSteps(recruitmentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitment', recruitmentId] })
    },
  })
}
```

### Step 4: Verify TypeScript compiles

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx tsc --noEmit`

### Step 5: Commit

```bash
git add web/src/lib/api/recruitments.types.ts web/src/lib/api/recruitments.ts web/src/features/recruitments/hooks/useRecruitmentMutations.ts
git commit -m "feat(story-2.3): frontend API types + client methods + mutation hooks"
```

---

## Task 8: Frontend — MSW handlers for new endpoints

**Files:**
- Modify: `web/src/mocks/recruitmentHandlers.ts`

### Step 1: Add MSW handlers

Add handlers for PUT recruitment, POST step, DELETE step, PUT reorder:

```typescript
// PUT /api/recruitments/:id — update recruitment
http.put('/api/recruitments/:id', async ({ params, request }) => {
  const { id } = params
  const recruitment = recruitmentsById[id as string]
  if (!recruitment) {
    return HttpResponse.json(
      { type: '...', title: 'Not Found', status: 404 },
      { status: 404 },
    )
  }
  if (recruitment.status === 'Closed') {
    return HttpResponse.json(
      { type: '...', title: 'Recruitment is closed', status: 400 },
      { status: 400 },
    )
  }
  const body = (await request.json()) as Record<string, unknown>
  recruitment.title = body.title as string
  recruitment.description = (body.description as string) ?? null
  recruitment.jobRequisitionId = (body.jobRequisitionId as string) ?? null
  return new HttpResponse(null, { status: 204 })
}),

// POST /api/recruitments/:id/steps — add step
http.post('/api/recruitments/:id/steps', async ({ params, request }) => {
  const { id } = params
  const recruitment = recruitmentsById[id as string]
  if (!recruitment) {
    return HttpResponse.json({ title: 'Not Found', status: 404 }, { status: 404 })
  }
  const body = (await request.json()) as { name: string; order: number }
  const newStep = { id: `step-new-${Date.now()}`, name: body.name, order: body.order }
  recruitment.steps.push(newStep)
  return HttpResponse.json(newStep)
}),

// DELETE /api/recruitments/:id/steps/:stepId — remove step
http.delete('/api/recruitments/:id/steps/:stepId', ({ params }) => {
  const { id, stepId } = params
  const recruitment = recruitmentsById[id as string]
  if (!recruitment) {
    return HttpResponse.json({ title: 'Not Found', status: 404 }, { status: 404 })
  }
  // Mock: step-3 has outcomes (for testing 409 scenario)
  if (stepId === 'step-has-outcomes') {
    return HttpResponse.json(
      { title: 'Cannot remove -- outcomes recorded at this step', status: 409 },
      { status: 409 },
    )
  }
  recruitment.steps = recruitment.steps.filter((s) => s.id !== stepId)
  return new HttpResponse(null, { status: 204 })
}),

// PUT /api/recruitments/:id/steps/reorder — reorder steps
http.put('/api/recruitments/:id/steps/reorder', async ({ params, request }) => {
  const { id } = params
  const recruitment = recruitmentsById[id as string]
  if (!recruitment) {
    return HttpResponse.json({ title: 'Not Found', status: 404 }, { status: 404 })
  }
  const body = (await request.json()) as { steps: { stepId: string; order: number }[] }
  for (const item of body.steps) {
    const step = recruitment.steps.find((s) => s.id === item.stepId)
    if (step) step.order = item.order
  }
  recruitment.steps.sort((a, b) => a.order - b.order)
  return new HttpResponse(null, { status: 204 })
}),
```

**Note:** The `recruitmentsById` map needs to be mutable for MSW handlers to mutate step arrays. Change `const` to `let` for the `steps` arrays in the mock data, or restructure slightly.

### Step 2: Verify no TypeScript errors

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx tsc --noEmit`

### Step 3: Commit

```bash
git add web/src/mocks/recruitmentHandlers.ts
git commit -m "feat(story-2.3): MSW handlers for edit + step management endpoints"
```

---

## Task 9: Frontend — EditRecruitmentForm component

Inline form for editing recruitment title, description, job requisition ID. Disabled in closed state.

**Files:**
- Create: `web/src/features/recruitments/EditRecruitmentForm.tsx`
- Create: `web/src/features/recruitments/EditRecruitmentForm.test.tsx`

**Testing mode:** Test-first.

### Step 1: Write failing tests

```typescript
// EditRecruitmentForm.test.tsx
import { describe, expect, it } from 'vitest'
import { EditRecruitmentForm } from './EditRecruitmentForm'
import type { RecruitmentDetail } from '@/lib/api/recruitments.types'
import { render, screen, waitFor } from '@/test-utils'
import userEvent from '@testing-library/user-event'

const activeRecruitment: RecruitmentDetail = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  title: 'Senior .NET Developer',
  description: 'Backend role',
  jobRequisitionId: 'REQ-001',
  status: 'Active',
  createdAt: new Date().toISOString(),
  closedAt: null,
  createdByUserId: 'user-1',
  steps: [],
  members: [],
}

const closedRecruitment: RecruitmentDetail = {
  ...activeRecruitment,
  status: 'Closed',
  closedAt: new Date().toISOString(),
}

describe('EditRecruitmentForm', () => {
  it('should render form with existing values', () => {
    render(<EditRecruitmentForm recruitment={activeRecruitment} />)

    expect(screen.getByDisplayValue('Senior .NET Developer')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Backend role')).toBeInTheDocument()
    expect(screen.getByDisplayValue('REQ-001')).toBeInTheDocument()
  })

  it('should disable all fields when recruitment is closed', () => {
    render(<EditRecruitmentForm recruitment={closedRecruitment} />)

    expect(screen.getByLabelText(/title/i)).toBeDisabled()
    expect(screen.getByLabelText(/description/i)).toBeDisabled()
    expect(screen.getByLabelText(/job requisition/i)).toBeDisabled()
  })

  it('should show validation error for empty title on submit', async () => {
    const user = userEvent.setup()
    render(<EditRecruitmentForm recruitment={activeRecruitment} />)

    const titleInput = screen.getByLabelText(/title/i)
    await user.clear(titleInput)
    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(screen.getByText(/title is required/i)).toBeInTheDocument()
    })
  })

  it('should not show save button when recruitment is closed', () => {
    render(<EditRecruitmentForm recruitment={closedRecruitment} />)

    expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument()
  })
})
```

### Step 2: Implement component

```tsx
// EditRecruitmentForm.tsx
import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod/v4'
import { useUpdateRecruitment } from './hooks/useRecruitmentMutations'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'
import type { RecruitmentDetail } from '@/lib/api/recruitments.types'

const editRecruitmentSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(2000).optional(),
  jobRequisitionId: z.string().max(100).optional(),
})

type FormValues = z.infer<typeof editRecruitmentSchema>

interface EditRecruitmentFormProps {
  recruitment: RecruitmentDetail
}

export function EditRecruitmentForm({ recruitment }: EditRecruitmentFormProps) {
  const toast = useAppToast()
  const updateMutation = useUpdateRecruitment(recruitment.id)
  const isClosed = recruitment.status === 'Closed'

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(editRecruitmentSchema),
    defaultValues: {
      title: recruitment.title,
      description: recruitment.description ?? '',
      jobRequisitionId: recruitment.jobRequisitionId ?? '',
    },
  })

  useEffect(() => {
    reset({
      title: recruitment.title,
      description: recruitment.description ?? '',
      jobRequisitionId: recruitment.jobRequisitionId ?? '',
    })
  }, [recruitment, reset])

  function onSubmit(data: FormValues) {
    updateMutation.mutate(
      {
        title: data.title,
        description: data.description || null,
        jobRequisitionId: data.jobRequisitionId || null,
      },
      {
        onSuccess: () => {
          toast.success('Recruitment updated')
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            toast.error(error.problemDetails.title)
          } else {
            toast.error('Failed to update recruitment')
          }
        },
      },
    )
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="edit-title">Title *</Label>
        <Input
          id="edit-title"
          {...register('title')}
          disabled={isClosed}
          aria-invalid={!!errors.title}
          aria-describedby={errors.title ? 'edit-title-error' : undefined}
        />
        {errors.title && (
          <p id="edit-title-error" className="text-destructive text-sm">
            {errors.title.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="edit-description">Description (optional)</Label>
        <Textarea
          id="edit-description"
          {...register('description')}
          disabled={isClosed}
          rows={3}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="edit-jobRequisitionId">Job Requisition Reference (optional)</Label>
        <Input
          id="edit-jobRequisitionId"
          {...register('jobRequisitionId')}
          disabled={isClosed}
        />
      </div>

      {!isClosed && (
        <div className="flex justify-end">
          <Button
            type="submit"
            disabled={updateMutation.isPending || !isDirty}
          >
            {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
          </Button>
        </div>
      )}
    </form>
  )
}
```

### Step 3: Run tests

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run src/features/recruitments/EditRecruitmentForm.test.tsx`

### Step 4: Commit

```bash
git add web/src/features/recruitments/EditRecruitmentForm.tsx web/src/features/recruitments/EditRecruitmentForm.test.tsx
git commit -m "feat(story-2.3): EditRecruitmentForm component with tests"
```

---

## Task 10: Frontend — WorkflowStepEditor edit mode

Extends the existing `WorkflowStepEditor` to support an "edit" mode where each action (add/remove/reorder) triggers an API mutation instead of local-only state changes.

**Files:**
- Modify: `web/src/features/recruitments/WorkflowStepEditor.tsx`
- Modify: `web/src/features/recruitments/WorkflowStepEditor.test.tsx`

**Testing mode:** Test-first.

### Step 1: Design the edit mode

The component currently takes `steps` + `onChange` for local-only mode. For edit mode, add a `mode` prop:

- `mode="create"` (default): existing behavior — local state, `onChange` callback
- `mode="edit"`: API-backed — `recruitmentId` required, mutations fire on each action, step removal shows inline error on 409

Props interface becomes:
```typescript
interface WorkflowStepEditorBaseProps {
  steps: WorkflowStep[]
  disabled?: boolean
}

interface CreateModeProps extends WorkflowStepEditorBaseProps {
  mode?: 'create'
  onChange: (steps: WorkflowStep[]) => void
  recruitmentId?: never
}

interface EditModeProps extends WorkflowStepEditorBaseProps {
  mode: 'edit'
  recruitmentId: string
  onChange?: never
}

type WorkflowStepEditorProps = CreateModeProps | EditModeProps
```

### Step 2: Write failing tests for edit mode

Add to `WorkflowStepEditor.test.tsx`:

```typescript
describe('WorkflowStepEditor (edit mode)', () => {
  it('should call add step API when adding a step', async () => {
    // ... render with mode="edit" + recruitmentId, click Add, enter name, confirm
  })

  it('should call remove step API when removing a step', async () => {
    // ... render with existing steps, click remove, verify MSW handler called
  })

  it('should show inline error when removing step with outcomes', async () => {
    // ... render with step-has-outcomes ID, click remove, verify error message shown
  })

  it('should disable all controls when disabled prop is true', () => {
    // ... render with disabled=true, verify buttons disabled
  })
})
```

### Step 3: Implement edit mode

Extend the component to:
1. In edit mode, "Add Step" opens an inline input; on confirm, calls `useAddWorkflowStep` mutation
2. In edit mode, remove calls `useRemoveWorkflowStep` mutation; on 409, shows inline error
3. In edit mode, up/down calls `useReorderWorkflowSteps` mutation
4. All controls disabled when `disabled=true` (closed recruitment)

### Step 4: Run tests

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run src/features/recruitments/WorkflowStepEditor.test.tsx`

### Step 5: Commit

```bash
git add web/src/features/recruitments/WorkflowStepEditor.tsx web/src/features/recruitments/WorkflowStepEditor.test.tsx
git commit -m "feat(story-2.3): WorkflowStepEditor edit mode with API mutations"
```

---

## Task 11: Frontend — RecruitmentPage integration

Integrates EditRecruitmentForm and edit-mode WorkflowStepEditor into the existing RecruitmentPage.

**Files:**
- Modify: `web/src/features/recruitments/pages/RecruitmentPage.tsx`
- Modify: `web/src/features/recruitments/pages/RecruitmentPage.test.tsx`

### Step 1: Update RecruitmentPage to show edit form and step editor

Replace the static display with:
- `EditRecruitmentForm` component (inline, pre-populated)
- `WorkflowStepEditor` in edit mode with `recruitmentId`
- Both disabled when status is "Closed"

### Step 2: Add/update tests

Add test for closed recruitment showing read-only form + disabled step editor.

### Step 3: Run all frontend tests

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run`

### Step 4: Run ESLint + TypeScript

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx eslint src/ && npx tsc --noEmit`

### Step 5: Commit

```bash
git add web/src/features/recruitments/pages/RecruitmentPage.tsx web/src/features/recruitments/pages/RecruitmentPage.test.tsx
git commit -m "feat(story-2.3): RecruitmentPage integration with edit form + step editor"
```

---

## Task 12: Verification + Final commit

### Step 1: Run all backend tests

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/api && dotnet test tests/Domain.UnitTests --verbosity quiet`

### Step 2: Run all frontend tests

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run`

### Step 3: Run ESLint + TypeScript

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx eslint src/ && npx tsc --noEmit`

### Step 4: Run dotnet format

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/api && dotnet format --verbosity quiet`

### Step 5: Fill Dev Agent Record in story file

Update `_bmad-output/implementation-artifacts/2-3-edit-recruitment-manage-workflow-steps.md` Dev Agent Record section.

### Step 6: Update sprint status

Update `_bmad-output/implementation-artifacts/sprint-status.yaml`:
```yaml
2-3-edit-recruitment-manage-workflow-steps: done
```

### Step 7: Final commit

```bash
git add _bmad-output/implementation-artifacts/2-3-edit-recruitment-manage-workflow-steps.md _bmad-output/implementation-artifacts/sprint-status.yaml
git commit -m "chore: update sprint status + dev agent record for Story 2.3"
```

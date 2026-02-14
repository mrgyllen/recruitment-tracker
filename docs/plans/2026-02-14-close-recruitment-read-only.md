# Close Recruitment & Read-Only View Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add the ability to close a recruitment, enforcing read-only mode across all views, with a confirmation dialog and GDPR retention explanation.

**Architecture:** CQRS command with MediatR for close action. Domain entity `Recruitment.Close()` already exists with all invariants. Frontend uses AlertDialog for destructive confirmation. Most read-only enforcement (disabled controls, badges) is already wired from Stories 2.2-2.4.

**Tech Stack:** ASP.NET Core (.NET 10), MediatR, FluentValidation, NUnit, NSubstitute, FluentAssertions | React 19, TypeScript, TanStack Query v5, shadcn/ui AlertDialog, Vitest, MSW

---

## Pre-Implementation Assessment

**Already complete (no work needed):**
- Domain: `Close()`, `EnsureNotClosed()`, `RecruitmentClosedException`, `RecruitmentClosedEvent`
- Domain tests: Close, AlreadyClosed, AddStep/RemoveStep/UpdateDetails/ReorderSteps when closed
- Exception handler: `RecruitmentClosedException` -> 400 Problem Details
- Frontend types: `status`, `closedAt` already in response types
- MSW: Closed recruitment mock (`mockRecruitmentId2`) exists
- RecruitmentList: Active/Closed badges shown
- EditRecruitmentForm: `isClosed` disables fields, hides Save
- WorkflowStepEditor: `disabled={isClosed}` prop wired
- MemberList: `disabled={isClosed}` prop wired
- RecruitmentPage: Computes `isClosed`, passes down, shows status Badge
- RecruitmentPage tests: Closed recruitment disables form fields

**Gaps to fill:**
1. Backend: CloseRecruitment command/handler/validator/tests
2. Backend: Close endpoint
3. Domain tests: AddMember/RemoveMember when closed
4. Frontend: Install shadcn/ui AlertDialog
5. Frontend: API client `close` method
6. Frontend: `useCloseRecruitment` mutation hook
7. Frontend: MSW handler for close endpoint
8. Frontend: CloseRecruitmentDialog + tests
9. Frontend: Wire close button into RecruitmentPage + test

---

### Task 1: Domain Tests -- AddMember/RemoveMember Closed Guard

**Files:**
- Modify: `api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`

**Step 1: Write the failing tests**

Add two tests after the existing `ReorderSteps_WhenClosed_ThrowsRecruitmentClosedException` test:

```csharp
[Test]
public void AddMember_WhenClosed_ThrowsRecruitmentClosedException()
{
    var recruitment = CreateRecruitment();
    recruitment.Close();

    var act = () => recruitment.AddMember(Guid.NewGuid(), "SME/Collaborator");

    act.Should().Throw<RecruitmentClosedException>();
}

[Test]
public void RemoveMember_WhenClosed_ThrowsRecruitmentClosedException()
{
    var recruitment = CreateRecruitment();
    var userId = Guid.NewGuid();
    recruitment.AddMember(userId, "SME/Collaborator");
    var memberId = recruitment.Members.First(m => m.UserId == userId).Id;
    recruitment.Close();

    var act = () => recruitment.RemoveMember(memberId);

    act.Should().Throw<RecruitmentClosedException>();
}
```

**Step 2: Run tests to verify they pass (domain already has guards)**

Run: `dotnet test api/tests/Domain.UnitTests/Domain.UnitTests.csproj --verbosity quiet`
Expected: PASS (49 tests -- `EnsureNotClosed()` already called in `AddMember`/`RemoveMember`)

**Step 3: Commit**

```bash
git add api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs
git commit -m "test(2.5): add domain tests for AddMember/RemoveMember closed guard"
```

---

### Task 2: CloseRecruitment Command, Validator, Handler (TDD)

**Files:**
- Create: `api/src/Application/Features/Recruitments/Commands/CloseRecruitment/CloseRecruitmentCommand.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/CloseRecruitment/CloseRecruitmentCommandValidator.cs`
- Create: `api/src/Application/Features/Recruitments/Commands/CloseRecruitment/CloseRecruitmentCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/CloseRecruitment/CloseRecruitmentCommandHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Recruitments/Commands/CloseRecruitment/CloseRecruitmentCommandValidatorTests.cs`

**Step 1: Write handler tests (RED)**

```csharp
// CloseRecruitmentCommandHandlerTests.cs
using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.CloseRecruitment;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.CloseRecruitment;

[TestFixture]
public class CloseRecruitmentCommandHandlerTests
{
    private IApplicationDbContext _dbContext;
    private ITenantContext _tenantContext;

    [SetUp]
    public void Setup()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
    }

    [Test]
    public async Task Handle_ValidRequest_ClosesRecruitment()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(creatorId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        await handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        recruitment.Status.Should().Be(RecruitmentStatus.Closed);
        recruitment.ClosedAt.Should().NotBeNull();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_AlreadyClosed_ThrowsRecruitmentClosedException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        recruitment.Close();

        _tenantContext.UserGuid.Returns(creatorId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(Guid.NewGuid()); // different user
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

```csharp
// CloseRecruitmentCommandValidatorTests.cs
using api.Application.Features.Recruitments.Commands.CloseRecruitment;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands.CloseRecruitment;

[TestFixture]
public class CloseRecruitmentCommandValidatorTests
{
    private CloseRecruitmentCommandValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new CloseRecruitmentCommandValidator();
    }

    [Test]
    public void Validate_ValidId_Passes()
    {
        var command = new CloseRecruitmentCommand { RecruitmentId = Guid.NewGuid() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyId_Fails()
    {
        var command = new CloseRecruitmentCommand { RecruitmentId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RecruitmentId);
    }
}
```

**Step 2: Run tests to verify they fail (RED)**

Run: `dotnet build api/tests/Application.UnitTests/Application.UnitTests.csproj --verbosity quiet`
Expected: FAIL (types don't exist yet)

**Step 3: Write implementation (GREEN)**

```csharp
// CloseRecruitmentCommand.cs
namespace api.Application.Features.Recruitments.Commands.CloseRecruitment;

public class CloseRecruitmentCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
}
```

```csharp
// CloseRecruitmentCommandValidator.cs
namespace api.Application.Features.Recruitments.Commands.CloseRecruitment;

public class CloseRecruitmentCommandValidator : AbstractValidator<CloseRecruitmentCommand>
{
    public CloseRecruitmentCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
    }
}
```

```csharp
// CloseRecruitmentCommandHandler.cs
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Commands.CloseRecruitment;

public class CloseRecruitmentCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<CloseRecruitmentCommand>
{
    public async Task Handle(CloseRecruitmentCommand request, CancellationToken cancellationToken)
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

        recruitment.Close();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

**Step 4: Run build + tests to verify (GREEN)**

Run: `dotnet build api/tests/Application.UnitTests/Application.UnitTests.csproj --verbosity quiet`
Expected: Build succeeded. 0 errors.

Run: `dotnet build api/src/Web/Web.csproj --verbosity quiet`
Expected: Build succeeded. 0 errors.

**Step 5: Commit**

```bash
git add api/src/Application/Features/Recruitments/Commands/CloseRecruitment/ \
  api/tests/Application.UnitTests/Features/Recruitments/Commands/CloseRecruitment/
git commit -m "feat(2.5): add CloseRecruitment command, validator, handler + tests"
```

---

### Task 3: Close Endpoint

**Files:**
- Modify: `api/src/Web/Endpoints/RecruitmentEndpoints.cs`

**Step 1: Add close endpoint**

Add to `Map()` method after the reorder line:

```csharp
group.MapPost("/{id:guid}/close", CloseRecruitment);
```

Add the handler method:

```csharp
private static async Task<IResult> CloseRecruitment(
    ISender sender,
    Guid id)
{
    await sender.Send(new CloseRecruitmentCommand { RecruitmentId = id });
    return Results.Ok();
}
```

Add using:

```csharp
using api.Application.Features.Recruitments.Commands.CloseRecruitment;
```

**Step 2: Verify build**

Run: `dotnet build api/src/Web/Web.csproj --verbosity quiet`
Expected: Build succeeded. 0 errors.

**Step 3: Commit**

```bash
git add api/src/Web/Endpoints/RecruitmentEndpoints.cs
git commit -m "feat(2.5): add POST /api/recruitments/{id}/close endpoint"
```

---

### Task 4: Install shadcn/ui AlertDialog

**Step 1: Install the component**

Run: `cd web && npx shadcn@latest add alert-dialog --yes`

**Step 2: Verify component exists**

Run: `ls web/src/components/ui/alert-dialog.tsx`

**Step 3: Commit**

```bash
git add web/src/components/ui/alert-dialog.tsx
git commit -m "chore(2.5): install shadcn/ui AlertDialog component"
```

---

### Task 5: Frontend API Client + Mutation Hook + MSW Handler

**Files:**
- Modify: `web/src/lib/api/recruitments.ts` -- add `close` method
- Modify: `web/src/features/recruitments/hooks/useRecruitmentMutations.ts` -- add `useCloseRecruitment`
- Modify: `web/src/mocks/recruitmentHandlers.ts` -- add close MSW handler

**Step 1: Add close to API client**

In `web/src/lib/api/recruitments.ts`, add after `reorderSteps`:

```typescript
close: (id: string) =>
  apiPost<void>(`/recruitments/${id}/close`),
```

**Step 2: Add useCloseRecruitment hook**

In `web/src/features/recruitments/hooks/useRecruitmentMutations.ts`, add:

```typescript
export function useCloseRecruitment(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => recruitmentApi.close(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitment', id] })
      void queryClient.invalidateQueries({ queryKey: ['recruitments'] })
    },
  })
}
```

**Step 3: Add MSW handler**

In `web/src/mocks/recruitmentHandlers.ts`, add to the `recruitmentHandlers` array:

```typescript
http.post('/api/recruitments/:id/close', ({ params }) => {
  const { id } = params
  const recruitment = recruitmentsById[id as string]
  if (!recruitment) {
    return HttpResponse.json(
      { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4', title: 'Not Found', status: 404 },
      { status: 404 },
    )
  }
  if (recruitment.status === 'Closed') {
    return HttpResponse.json(
      { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1', title: 'Recruitment is closed', status: 400 },
      { status: 400 },
    )
  }
  recruitment.status = 'Closed'
  recruitment.closedAt = new Date().toISOString()
  return HttpResponse.json(null, { status: 200 })
}),
```

**Step 4: Verify TypeScript compiles**

Run: `cd web && npx tsc --noEmit`
Expected: No errors.

**Step 5: Run existing tests**

Run: `cd web && npx vitest run`
Expected: All tests pass.

**Step 6: Commit**

```bash
git add web/src/lib/api/recruitments.ts \
  web/src/features/recruitments/hooks/useRecruitmentMutations.ts \
  web/src/mocks/recruitmentHandlers.ts
git commit -m "feat(2.5): add close API client, mutation hook, and MSW handler"
```

---

### Task 6: CloseRecruitmentDialog Component + Tests (TDD)

**Files:**
- Create: `web/src/features/recruitments/CloseRecruitmentDialog.test.tsx`
- Create: `web/src/features/recruitments/CloseRecruitmentDialog.tsx`

**Step 1: Write tests (RED)**

```typescript
// CloseRecruitmentDialog.test.tsx
import { describe, expect, it, vi } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '@/test-utils'
import { CloseRecruitmentDialog } from './CloseRecruitmentDialog'

const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('CloseRecruitmentDialog', () => {
  it('should render explanation text when open', () => {
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(screen.getByText(/lock the recruitment/i)).toBeInTheDocument()
    expect(screen.getByText(/retention period/i)).toBeInTheDocument()
  })

  it('should render close recruitment button', () => {
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(
      screen.getByRole('button', { name: /close recruitment/i }),
    ).toBeInTheDocument()
  })

  it('should render cancel button', () => {
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
  })

  it('should call API and close dialog on confirm', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={onOpenChange}
      />,
    )

    await user.click(screen.getByRole('button', { name: /close recruitment/i }))

    await waitFor(() => {
      expect(onOpenChange).toHaveBeenCalledWith(false)
    }, { timeout: 2000 })
  })

  it('should call onOpenChange(false) when cancel is clicked', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={onOpenChange}
      />,
    )

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(onOpenChange).toHaveBeenCalledWith(false)
  })
})
```

**Step 2: Run tests to verify they fail (RED)**

Run: `cd web && npx vitest run src/features/recruitments/CloseRecruitmentDialog.test.tsx`
Expected: FAIL (module not found)

**Step 3: Write implementation (GREEN)**

```typescript
// CloseRecruitmentDialog.tsx
import { useCloseRecruitment } from './hooks/useRecruitmentMutations'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'

interface CloseRecruitmentDialogProps {
  recruitmentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CloseRecruitmentDialog({
  recruitmentId,
  open,
  onOpenChange,
}: CloseRecruitmentDialogProps) {
  const closeMutation = useCloseRecruitment(recruitmentId)
  const toast = useAppToast()

  function handleConfirm() {
    closeMutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Recruitment closed')
        onOpenChange(false)
      },
      onError: (error) => {
        if (error instanceof ApiError) {
          toast.error(error.problemDetails.title)
        } else {
          toast.error('Failed to close recruitment')
        }
      },
    })
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Close Recruitment</AlertDialogTitle>
          <AlertDialogDescription>
            This will lock the recruitment from further changes. No edits can be
            made to candidates, workflow steps, or team members after closing.
            Data will be retained for the configured retention period before
            anonymization.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={closeMutation.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {closeMutation.isPending ? 'Closing...' : 'Close Recruitment'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
```

**Step 4: Run tests (GREEN)**

Run: `cd web && npx vitest run src/features/recruitments/CloseRecruitmentDialog.test.tsx`
Expected: PASS (5 tests)

**Step 5: Commit**

```bash
git add web/src/features/recruitments/CloseRecruitmentDialog.tsx \
  web/src/features/recruitments/CloseRecruitmentDialog.test.tsx
git commit -m "feat(2.5): add CloseRecruitmentDialog with confirmation and tests"
```

---

### Task 7: Wire Close Button into RecruitmentPage + Test

**Files:**
- Modify: `web/src/features/recruitments/pages/RecruitmentPage.tsx`
- Modify: `web/src/features/recruitments/pages/RecruitmentPage.test.tsx`

**Step 1: Add close button test (RED)**

Add to `RecruitmentPage.test.tsx`:

```typescript
it('renders close button for active recruitment', async () => {
  renderWithRoute(mockRecruitmentId)

  await waitFor(() => {
    expect(
      screen.getByRole('button', { name: /close recruitment/i }),
    ).toBeInTheDocument()
  })
})

it('does not render close button for closed recruitment', async () => {
  renderWithRoute(mockRecruitmentId2)

  await waitFor(() => {
    expect(screen.getByDisplayValue('Frontend Engineer')).toBeInTheDocument()
  })

  expect(
    screen.queryByRole('button', { name: /close recruitment/i }),
  ).not.toBeInTheDocument()
})
```

**Step 2: Run tests to verify they fail (RED)**

Run: `cd web && npx vitest run src/features/recruitments/pages/RecruitmentPage.test.tsx`
Expected: FAIL (button not found)

**Step 3: Implement (GREEN)**

In `RecruitmentPage.tsx`:

1. Add imports:
```typescript
import { useState } from 'react'
import { CloseRecruitmentDialog } from '../CloseRecruitmentDialog'
```

2. Add state inside the component:
```typescript
const [closeDialogOpen, setCloseDialogOpen] = useState(false)
```

3. Add close button in the header section (next to the Badge), wrapped in `{!isClosed && ...}`:
```typescript
<div className="flex items-center gap-2">
  {!isClosed && (
    <Button
      variant="destructive"
      onClick={() => setCloseDialogOpen(true)}
    >
      Close Recruitment
    </Button>
  )}
  <Badge variant={data.status === 'Active' ? 'default' : 'secondary'}>
    {data.status}
  </Badge>
</div>
```

4. Add dialog before closing `</div>`:
```typescript
<CloseRecruitmentDialog
  recruitmentId={data.id}
  open={closeDialogOpen}
  onOpenChange={setCloseDialogOpen}
/>
```

**Step 4: Run tests (GREEN)**

Run: `cd web && npx vitest run src/features/recruitments/pages/RecruitmentPage.test.tsx`
Expected: PASS (all tests including new ones)

**Step 5: Commit**

```bash
git add web/src/features/recruitments/pages/RecruitmentPage.tsx \
  web/src/features/recruitments/pages/RecruitmentPage.test.tsx
git commit -m "feat(2.5): wire close button and dialog into RecruitmentPage"
```

---

### Task 8: Final Verification and Cleanup

**Step 1: Run all backend tests**

Run: `dotnet test api/tests/Domain.UnitTests/Domain.UnitTests.csproj --verbosity quiet`
Expected: All pass (49 tests)

Run: `dotnet build api/tests/Application.UnitTests/Application.UnitTests.csproj --verbosity quiet`
Expected: Build succeeded. 0 errors.

Run: `dotnet build api/src/Web/Web.csproj --verbosity quiet`
Expected: Build succeeded. 0 errors.

**Step 2: Run all frontend checks**

Run: `cd web && npx tsc --noEmit`
Expected: 0 errors.

Run: `cd web && npx eslint src/`
Expected: 0 errors.

Run: `cd web && npx vitest run`
Expected: All tests pass.

**Step 3: Update sprint status**

Modify `_bmad-output/implementation-artifacts/sprint-status.yaml`:
Change `2-5-close-recruitment-read-only-view: ready-for-dev` to `2-5-close-recruitment-read-only-view: done`

**Step 4: Commit and notify**

```bash
git add _bmad-output/implementation-artifacts/sprint-status.yaml
git commit -m "chore(2.5): mark story 2.5 done in sprint status"
```

Send completion message to team lead.

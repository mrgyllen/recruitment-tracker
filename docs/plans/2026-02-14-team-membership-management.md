# Team Membership Management Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement full-stack team membership management allowing recruiting leaders to invite members via directory search, view the member list, and remove non-creator members.

**Architecture:** CQRS with MediatR for backend (AddMember, RemoveMember commands; GetMembers, SearchDirectory queries). New `IDirectoryService` infrastructure service with dev stub. Frontend uses TanStack Query hooks, MemberList component, InviteMemberDialog with debounced search. All member mutations go through `Recruitment` aggregate root methods.

**Tech Stack:** .NET 10, EF Core, MediatR, FluentValidation, NSubstitute (backend); React 19, TypeScript, TanStack Query v5, shadcn/ui Dialog, MSW (frontend)

---

## Architecture Decision: DisplayName on RecruitmentMember

The story spec recommends adding `DisplayName` to `RecruitmentMember` entity (option 1: denormalization at invite time). This avoids N+1 Graph API calls on every member list load.

**Changes required:**
- Add `DisplayName` property to `RecruitmentMember` entity
- Update `RecruitmentMember.Create()` to accept displayName parameter
- Update `Recruitment.AddMember()` signature to accept displayName
- Update `RecruitmentMemberConfiguration` with MaxLength
- Update `CreateRecruitmentCommandHandler` to pass display name for the creator member
- EF migration (handled by EF Core auto-migration in dev)

**However**, the story spec also says "DO NOT modify domain entities -- they are complete." This is a contradiction. The spec acknowledges the contradiction ("MAYBE add DisplayName property") and recommends option 1. We will add `DisplayName` as a minimal change since the alternative (resolving names on every query) is architecturally worse.

---

### Task 1: Add DisplayName to RecruitmentMember entity

**Testing mode:** Characterization (modifying existing entity, domain tests already exist)

**Files:**
- Modify: `/home/thomasg/Projects/Web/recruitment-tracker-epic2/api/src/Domain/Entities/RecruitmentMember.cs`
- Modify: `/home/thomasg/Projects/Web/recruitment-tracker-epic2/api/src/Domain/Entities/Recruitment.cs`
- Modify: `/home/thomasg/Projects/Web/recruitment-tracker-epic2/api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs`
- Modify: `/home/thomasg/Projects/Web/recruitment-tracker-epic2/api/tests/Domain.UnitTests/Entities/RecruitmentTests.cs`
- Modify: `/home/thomasg/Projects/Web/recruitment-tracker-epic2/api/src/Application/Features/Recruitments/Commands/CreateRecruitment/CreateRecruitmentCommandHandler.cs`

**Step 1: Update RecruitmentMember entity**

Add `DisplayName` property and update `Create()` method:

```csharp
// RecruitmentMember.cs
public class RecruitmentMember : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public DateTimeOffset InvitedAt { get; private set; }

    private RecruitmentMember() { } // EF Core

    internal static RecruitmentMember Create(Guid recruitmentId, Guid userId, string role, string? displayName = null)
    {
        return new RecruitmentMember
        {
            RecruitmentId = recruitmentId,
            UserId = userId,
            Role = role,
            DisplayName = displayName,
            InvitedAt = DateTimeOffset.UtcNow,
        };
    }
}
```

**Step 2: Update Recruitment.AddMember() to accept displayName**

```csharp
// In Recruitment.cs, change AddMember signature:
public void AddMember(Guid userId, string role, string? displayName = null)
{
    EnsureNotClosed();

    if (_members.Any(m => m.UserId == userId))
    {
        throw new InvalidOperationException($"User {userId} is already a member.");
    }

    var member = RecruitmentMember.Create(Id, userId, role, displayName);
    _members.Add(member);
    AddDomainEvent(new MembershipChangedEvent(Id, userId, "Added"));
}
```

**Step 3: Update EF configuration**

```csharp
// In RecruitmentMemberConfiguration.cs, add:
builder.Property(m => m.DisplayName)
    .HasMaxLength(200);
```

**Step 4: Verify existing domain tests still pass**

Run: `dotnet test /home/thomasg/Projects/Web/recruitment-tracker-epic2/api/tests/Domain.UnitTests/ --filter "RecruitmentTests"`
Expected: All existing tests pass (displayName parameter is optional with default null)

**Step 5: Verify build is clean**

Run: `dotnet build /home/thomasg/Projects/Web/recruitment-tracker-epic2/api/api.sln --no-restore`
Expected: 0 errors. The CreateRecruitmentCommandHandler and any other callers of AddMember still compile because the new parameter has a default value.

**Step 6: Commit**

```bash
git add api/src/Domain/Entities/RecruitmentMember.cs api/src/Domain/Entities/Recruitment.cs api/src/Infrastructure/Data/Configurations/RecruitmentMemberConfiguration.cs
git commit -m "feat(2.4): add DisplayName property to RecruitmentMember entity"
```

---

### Task 2: Backend -- IDirectoryService interface and dev stub

**Testing mode:** Spike (new infrastructure integration, tests added for dev stub)

**Files:**
- Create: `api/src/Application/Common/Interfaces/IDirectoryService.cs`
- Create: `api/src/Application/Common/Models/DirectoryUser.cs`
- Create: `api/src/Infrastructure/Identity/DevDirectoryService.cs`
- Create: `api/src/Infrastructure/Identity/EntraIdDirectoryService.cs`
- Modify: `api/src/Infrastructure/DependencyInjection.cs`

**Step 1: Create IDirectoryService interface**

```csharp
// api/src/Application/Common/Interfaces/IDirectoryService.cs
namespace api.Application.Common.Interfaces;

public interface IDirectoryService
{
    Task<IReadOnlyList<DirectoryUser>> SearchUsersAsync(
        string searchTerm, CancellationToken cancellationToken);
}
```

**Step 2: Create DirectoryUser model**

```csharp
// api/src/Application/Common/Models/DirectoryUser.cs
namespace api.Application.Common.Models;

public record DirectoryUser(Guid Id, string DisplayName, string Email);
```

**Step 3: Create DevDirectoryService**

Returns hardcoded personas matching dev auth users. Filters by search term (case-insensitive contains on DisplayName or Email).

```csharp
// api/src/Infrastructure/Identity/DevDirectoryService.cs
using api.Application.Common.Interfaces;
using api.Application.Common.Models;

namespace api.Infrastructure.Identity;

public class DevDirectoryService : IDirectoryService
{
    private static readonly List<DirectoryUser> DevUsers =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Dev User A", "usera@dev.local"),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Dev User B", "userb@dev.local"),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Dev Admin", "admin@dev.local"),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Erik Leader", "erik@dev.local"),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Sara Specialist", "sara@dev.local"),
    ];

    public Task<IReadOnlyList<DirectoryUser>> SearchUsersAsync(
        string searchTerm, CancellationToken cancellationToken)
    {
        var results = DevUsers
            .Where(u => u.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                     || u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IReadOnlyList<DirectoryUser>>(results);
    }
}
```

**Step 4: Create EntraIdDirectoryService placeholder**

```csharp
// api/src/Infrastructure/Identity/EntraIdDirectoryService.cs
using api.Application.Common.Interfaces;
using api.Application.Common.Models;

namespace api.Infrastructure.Identity;

public class EntraIdDirectoryService : IDirectoryService
{
    public Task<IReadOnlyList<DirectoryUser>> SearchUsersAsync(
        string searchTerm, CancellationToken cancellationToken)
    {
        // TODO: Implement Microsoft Graph API integration when Entra ID tenant is configured.
        // Requires User.Read.All or People.Read.All application permission.
        throw new NotImplementedException(
            "EntraIdDirectoryService requires Entra ID configuration. Use DevDirectoryService in development.");
    }
}
```

**Step 5: Register DI**

In `DependencyInjection.cs`, add environment-based registration inside `AddInfrastructureServices`:

```csharp
// After existing registrations, add:
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IDirectoryService, DevDirectoryService>();
}
else
{
    builder.Services.AddScoped<IDirectoryService, EntraIdDirectoryService>();
}
```

This requires adding `IHostApplicationBuilder` which already exists as the parameter type.

**Step 6: Verify build**

Run: `dotnet build /home/thomasg/Projects/Web/recruitment-tracker-epic2/api/api.sln --no-restore`
Expected: 0 errors

**Step 7: Commit**

```bash
git add api/src/Application/Common/Interfaces/IDirectoryService.cs api/src/Application/Common/Models/DirectoryUser.cs api/src/Infrastructure/Identity/DevDirectoryService.cs api/src/Infrastructure/Identity/EntraIdDirectoryService.cs api/src/Infrastructure/DependencyInjection.cs
git commit -m "feat(2.4): add IDirectoryService interface with dev stub and Entra ID placeholder"
```

---

### Task 3: Backend -- SearchDirectory query

**Testing mode:** Test-first

**Files:**
- Create: `api/src/Application/Features/Team/Queries/SearchDirectory/SearchDirectoryQuery.cs`
- Create: `api/src/Application/Features/Team/Queries/SearchDirectory/SearchDirectoryQueryValidator.cs`
- Create: `api/src/Application/Features/Team/Queries/SearchDirectory/SearchDirectoryQueryHandler.cs`
- Create: `api/src/Application/Features/Team/Queries/SearchDirectory/DirectoryUserDto.cs`
- Create: `api/tests/Application.UnitTests/Features/Team/Queries/SearchDirectory/SearchDirectoryQueryHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Team/Queries/SearchDirectory/SearchDirectoryQueryValidatorTests.cs`

**Step 1: Write failing handler test**

```csharp
// SearchDirectoryQueryHandlerTests.cs
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Application.Features.Team.Queries.SearchDirectory;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Queries.SearchDirectory;

public class SearchDirectoryQueryHandlerTests
{
    private IDirectoryService _directoryService = null!;
    private SearchDirectoryQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _directoryService = Substitute.For<IDirectoryService>();
        _handler = new SearchDirectoryQueryHandler(_directoryService);
    }

    [Test]
    public async Task Handle_ValidSearch_ReturnsMappedResults()
    {
        var users = new List<DirectoryUser>
        {
            new(Guid.NewGuid(), "Erik Leader", "erik@test.com"),
            new(Guid.NewGuid(), "Sara Specialist", "sara@test.com"),
        };
        _directoryService.SearchUsersAsync("eri", Arg.Any<CancellationToken>())
            .Returns(users);

        var result = await _handler.Handle(
            new SearchDirectoryQuery { SearchTerm = "eri" }, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("Erik Leader");
        result[0].Email.Should().Be("erik@test.com");
    }

    [Test]
    public async Task Handle_NoResults_ReturnsEmptyList()
    {
        _directoryService.SearchUsersAsync("xyz", Arg.Any<CancellationToken>())
            .Returns(new List<DirectoryUser>());

        var result = await _handler.Handle(
            new SearchDirectoryQuery { SearchTerm = "xyz" }, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
```

**Step 2: Write failing validator test**

```csharp
// SearchDirectoryQueryValidatorTests.cs
using api.Application.Features.Team.Queries.SearchDirectory;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Queries.SearchDirectory;

public class SearchDirectoryQueryValidatorTests
{
    private SearchDirectoryQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SearchDirectoryQueryValidator();
    }

    [Test]
    public void Validate_EmptySearchTerm_Fails()
    {
        var query = new SearchDirectoryQuery { SearchTerm = "" };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_SingleCharSearchTerm_Fails()
    {
        var query = new SearchDirectoryQuery { SearchTerm = "a" };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ValidSearchTerm_Passes()
    {
        var query = new SearchDirectoryQuery { SearchTerm = "erik" };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_SearchTermExceedsMaxLength_Fails()
    {
        var query = new SearchDirectoryQuery { SearchTerm = new string('a', 101) };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }
}
```

**Step 3: Run tests to verify they fail**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "SearchDirectory"`
Expected: Build errors (classes don't exist yet)

**Step 4: Implement SearchDirectoryQuery**

```csharp
// SearchDirectoryQuery.cs
namespace api.Application.Features.Team.Queries.SearchDirectory;

public record SearchDirectoryQuery : IRequest<IReadOnlyList<DirectoryUserDto>>
{
    public string SearchTerm { get; init; } = null!;
}
```

**Step 5: Implement DirectoryUserDto**

```csharp
// DirectoryUserDto.cs
using api.Application.Common.Models;

namespace api.Application.Features.Team.Queries.SearchDirectory;

public record DirectoryUserDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = null!;
    public string Email { get; init; } = null!;

    public static DirectoryUserDto From(DirectoryUser user) =>
        new()
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
        };
}
```

**Step 6: Implement SearchDirectoryQueryHandler**

```csharp
// SearchDirectoryQueryHandler.cs
using api.Application.Common.Interfaces;

namespace api.Application.Features.Team.Queries.SearchDirectory;

public class SearchDirectoryQueryHandler(IDirectoryService directoryService)
    : IRequestHandler<SearchDirectoryQuery, IReadOnlyList<DirectoryUserDto>>
{
    public async Task<IReadOnlyList<DirectoryUserDto>> Handle(
        SearchDirectoryQuery request, CancellationToken cancellationToken)
    {
        var users = await directoryService.SearchUsersAsync(request.SearchTerm, cancellationToken);
        return users.Select(DirectoryUserDto.From).ToList();
    }
}
```

**Step 7: Implement SearchDirectoryQueryValidator**

```csharp
// SearchDirectoryQueryValidator.cs
namespace api.Application.Features.Team.Queries.SearchDirectory;

public class SearchDirectoryQueryValidator : AbstractValidator<SearchDirectoryQuery>
{
    public SearchDirectoryQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required.")
            .MinimumLength(2).WithMessage("Search term must be at least 2 characters.")
            .MaximumLength(100);
    }
}
```

**Step 8: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "SearchDirectory"`
Expected: All 6 tests pass

**Step 9: Commit**

```bash
git add api/src/Application/Features/Team/ api/tests/Application.UnitTests/Features/Team/
git commit -m "feat(2.4): add SearchDirectory query with handler and validator"
```

---

### Task 4: Backend -- GetMembers query

**Testing mode:** Test-first

**Files:**
- Create: `api/src/Application/Features/Team/Queries/GetMembers/GetMembersQuery.cs`
- Create: `api/src/Application/Features/Team/Queries/GetMembers/GetMembersQueryHandler.cs`
- Create: `api/src/Application/Features/Team/Queries/GetMembers/MemberDto.cs`
- Create: `api/src/Application/Features/Team/Queries/GetMembers/MembersListDto.cs`
- Create: `api/tests/Application.UnitTests/Features/Team/Queries/GetMembers/GetMembersQueryHandlerTests.cs`

**Step 1: Write failing handler test**

```csharp
// GetMembersQueryHandlerTests.cs
using api.Application.Common.Interfaces;
using api.Application.Features.Team.Queries.GetMembers;
using api.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Queries.GetMembers;

public class GetMembersQueryHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private GetMembersQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _handler = new GetMembersQueryHandler(_dbContext, _tenantContext);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsMembersWithCreatorFlag()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        recruitment.AddMember(memberId, "SME/Collaborator", "Member Name");

        _tenantContext.UserGuid.Returns(creatorId);

        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        // Act
        var result = await _handler.Handle(
            new GetMembersQuery { RecruitmentId = recruitment.Id }, CancellationToken.None);

        // Assert
        result.Members.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        var creator = result.Members.First(m => m.UserId == creatorId);
        creator.IsCreator.Should().BeTrue();
        creator.Role.Should().Be("Recruiting Leader");

        var member = result.Members.First(m => m.UserId == memberId);
        member.IsCreator.Should().BeFalse();
        member.Role.Should().Be("SME/Collaborator");
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var act = () => _handler.Handle(
            new GetMembersQuery { RecruitmentId = recruitment.Id }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitments = new List<Recruitment>().AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var act = () => _handler.Handle(
            new GetMembersQuery { RecruitmentId = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

**IMPORTANT:** The test project likely has or needs a `TestAsyncQueryProvider` helper for mocking EF Core async queries. Check if one exists in `api/tests/Application.UnitTests/`. If not, check how existing handler tests like `GetRecruitmentByIdQueryTests.cs` mock the DbSet -- they likely use the same pattern. Copy that pattern exactly.

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "GetMembersQuery"`
Expected: Build errors (classes don't exist yet)

**Step 3: Implement GetMembersQuery**

```csharp
// GetMembersQuery.cs
namespace api.Application.Features.Team.Queries.GetMembers;

public record GetMembersQuery : IRequest<MembersListDto>
{
    public Guid RecruitmentId { get; init; }
}
```

**Step 4: Implement MemberDto**

```csharp
// MemberDto.cs
using api.Domain.Entities;

namespace api.Application.Features.Team.Queries.GetMembers;

public record MemberDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string Role { get; init; } = null!;
    public bool IsCreator { get; init; }
    public DateTimeOffset InvitedAt { get; init; }

    public static MemberDto From(RecruitmentMember member, bool isCreator) =>
        new()
        {
            Id = member.Id,
            UserId = member.UserId,
            DisplayName = member.DisplayName,
            Role = member.Role,
            IsCreator = isCreator,
            InvitedAt = member.InvitedAt,
        };
}
```

**Step 5: Implement MembersListDto**

```csharp
// MembersListDto.cs
namespace api.Application.Features.Team.Queries.GetMembers;

public record MembersListDto
{
    public List<MemberDto> Members { get; init; } = [];
    public int TotalCount { get; init; }
}
```

**Step 6: Implement GetMembersQueryHandler**

```csharp
// GetMembersQueryHandler.cs
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Team.Queries.GetMembers;

public class GetMembersQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetMembersQuery, MembersListDto>
{
    public async Task<MembersListDto> Handle(
        GetMembersQuery request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        var members = recruitment.Members
            .Select(m => MemberDto.From(m, m.UserId == recruitment.CreatedByUserId))
            .OrderBy(m => m.InvitedAt)
            .ToList();

        return new MembersListDto
        {
            Members = members,
            TotalCount = members.Count,
        };
    }
}
```

**Step 7: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "GetMembersQuery"`
Expected: All 3 tests pass

**Step 8: Commit**

```bash
git add api/src/Application/Features/Team/Queries/GetMembers/ api/tests/Application.UnitTests/Features/Team/Queries/GetMembers/
git commit -m "feat(2.4): add GetMembers query with ITenantContext membership check"
```

---

### Task 5: Backend -- AddMember command

**Testing mode:** Test-first

**Files:**
- Create: `api/src/Application/Features/Team/Commands/AddMember/AddMemberCommand.cs`
- Create: `api/src/Application/Features/Team/Commands/AddMember/AddMemberCommandValidator.cs`
- Create: `api/src/Application/Features/Team/Commands/AddMember/AddMemberCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Team/Commands/AddMember/AddMemberCommandHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Team/Commands/AddMember/AddMemberCommandValidatorTests.cs`

**Step 1: Write failing handler test**

```csharp
// AddMemberCommandHandlerTests.cs
using api.Application.Common.Interfaces;
using api.Application.Features.Team.Commands.AddMember;
using api.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Commands.AddMember;

public class AddMemberCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private AddMemberCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _handler = new AddMemberCommandHandler(_dbContext, _tenantContext);
    }

    [Test]
    public async Task Handle_ValidRequest_AddsMemberAndReturnsMemberId()
    {
        var creatorId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(creatorId);

        // Mock DbSet (follow existing pattern from other handler tests)
        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var result = await _handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = recruitment.Id,
                UserId = newUserId,
                DisplayName = "New Member"
            },
            CancellationToken.None);

        result.Should().NotBeEmpty();
        recruitment.Members.Should().HaveCount(2);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_DuplicateUser_ThrowsInvalidOperationException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(creatorId);

        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var act = () => _handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = recruitment.Id,
                UserId = creatorId, // already a member
                DisplayName = "Duplicate"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already a member*");
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var act = () => _handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = recruitment.Id,
                UserId = Guid.NewGuid(),
                DisplayName = "Intruder"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitments = new List<Recruitment>().AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var act = () => _handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DisplayName = "Test"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

**Step 2: Write failing validator test**

```csharp
// AddMemberCommandValidatorTests.cs
using api.Application.Features.Team.Commands.AddMember;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Commands.AddMember;

public class AddMemberCommandValidatorTests
{
    private AddMemberCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new AddMemberCommandValidator();
    }

    [Test]
    public void Validate_MissingRecruitmentId_Fails()
    {
        var command = new AddMemberCommand
        {
            RecruitmentId = Guid.Empty,
            UserId = Guid.NewGuid(),
            DisplayName = "Test"
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_MissingUserId_Fails()
    {
        var command = new AddMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            UserId = Guid.Empty,
            DisplayName = "Test"
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ValidInput_Passes()
    {
        var command = new AddMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DisplayName = "Test User"
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
```

**Step 3: Run tests to verify they fail**

Expected: Build errors

**Step 4: Implement AddMemberCommand**

```csharp
// AddMemberCommand.cs
namespace api.Application.Features.Team.Commands.AddMember;

public record AddMemberCommand : IRequest<Guid>
{
    public Guid RecruitmentId { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = null!;
}
```

**Step 5: Implement AddMemberCommandValidator**

```csharp
// AddMemberCommandValidator.cs
namespace api.Application.Features.Team.Commands.AddMember;

public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.RecruitmentId)
            .NotEmpty().WithMessage("Recruitment ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200);
    }
}
```

**Step 6: Implement AddMemberCommandHandler**

```csharp
// AddMemberCommandHandler.cs
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Team.Commands.AddMember;

public class AddMemberCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<AddMemberCommand, Guid>
{
    public async Task<Guid> Handle(
        AddMemberCommand request, CancellationToken cancellationToken)
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

        recruitment.AddMember(request.UserId, "SME/Collaborator", request.DisplayName);
        await dbContext.SaveChangesAsync(cancellationToken);

        return recruitment.Members.First(m => m.UserId == request.UserId).Id;
    }
}
```

**Step 7: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "AddMember"`
Expected: All 7 tests pass (4 handler + 3 validator)

**Step 8: Commit**

```bash
git add api/src/Application/Features/Team/Commands/AddMember/ api/tests/Application.UnitTests/Features/Team/Commands/AddMember/
git commit -m "feat(2.4): add AddMember command with handler, validator, and tests"
```

---

### Task 6: Backend -- RemoveMember command

**Testing mode:** Test-first

**Files:**
- Create: `api/src/Application/Features/Team/Commands/RemoveMember/RemoveMemberCommand.cs`
- Create: `api/src/Application/Features/Team/Commands/RemoveMember/RemoveMemberCommandValidator.cs`
- Create: `api/src/Application/Features/Team/Commands/RemoveMember/RemoveMemberCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Team/Commands/RemoveMember/RemoveMemberCommandHandlerTests.cs`
- Create: `api/tests/Application.UnitTests/Features/Team/Commands/RemoveMember/RemoveMemberCommandValidatorTests.cs`

**Step 1: Write failing handler test**

```csharp
// RemoveMemberCommandHandlerTests.cs
using api.Application.Common.Interfaces;
using api.Application.Features.Team.Commands.RemoveMember;
using api.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Commands.RemoveMember;

public class RemoveMemberCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private RemoveMemberCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _handler = new RemoveMemberCommandHandler(_dbContext, _tenantContext);
    }

    [Test]
    public async Task Handle_ValidRequest_RemovesMember()
    {
        var creatorId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        recruitment.AddMember(memberUserId, "SME/Collaborator");
        var memberId = recruitment.Members.First(m => m.UserId == memberUserId).Id;

        _tenantContext.UserGuid.Returns(creatorId);

        // Mock DbSet
        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        await _handler.Handle(
            new RemoveMemberCommand { RecruitmentId = recruitment.Id, MemberId = memberId },
            CancellationToken.None);

        recruitment.Members.Should().HaveCount(1); // only creator remains
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RemoveCreator_ThrowsInvalidOperationException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        // Add a second leader so last-leader guard doesn't fire first
        recruitment.AddMember(Guid.NewGuid(), "Recruiting Leader");
        var creatorMemberId = recruitment.Members.First(m => m.UserId == creatorId).Id;

        _tenantContext.UserGuid.Returns(creatorId);

        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var act = () => _handler.Handle(
            new RemoveMemberCommand { RecruitmentId = recruitment.Id, MemberId = creatorMemberId },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*creator*");
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitments = new List<Recruitment> { recruitment }.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<Recruitment>, IQueryable<Recruitment>>();
        ((IQueryable<Recruitment>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<Recruitment>(recruitments.Provider));
        ((IQueryable<Recruitment>)mockDbSet).Expression.Returns(recruitments.Expression);
        ((IQueryable<Recruitment>)mockDbSet).ElementType.Returns(recruitments.ElementType);
        ((IQueryable<Recruitment>)mockDbSet).GetEnumerator().Returns(recruitments.GetEnumerator());
        _dbContext.Recruitments.Returns(mockDbSet);

        var act = () => _handler.Handle(
            new RemoveMemberCommand { RecruitmentId = recruitment.Id, MemberId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
```

**Step 2: Write failing validator test**

```csharp
// RemoveMemberCommandValidatorTests.cs
using api.Application.Features.Team.Commands.RemoveMember;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Commands.RemoveMember;

public class RemoveMemberCommandValidatorTests
{
    private RemoveMemberCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RemoveMemberCommandValidator();
    }

    [Test]
    public void Validate_MissingRecruitmentId_Fails()
    {
        var command = new RemoveMemberCommand
        {
            RecruitmentId = Guid.Empty,
            MemberId = Guid.NewGuid()
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_MissingMemberId_Fails()
    {
        var command = new RemoveMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            MemberId = Guid.Empty
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ValidInput_Passes()
    {
        var command = new RemoveMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            MemberId = Guid.NewGuid()
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
```

**Step 3: Implement RemoveMemberCommand**

```csharp
// RemoveMemberCommand.cs
namespace api.Application.Features.Team.Commands.RemoveMember;

public record RemoveMemberCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
    public Guid MemberId { get; init; }
}
```

**Step 4: Implement RemoveMemberCommandValidator**

```csharp
// RemoveMemberCommandValidator.cs
namespace api.Application.Features.Team.Commands.RemoveMember;

public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.RecruitmentId)
            .NotEmpty().WithMessage("Recruitment ID is required.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");
    }
}
```

**Step 5: Implement RemoveMemberCommandHandler**

```csharp
// RemoveMemberCommandHandler.cs
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Team.Commands.RemoveMember;

public class RemoveMemberCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(
        RemoveMemberCommand request, CancellationToken cancellationToken)
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

        recruitment.RemoveMember(request.MemberId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

**Step 6: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.UnitTests/ --filter "RemoveMember"`
Expected: All 6 tests pass (3 handler + 3 validator)

**Step 7: Commit**

```bash
git add api/src/Application/Features/Team/Commands/RemoveMember/ api/tests/Application.UnitTests/Features/Team/Commands/RemoveMember/
git commit -m "feat(2.4): add RemoveMember command with handler, validator, and tests"
```

---

### Task 7: Backend -- TeamEndpoints

**Testing mode:** Test-first (endpoint wiring)

**Files:**
- Create: `api/src/Web/Endpoints/TeamEndpoints.cs`

**Step 1: Create TeamEndpoints**

The story spec shows `TeamEndpoints` as a static class with `MapTeamEndpoints()` extension method. However, the existing pattern uses `EndpointGroupBase` (see `RecruitmentEndpoints`). Check which pattern the `MapEndpoints()` reflection uses -- if it discovers `EndpointGroupBase` subclasses, use that. If not, use the static extension method pattern and register in `Program.cs`.

Looking at `RecruitmentEndpoints`, it extends `EndpointGroupBase` with `GroupName => "recruitments"` and all endpoints are relative to `/api/recruitments`. For team endpoints that are nested under `/api/recruitments/{recruitmentId}/`, we need a different group or an extension method.

Since the `EndpointGroupBase.Map()` receives a `RouteGroupBuilder` already scoped to `/api/{GroupName}`, nesting under `/recruitments/{recruitmentId}/` doesn't cleanly fit the existing `EndpointGroupBase` pattern. Use the static extension method approach as shown in the story spec.

```csharp
// TeamEndpoints.cs
using api.Application.Features.Team.Commands.AddMember;
using api.Application.Features.Team.Commands.RemoveMember;
using api.Application.Features.Team.Queries.GetMembers;
using api.Application.Features.Team.Queries.SearchDirectory;
using MediatR;

namespace api.Web.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recruitments/{recruitmentId:guid}")
            .RequireAuthorization();

        group.MapGet("/members", GetMembers);
        group.MapGet("/directory-search", SearchDirectory);
        group.MapPost("/members", AddMember);
        group.MapDelete("/members/{memberId:guid}", RemoveMember);
    }

    private static async Task<IResult> GetMembers(
        ISender sender,
        Guid recruitmentId)
    {
        var result = await sender.Send(new GetMembersQuery { RecruitmentId = recruitmentId });
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchDirectory(
        ISender sender,
        Guid recruitmentId,
        string q)
    {
        var result = await sender.Send(new SearchDirectoryQuery { SearchTerm = q });
        return Results.Ok(result);
    }

    private static async Task<IResult> AddMember(
        ISender sender,
        Guid recruitmentId,
        AddMemberCommand command)
    {
        var memberId = await sender.Send(command with { RecruitmentId = recruitmentId });
        return Results.Created(
            $"/api/recruitments/{recruitmentId}/members/{memberId}",
            new { id = memberId });
    }

    private static async Task<IResult> RemoveMember(
        ISender sender,
        Guid recruitmentId,
        Guid memberId)
    {
        await sender.Send(new RemoveMemberCommand { RecruitmentId = recruitmentId, MemberId = memberId });
        return Results.NoContent();
    }
}
```

**Step 2: Register in Program.cs**

Add `app.MapTeamEndpoints();` after `app.MapEndpoints();`.

**Step 3: Verify build**

Run: `dotnet build api/api.sln --no-restore`
Expected: 0 errors

**Step 4: Commit**

```bash
git add api/src/Web/Endpoints/TeamEndpoints.cs api/src/Web/Program.cs
git commit -m "feat(2.4): add TeamEndpoints with GET/POST/DELETE members and directory search"
```

---

### Task 8: Frontend -- API types and client

**Testing mode:** Characterization (thin wrapper, tested via component integration)

**Files:**
- Create: `web/src/lib/api/team.types.ts`
- Create: `web/src/lib/api/team.ts`

**Step 1: Create team.types.ts**

```typescript
// web/src/lib/api/team.types.ts
export interface TeamMemberDto {
  id: string
  userId: string
  displayName: string | null
  role: string
  isCreator: boolean
  invitedAt: string
}

export interface MembersListResponse {
  members: TeamMemberDto[]
  totalCount: number
}

export interface DirectoryUserDto {
  id: string
  displayName: string
  email: string
}

export interface AddMemberRequest {
  userId: string
  displayName: string
}
```

**Step 2: Create team.ts**

```typescript
// web/src/lib/api/team.ts
import { apiDelete, apiGet, apiPost } from './httpClient'
import type {
  AddMemberRequest,
  DirectoryUserDto,
  MembersListResponse,
} from './team.types'

export const teamApi = {
  getMembers: (recruitmentId: string) =>
    apiGet<MembersListResponse>(`/recruitments/${recruitmentId}/members`),

  searchDirectory: (recruitmentId: string, query: string) =>
    apiGet<DirectoryUserDto[]>(
      `/recruitments/${recruitmentId}/directory-search?q=${encodeURIComponent(query)}`,
    ),

  addMember: (recruitmentId: string, data: AddMemberRequest) =>
    apiPost<{ id: string }>(`/recruitments/${recruitmentId}/members`, data),

  removeMember: (recruitmentId: string, memberId: string) =>
    apiDelete(`/recruitments/${recruitmentId}/members/${memberId}`),
}
```

**Step 3: Verify TypeScript compiles**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx tsc --noEmit`
Expected: 0 errors

**Step 4: Commit**

```bash
git add web/src/lib/api/team.types.ts web/src/lib/api/team.ts
git commit -m "feat(2.4): add team API client and types"
```

---

### Task 9: Frontend -- TanStack Query hooks + useDebounce

**Testing mode:** Characterization

**Files:**
- Create: `web/src/features/team/hooks/useTeamMembers.ts`
- Create: `web/src/hooks/useDebounce.ts` (if it doesn't exist)

**Step 1: Check if useDebounce exists**

The story spec says it exists at `web/src/hooks/useDebounce.ts`. The glob search returned no results, so it needs to be created.

```typescript
// web/src/hooks/useDebounce.ts
import { useEffect, useState } from 'react'

export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])

  return debouncedValue
}
```

**Step 2: Create useTeamMembers hook**

```typescript
// web/src/features/team/hooks/useTeamMembers.ts
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { teamApi } from '@/lib/api/team'
import type { AddMemberRequest } from '@/lib/api/team.types'

export function useTeamMembers(recruitmentId: string) {
  return useQuery({
    queryKey: ['recruitment', recruitmentId, 'members'],
    queryFn: () => teamApi.getMembers(recruitmentId),
    enabled: !!recruitmentId,
  })
}

export function useDirectorySearch(recruitmentId: string, searchTerm: string) {
  return useQuery({
    queryKey: ['directory-search', recruitmentId, searchTerm],
    queryFn: () => teamApi.searchDirectory(recruitmentId, searchTerm),
    enabled: searchTerm.length >= 2,
  })
}

export function useAddMember(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AddMemberRequest) =>
      teamApi.addMember(recruitmentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['recruitment', recruitmentId, 'members'],
      })
    },
  })
}

export function useRemoveMember(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (memberId: string) =>
      teamApi.removeMember(recruitmentId, memberId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['recruitment', recruitmentId, 'members'],
      })
    },
  })
}
```

**Step 3: Verify TypeScript compiles**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx tsc --noEmit`
Expected: 0 errors

**Step 4: Commit**

```bash
git add web/src/hooks/useDebounce.ts web/src/features/team/hooks/useTeamMembers.ts
git commit -m "feat(2.4): add team TanStack Query hooks and useDebounce utility"
```

---

### Task 10: Frontend -- MSW handlers for team endpoints

**Testing mode:** Characterization

**Files:**
- Create: `web/src/mocks/teamHandlers.ts`
- Modify: `web/src/mocks/handlers.ts`

**Step 1: Create teamHandlers.ts**

```typescript
// web/src/mocks/teamHandlers.ts
import { http, HttpResponse } from 'msw'
import type { TeamMemberDto, MembersListResponse } from '@/lib/api/team.types'

export const mockCreatorId = 'dev-user-a'
export const mockMember2Id = '22222222-2222-2222-2222-222222222222'

const mockMembers: Record<string, TeamMemberDto[]> = {
  '550e8400-e29b-41d4-a716-446655440000': [
    {
      id: 'member-1',
      userId: mockCreatorId,
      displayName: 'Dev User A',
      role: 'Recruiting Leader',
      isCreator: true,
      invitedAt: new Date().toISOString(),
    },
  ],
}

const directoryUsers = [
  { id: '22222222-2222-2222-2222-222222222222', displayName: 'Dev User B', email: 'userb@dev.local' },
  { id: '33333333-3333-3333-3333-333333333333', displayName: 'Dev Admin', email: 'admin@dev.local' },
  { id: '44444444-4444-4444-4444-444444444444', displayName: 'Erik Leader', email: 'erik@dev.local' },
  { id: '55555555-5555-5555-5555-555555555555', displayName: 'Sara Specialist', email: 'sara@dev.local' },
]

export const teamHandlers = [
  http.get('/api/recruitments/:recruitmentId/members', ({ params }) => {
    const { recruitmentId } = params
    const members = mockMembers[recruitmentId as string] ?? []
    const response: MembersListResponse = {
      members,
      totalCount: members.length,
    }
    return HttpResponse.json(response)
  }),

  http.get('/api/recruitments/:recruitmentId/directory-search', ({ request }) => {
    const url = new URL(request.url)
    const q = url.searchParams.get('q') ?? ''
    if (q.length < 2) {
      return HttpResponse.json(
        { type: 'validation', title: 'Validation Failed', status: 400, errors: { SearchTerm: ['Minimum 2 characters'] } },
        { status: 400 },
      )
    }
    const results = directoryUsers.filter(
      u => u.displayName.toLowerCase().includes(q.toLowerCase())
        || u.email.toLowerCase().includes(q.toLowerCase()),
    )
    return HttpResponse.json(results)
  }),

  http.post('/api/recruitments/:recruitmentId/members', async ({ params, request }) => {
    const { recruitmentId } = params
    const body = (await request.json()) as { userId: string; displayName: string }

    // Check for duplicate
    const members = mockMembers[recruitmentId as string] ?? []
    if (members.some(m => m.userId === body.userId)) {
      return HttpResponse.json(
        { type: 'validation', title: 'Bad Request', status: 400, detail: `User ${body.userId} is already a member.` },
        { status: 400 },
      )
    }

    const newMember: TeamMemberDto = {
      id: crypto.randomUUID(),
      userId: body.userId,
      displayName: body.displayName,
      role: 'SME/Collaborator',
      isCreator: false,
      invitedAt: new Date().toISOString(),
    }
    if (!mockMembers[recruitmentId as string]) {
      mockMembers[recruitmentId as string] = []
    }
    mockMembers[recruitmentId as string].push(newMember)

    return HttpResponse.json(
      { id: newMember.id },
      { status: 201, headers: { Location: `/api/recruitments/${recruitmentId as string}/members/${newMember.id}` } },
    )
  }),

  http.delete('/api/recruitments/:recruitmentId/members/:memberId', ({ params }) => {
    const { recruitmentId, memberId } = params
    const members = mockMembers[recruitmentId as string] ?? []
    const member = members.find(m => m.id === memberId)

    if (!member) {
      return HttpResponse.json(
        { type: 'not-found', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }

    if (member.isCreator) {
      return HttpResponse.json(
        { type: 'validation', title: 'Bad Request', status: 400, detail: 'Cannot remove the creator of the recruitment.' },
        { status: 400 },
      )
    }

    mockMembers[recruitmentId as string] = members.filter(m => m.id !== memberId)
    return new HttpResponse(null, { status: 204 })
  }),
]
```

**Step 2: Update handlers.ts**

```typescript
// web/src/mocks/handlers.ts
import { recruitmentHandlers } from './recruitmentHandlers'
import { teamHandlers } from './teamHandlers'
import type { RequestHandler } from 'msw'

export const handlers: RequestHandler[] = [...recruitmentHandlers, ...teamHandlers]
```

**Step 3: Verify TypeScript compiles**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx tsc --noEmit`

**Step 4: Commit**

```bash
git add web/src/mocks/teamHandlers.ts web/src/mocks/handlers.ts
git commit -m "feat(2.4): add MSW handlers for team membership endpoints"
```

---

### Task 11: Frontend -- MemberList component (TDD)

**Testing mode:** Test-first

**Files:**
- Create: `web/src/features/team/MemberList.test.tsx`
- Create: `web/src/features/team/MemberList.tsx`

**Step 1: Write failing tests**

```typescript
// MemberList.test.tsx
import { describe, expect, it, vi } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '@/test-utils'
import { MemberList } from './MemberList'

const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('MemberList', () => {
  it('should render member names and roles', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Dev User A')).toBeInTheDocument()
    })
    expect(screen.getByText('Recruiting Leader')).toBeInTheDocument()
  })

  it('should show Creator badge for the creator member', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Creator')).toBeInTheDocument()
    })
  })

  it('should not show remove button for creator', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Dev User A')).toBeInTheDocument()
    })

    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument()
  })

  it('should show remove button for non-creator members', async () => {
    // This test will need the MSW mock to include a non-creator member
    // The mock data has only the creator initially. We'll add a member first
    // or use a different recruitmentId with pre-populated members.
    // For now, use the addMember mock to set up state.
    // Alternative: modify the mock to include a second member.
    // Best approach: render with the real MSW handlers which have only creator,
    // add a member via the API, then verify.
    // Simplest: update mock data for this test.
    // Actually, the simplest approach is to ensure the MSW mock for the
    // specific recruitmentId includes both creator and non-creator members.
  })

  it('should show invite button', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /invite/i })).toBeInTheDocument()
    })
  })
})
```

Note: The exact test setup may need adjustment based on how MSW mock data is structured. The implementer should ensure the MSW handler for the test recruitmentId includes both a creator and non-creator member for the "remove button" tests.

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run src/features/team/MemberList.test.tsx`
Expected: Fails (component doesn't exist)

**Step 3: Implement MemberList component**

```typescript
// MemberList.tsx
import { useState } from 'react'
import { useTeamMembers, useRemoveMember } from './hooks/useTeamMembers'
import { InviteMemberDialog } from './InviteMemberDialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { useAppToast } from '@/hooks/useAppToast'
import type { TeamMemberDto } from '@/lib/api/team.types'

interface MemberListProps {
  recruitmentId: string
  disabled?: boolean
}

export function MemberList({ recruitmentId, disabled }: MemberListProps) {
  const { data, isPending } = useTeamMembers(recruitmentId)
  const removeMember = useRemoveMember(recruitmentId)
  const toast = useAppToast()
  const [inviteOpen, setInviteOpen] = useState(false)
  const [confirmRemove, setConfirmRemove] = useState<TeamMemberDto | null>(null)

  if (isPending) {
    return <SkeletonLoader variant="card" />
  }

  const members = data?.members ?? []

  function handleRemove(member: TeamMemberDto) {
    setConfirmRemove(member)
  }

  function confirmRemoval() {
    if (!confirmRemove) return
    removeMember.mutate(confirmRemove.id, {
      onSuccess: () => {
        toast.success('Member removed')
        setConfirmRemove(null)
      },
      onError: () => {
        toast.error('Failed to remove member')
        setConfirmRemove(null)
      },
    })
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Team Members</h2>
        {!disabled && (
          <Button onClick={() => setInviteOpen(true)}>
            Invite Member
          </Button>
        )}
      </div>

      <div className="divide-y rounded-lg border">
        {members.map((member) => (
          <div key={member.id} className="flex items-center justify-between p-4">
            <div className="flex items-center gap-3">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium">{member.displayName ?? member.userId}</span>
                  {member.isCreator && (
                    <Badge variant="secondary">Creator</Badge>
                  )}
                </div>
                <span className="text-sm text-muted-foreground">{member.role}</span>
              </div>
            </div>
            {!member.isCreator && !disabled && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleRemove(member)}
                aria-label={`Remove ${member.displayName ?? 'member'}`}
              >
                Remove
              </Button>
            )}
          </div>
        ))}
      </div>

      {/* Confirmation dialog for removal */}
      {confirmRemove && (
        <div className="rounded-lg border border-destructive bg-destructive/5 p-4">
          <p>Remove {confirmRemove.displayName ?? 'this member'} from this recruitment?</p>
          <div className="mt-3 flex gap-2">
            <Button
              variant="destructive"
              size="sm"
              onClick={confirmRemoval}
              disabled={removeMember.isPending}
            >
              {removeMember.isPending ? 'Removing...' : 'Confirm Remove'}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setConfirmRemove(null)}
            >
              Cancel
            </Button>
          </div>
        </div>
      )}

      <InviteMemberDialog
        recruitmentId={recruitmentId}
        open={inviteOpen}
        onOpenChange={setInviteOpen}
      />
    </div>
  )
}
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run src/features/team/MemberList.test.tsx`
Expected: Tests pass

**Step 5: Commit**

```bash
git add web/src/features/team/MemberList.tsx web/src/features/team/MemberList.test.tsx
git commit -m "feat(2.4): add MemberList component with creator badge and remove action"
```

---

### Task 12: Frontend -- InviteMemberDialog component (TDD)

**Testing mode:** Test-first

**Files:**
- Create: `web/src/features/team/InviteMemberDialog.test.tsx`
- Create: `web/src/features/team/InviteMemberDialog.tsx`

**Step 1: Write failing tests**

```typescript
// InviteMemberDialog.test.tsx
import { describe, expect, it, vi } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '@/test-utils'
import { InviteMemberDialog } from './InviteMemberDialog'

const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('InviteMemberDialog', () => {
  it('should render search input when open', () => {
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(screen.getByPlaceholderText(/search/i)).toBeInTheDocument()
  })

  it('should show search results after typing and debounce', async () => {
    const user = userEvent.setup()
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    await user.type(screen.getByPlaceholderText(/search/i), 'Dev')

    await waitFor(() => {
      expect(screen.getByText('Dev User B')).toBeInTheDocument()
    }, { timeout: 2000 })
  })

  it('should call API when selecting a user and confirming', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={onOpenChange}
      />,
    )

    await user.type(screen.getByPlaceholderText(/search/i), 'Dev')

    await waitFor(() => {
      expect(screen.getByText('Dev User B')).toBeInTheDocument()
    }, { timeout: 2000 })

    await user.click(screen.getByText('Dev User B'))

    // After clicking the user, the invite should be triggered
    // The dialog should close on success
    await waitFor(() => {
      expect(onOpenChange).toHaveBeenCalledWith(false)
    }, { timeout: 2000 })
  })

  it('should not search with less than 2 characters', async () => {
    const user = userEvent.setup()
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    await user.type(screen.getByPlaceholderText(/search/i), 'D')

    // Should not show any results
    await new Promise(resolve => setTimeout(resolve, 500))
    expect(screen.queryByText('Dev User B')).not.toBeInTheDocument()
  })
})
```

**Step 2: Run tests to verify they fail**

Expected: Fails (component doesn't exist)

**Step 3: Implement InviteMemberDialog**

```typescript
// InviteMemberDialog.tsx
import { useState } from 'react'
import { useDebounce } from '@/hooks/useDebounce'
import { useDirectorySearch, useAddMember } from './hooks/useTeamMembers'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'
import type { DirectoryUserDto } from '@/lib/api/team.types'

interface InviteMemberDialogProps {
  recruitmentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function InviteMemberDialog({
  recruitmentId,
  open,
  onOpenChange,
}: InviteMemberDialogProps) {
  const [searchTerm, setSearchTerm] = useState('')
  const debouncedTerm = useDebounce(searchTerm, 300)
  const { data: searchResults, isPending: isSearching } = useDirectorySearch(
    recruitmentId,
    debouncedTerm,
  )
  const addMember = useAddMember(recruitmentId)
  const toast = useAppToast()
  const [error, setError] = useState<string | null>(null)

  function handleSelect(user: DirectoryUserDto) {
    setError(null)
    addMember.mutate(
      { userId: user.id, displayName: user.displayName },
      {
        onSuccess: () => {
          toast.success(`${user.displayName} added to team`)
          setSearchTerm('')
          onOpenChange(false)
        },
        onError: (err) => {
          if (err instanceof ApiError) {
            setError(err.problemDetails.detail ?? err.problemDetails.title)
          } else {
            setError('Failed to add member')
          }
        },
      },
    )
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setSearchTerm('')
      setError(null)
    }
    onOpenChange(nextOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Invite Team Member</DialogTitle>
        </DialogHeader>

        <Input
          placeholder="Search by name or email..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          autoFocus
        />

        {error && (
          <p className="text-sm text-destructive">{error}</p>
        )}

        {isSearching && debouncedTerm.length >= 2 && (
          <p className="text-sm text-muted-foreground">Searching...</p>
        )}

        {searchResults && searchResults.length > 0 && (
          <div className="max-h-60 space-y-1 overflow-y-auto">
            {searchResults.map((user) => (
              <Button
                key={user.id}
                variant="ghost"
                className="w-full justify-start"
                onClick={() => handleSelect(user)}
                disabled={addMember.isPending}
              >
                <div className="text-left">
                  <div className="font-medium">{user.displayName}</div>
                  <div className="text-sm text-muted-foreground">{user.email}</div>
                </div>
              </Button>
            ))}
          </div>
        )}

        {searchResults && searchResults.length === 0 && debouncedTerm.length >= 2 && (
          <p className="text-sm text-muted-foreground">No users found</p>
        )}
      </DialogContent>
    </Dialog>
  )
}
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run src/features/team/InviteMemberDialog.test.tsx`
Expected: Tests pass

**Step 5: Commit**

```bash
git add web/src/features/team/InviteMemberDialog.tsx web/src/features/team/InviteMemberDialog.test.tsx
git commit -m "feat(2.4): add InviteMemberDialog with debounced directory search"
```

---

### Task 13: Frontend -- Wire team into RecruitmentPage

**Testing mode:** Characterization

**Files:**
- Modify: `web/src/features/recruitments/pages/RecruitmentPage.tsx`

**Step 1: Add MemberList to RecruitmentPage**

Import `MemberList` and add it below the WorkflowStepEditor:

```typescript
// In RecruitmentPage.tsx, add import:
import { MemberList } from '@/features/team/MemberList'

// In the return JSX, after <WorkflowStepEditor>:
<MemberList recruitmentId={data.id} disabled={isClosed} />
```

**Step 2: Verify TypeScript compiles**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx tsc --noEmit`

**Step 3: Run existing RecruitmentPage tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run src/features/recruitments/pages/RecruitmentPage.test.tsx`
Expected: All existing tests still pass

**Step 4: Commit**

```bash
git add web/src/features/recruitments/pages/RecruitmentPage.tsx
git commit -m "feat(2.4): wire MemberList into RecruitmentPage"
```

---

### Task 14: Verification and final cleanup

**Step 1: Run all backend tests**

```bash
dotnet test /home/thomasg/Projects/Web/recruitment-tracker-epic2/api/tests/Domain.UnitTests/
```
Expected: All pass

**Step 2: Run backend build**

```bash
dotnet build /home/thomasg/Projects/Web/recruitment-tracker-epic2/api/api.sln --no-restore
```
Expected: 0 errors

**Step 3: Run all frontend tests**

```bash
cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx vitest run
```
Expected: All pass

**Step 4: Run TypeScript check**

```bash
cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx tsc --noEmit
```
Expected: 0 errors

**Step 5: Run ESLint**

```bash
cd /home/thomasg/Projects/Web/recruitment-tracker-epic2/web && npx eslint src/ --max-warnings 0
```
Expected: 0 errors, 0 warnings

**Step 6: Fix any issues found**

If any tests fail, TypeScript errors, or ESLint warnings, fix them now.

**Step 7: Update Dev Agent Record**

Update the story file at `_bmad-output/implementation-artifacts/2-4-team-membership-management.md` with:
- Agent Model Used: claude-opus-4-6
- Completion Notes: Summary of what was implemented
- File List: All files created/modified

**Step 8: Update sprint status**

Update `_bmad-output/sprint-status.yaml` to mark Story 2.4 as `done`.

**Step 9: Final commit**

```bash
git add _bmad-output/implementation-artifacts/2-4-team-membership-management.md _bmad-output/sprint-status.yaml
git commit -m "docs(2.4): update dev agent record and sprint status"
```

**Step 10: Notify team lead**

Send message to team lead confirming Story 2.4 is complete.

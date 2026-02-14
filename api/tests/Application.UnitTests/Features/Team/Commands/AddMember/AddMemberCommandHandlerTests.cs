using api.Application.Common.Interfaces;
using api.Application.Features.Team.Commands.AddMember;
using api.Domain.Entities;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Team.Commands.AddMember;

[TestFixture]
public class AddMemberCommandHandlerTests
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
    public async Task Handle_ValidRequest_AddsMemberAndReturnsMemberId()
    {
        var creatorId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(creatorId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddMemberCommandHandler(_dbContext, _tenantContext);
        var result = await handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = recruitment.Id,
                UserId = newUserId,
                DisplayName = "New Member",
            },
            CancellationToken.None);

        result.Should().NotBeEmpty();
        recruitment.Members.Should().HaveCount(2);
        recruitment.Members.First(m => m.UserId == newUserId).DisplayName.Should().Be("New Member");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_DuplicateUser_ThrowsDomainRuleViolationException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(creatorId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddMemberCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = recruitment.Id,
                UserId = creatorId, // already a member
                DisplayName = "Duplicate",
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleViolationException>()
            .WithMessage("*already a member*");
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(nonMemberId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddMemberCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = recruitment.Id,
                UserId = Guid.NewGuid(),
                DisplayName = "Intruder",
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddMemberCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new AddMemberCommand
            {
                RecruitmentId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DisplayName = "Test",
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

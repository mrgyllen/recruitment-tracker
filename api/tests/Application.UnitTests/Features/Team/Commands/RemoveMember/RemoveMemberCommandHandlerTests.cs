using api.Application.Common.Interfaces;
using api.Application.Features.Team.Commands.RemoveMember;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Team.Commands.RemoveMember;

[TestFixture]
public class RemoveMemberCommandHandlerTests
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
    public async Task Handle_ValidRequest_RemovesMember()
    {
        var creatorId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        recruitment.AddMember(memberUserId, "SME/Collaborator");
        var memberId = recruitment.Members.First(m => m.UserId == memberUserId).Id;

        _tenantContext.UserGuid.Returns(creatorId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveMemberCommandHandler(_dbContext, _tenantContext);
        await handler.Handle(
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
        // Add a second leader so the last-leader guard doesn't fire first
        recruitment.AddMember(Guid.NewGuid(), "Recruiting Leader");
        var creatorMemberId = recruitment.Members.First(m => m.UserId == creatorId).Id;

        _tenantContext.UserGuid.Returns(creatorId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveMemberCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
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

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveMemberCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new RemoveMemberCommand { RecruitmentId = recruitment.Id, MemberId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveMemberCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new RemoveMemberCommand { RecruitmentId = Guid.NewGuid(), MemberId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

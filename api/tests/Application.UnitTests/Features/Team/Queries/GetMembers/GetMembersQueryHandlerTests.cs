using api.Application.Common.Interfaces;
using api.Application.Features.Team.Queries.GetMembers;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Team.Queries.GetMembers;

[TestFixture]
public class GetMembersQueryHandlerTests
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
    public async Task Handle_ValidRequest_ReturnsMembersWithCreatorFlag()
    {
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        recruitment.AddMember(memberId, "SME/Collaborator", "Member Name");

        _tenantContext.UserGuid.Returns(creatorId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetMembersQueryHandler(_dbContext, _tenantContext);
        var result = await handler.Handle(
            new GetMembersQuery { RecruitmentId = recruitment.Id }, CancellationToken.None);

        result.Members.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        var creator = result.Members.First(m => m.UserId == creatorId);
        creator.IsCreator.Should().BeTrue();
        creator.Role.Should().Be("Recruiting Leader");

        var member = result.Members.First(m => m.UserId == memberId);
        member.IsCreator.Should().BeFalse();
        member.Role.Should().Be("SME/Collaborator");
        member.DisplayName.Should().Be("Member Name");
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

        var handler = new GetMembersQueryHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new GetMembersQuery { RecruitmentId = recruitment.Id }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetMembersQueryHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new GetMembersQuery { RecruitmentId = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

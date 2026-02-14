using api.Application.Common.Interfaces;
using api.Application.Features.Candidates.Queries.GetCandidates;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Candidates.Queries.GetCandidates;

[TestFixture]
public class GetCandidatesQueryHandlerTests
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
    public async Task Handle_ValidRequest_ReturnsPaginatedCandidates()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidate1 = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        var candidate2 = Candidate.Create(
            recruitment.Id, "Bob", "bob@example.com", null, null, DateTimeOffset.UtcNow);
        var candidateMockSet = new List<Candidate> { candidate1, candidate2 }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            Page = 1,
            PageSize = 50,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
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

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
        };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitmentMockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

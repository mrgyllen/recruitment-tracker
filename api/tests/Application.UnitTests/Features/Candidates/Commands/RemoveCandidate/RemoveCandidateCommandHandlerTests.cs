using api.Application.Common.Interfaces;
using api.Application.Features.Candidates.Commands.RemoveCandidate;
using api.Domain.Entities;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Candidates.Commands.RemoveCandidate;

[TestFixture]
public class RemoveCandidateCommandHandlerTests
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
    public async Task Handle_ValidRequest_RemovesCandidate()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidate = Candidate.Create(
            recruitment.Id, "Jane Doe", "jane@example.com", null, null, DateTimeOffset.UtcNow);
        var candidateMockSet = new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new RemoveCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            CandidateId = candidate.Id,
        };

        await handler.Handle(command, CancellationToken.None);

        _dbContext.Candidates.Received(1).Remove(Arg.Is<Candidate>(c => c.Id == candidate.Id));
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_CandidateNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new RemoveCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            CandidateId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
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

        var handler = new RemoveCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            CandidateId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<RecruitmentClosedException>();
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

        var handler = new RemoveCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            CandidateId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

using api.Application.Common.Interfaces;
using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Candidates.Commands.CreateCandidate;

[TestFixture]
public class CreateCandidateCommandHandlerTests
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
    public async Task Handle_ValidRequest_CreatesCandidateAndReturnsId()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test Recruitment", null, userId);
        recruitment.AddStep("Screening", 1);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new CreateCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new CreateCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            FullName = "Jane Doe",
            Email = "jane@example.com",
            PhoneNumber = "+1234567890",
            Location = "New York",
            DateApplied = DateTimeOffset.UtcNow,
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _dbContext.Candidates.Received(1).Add(Arg.Is<Candidate>(c =>
            c.Email == "jane@example.com" && c.FullName == "Jane Doe"));
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_DuplicateEmail_ThrowsDuplicateCandidateException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var existingCandidate = Candidate.Create(
            recruitment.Id, "Existing", "jane@example.com", null, null, DateTimeOffset.UtcNow);
        var candidateMockSet = new List<Candidate> { existingCandidate }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new CreateCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new CreateCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            FullName = "Jane Doe",
            Email = "jane@example.com",
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateCandidateException>();
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

        var handler = new CreateCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new CreateCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            FullName = "Jane Doe",
            Email = "jane@example.com",
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

        var handler = new CreateCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new CreateCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            FullName = "Jane Doe",
            Email = "jane@example.com",
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitmentMockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new CreateCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "jane@example.com",
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NoDateApplied_DefaultsToUtcNow()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var before = DateTimeOffset.UtcNow;
        var handler = new CreateCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new CreateCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            FullName = "Jane Doe",
            Email = "jane@example.com",
            DateApplied = null,
        };

        await handler.Handle(command, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        _dbContext.Candidates.Received(1).Add(Arg.Is<Candidate>(c =>
            c.DateApplied >= before && c.DateApplied <= after));
    }

    [Test]
    public async Task Handle_WithWorkflowSteps_PlacesCandidateAtFirstStep()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Second", 2);
        recruitment.AddStep("First", 1);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new CreateCandidateCommandHandler(_dbContext, _tenantContext);
        var command = new CreateCandidateCommand
        {
            RecruitmentId = recruitment.Id,
            FullName = "Jane Doe",
            Email = "jane@example.com",
        };

        await handler.Handle(command, CancellationToken.None);

        var firstStep = recruitment.Steps.OrderBy(s => s.Order).First();
        _dbContext.Candidates.Received(1).Add(Arg.Is<Candidate>(c =>
            c.Outcomes.Any(o => o.WorkflowStepId == firstStep.Id
                && o.Status == OutcomeStatus.NotStarted)));
    }
}

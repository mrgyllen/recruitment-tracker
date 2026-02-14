using api.Application.Common.Interfaces;
using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Screening.Commands.RecordOutcome;

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

    private void SetupRecruitmentDbSet(params Recruitment[] recruitments)
    {
        var mockSet = recruitments.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);
    }

    private void SetupCandidateDbSet(params Candidate[] candidates)
    {
        var mockSet = candidates.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(mockSet);
    }

    [Test]
    public async Task Handle_ValidOutcome_RecordsAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var (recruitment, candidate, steps) = SetupTestData(userId);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate);

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(
            recruitment.Id, candidate.Id, steps[0].Id, OutcomeStatus.Pass, "Good");

        var result = await handler.Handle(command, CancellationToken.None);

        result.CandidateId.Should().Be(candidate.Id);
        result.Outcome.Should().Be(OutcomeStatus.Pass);
        result.Reason.Should().Be("Good");
        result.NewCurrentStepId.Should().Be(steps[1].Id);
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

        SetupRecruitmentDbSet(recruitment);

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(
            recruitment.Id, candidate.Id, steps[0].Id, OutcomeStatus.Pass, null);

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

        SetupRecruitmentDbSet(recruitment);

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(
            recruitment.Id, candidate.Id, steps[0].Id, OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        SetupRecruitmentDbSet();

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_StepNotInRecruitment_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var (recruitment, candidate, _) = SetupTestData(userId);

        SetupRecruitmentDbSet(recruitment);

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(
            recruitment.Id, candidate.Id, Guid.NewGuid(), OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_CandidateNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);
        var (recruitment, _, steps) = SetupTestData(userId);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet();

        var handler = new RecordOutcomeCommandHandler(_dbContext, _tenantContext);
        var command = new RecordOutcomeCommand(
            recruitment.Id, Guid.NewGuid(), steps[0].Id, OutcomeStatus.Pass, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

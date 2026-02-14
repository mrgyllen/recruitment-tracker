using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Application.Features.Recruitments.Queries.GetRecruitmentOverview;
using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Queries.GetRecruitmentOverview;

[TestFixture]
public class GetRecruitmentOverviewQueryHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private IOptions<OverviewSettings> _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _settings = Options.Create(new OverviewSettings { StaleDays = 5 });
    }

    private GetRecruitmentOverviewQueryHandler CreateHandler() =>
        new(_dbContext, _tenantContext, _settings);

    private static Recruitment CreateRecruitmentWithSteps(Guid userId)
    {
        var recruitment = Recruitment.Create("Test Recruitment", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        return recruitment;
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
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        SetupRecruitmentDbSet();
        SetupCandidateDbSet();

        var handler = CreateHandler();
        var query = new GetRecruitmentOverviewQuery { RecruitmentId = Guid.NewGuid() };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = CreateRecruitmentWithSteps(creatorId);
        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet();

        var handler = CreateHandler();
        var query = new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsTotalCandidateCount()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = CreateRecruitmentWithSteps(userId);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate1 = Candidate.Create(recruitment.Id, "Alice", "alice@test.com", null, null, DateTimeOffset.UtcNow);
        candidate1.AssignToWorkflowStep(steps[0].Id);
        var candidate2 = Candidate.Create(recruitment.Id, "Bob", "bob@test.com", null, null, DateTimeOffset.UtcNow);
        candidate2.AssignToWorkflowStep(steps[0].Id);
        var candidate3 = Candidate.Create(recruitment.Id, "Carol", "carol@test.com", null, null, DateTimeOffset.UtcNow);
        candidate3.AssignToWorkflowStep(steps[1].Id);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate1, candidate2, candidate3);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        result.TotalCandidates.Should().Be(3);
        result.RecruitmentId.Should().Be(recruitment.Id);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsPerStepCandidateCounts()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = CreateRecruitmentWithSteps(userId);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate1 = Candidate.Create(recruitment.Id, "Alice", "alice@test.com", null, null, DateTimeOffset.UtcNow);
        candidate1.AssignToWorkflowStep(steps[0].Id);
        var candidate2 = Candidate.Create(recruitment.Id, "Bob", "bob@test.com", null, null, DateTimeOffset.UtcNow);
        candidate2.AssignToWorkflowStep(steps[0].Id);
        var candidate3 = Candidate.Create(recruitment.Id, "Carol", "carol@test.com", null, null, DateTimeOffset.UtcNow);
        candidate3.AssignToWorkflowStep(steps[1].Id);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate1, candidate2, candidate3);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        result.Steps.Should().HaveCount(2);
        result.Steps[0].StepName.Should().Be("Screening");
        result.Steps[0].TotalCandidates.Should().Be(2);
        result.Steps[1].StepName.Should().Be("Interview");
        result.Steps[1].TotalCandidates.Should().Be(1);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsOutcomeBreakdownPerStep()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = CreateRecruitmentWithSteps(userId);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        // Candidate with Fail outcome at Screening (still at Screening)
        var candidate1 = Candidate.Create(recruitment.Id, "Alice", "alice@test.com", null, null, DateTimeOffset.UtcNow);
        candidate1.AssignToWorkflowStep(steps[0].Id);
        candidate1.RecordOutcome(steps[0].Id, OutcomeStatus.Fail, userId, null, steps);

        // Candidate with Hold outcome at Screening (still at Screening)
        var candidate2 = Candidate.Create(recruitment.Id, "Bob", "bob@test.com", null, null, DateTimeOffset.UtcNow);
        candidate2.AssignToWorkflowStep(steps[0].Id);
        candidate2.RecordOutcome(steps[0].Id, OutcomeStatus.Hold, userId, null, steps);

        // Candidate with no outcome at Screening (pending)
        var candidate3 = Candidate.Create(recruitment.Id, "Carol", "carol@test.com", null, null, DateTimeOffset.UtcNow);
        candidate3.AssignToWorkflowStep(steps[0].Id);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate1, candidate2, candidate3);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        var screeningStep = result.Steps.First(s => s.StepName == "Screening");
        screeningStep.OutcomeBreakdown.Fail.Should().Be(1);
        screeningStep.OutcomeBreakdown.Hold.Should().Be(1);
        screeningStep.OutcomeBreakdown.NotStarted.Should().Be(1);
        screeningStep.OutcomeBreakdown.Pass.Should().Be(0);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsPendingActionCount()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = CreateRecruitmentWithSteps(userId);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        // 2 candidates pending at Screening
        var candidate1 = Candidate.Create(recruitment.Id, "Alice", "alice@test.com", null, null, DateTimeOffset.UtcNow);
        candidate1.AssignToWorkflowStep(steps[0].Id);
        var candidate2 = Candidate.Create(recruitment.Id, "Bob", "bob@test.com", null, null, DateTimeOffset.UtcNow);
        candidate2.AssignToWorkflowStep(steps[0].Id);

        // 1 candidate with outcome (not pending)
        var candidate3 = Candidate.Create(recruitment.Id, "Carol", "carol@test.com", null, null, DateTimeOffset.UtcNow);
        candidate3.AssignToWorkflowStep(steps[0].Id);
        candidate3.RecordOutcome(steps[0].Id, OutcomeStatus.Fail, userId, null, steps);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate1, candidate2, candidate3);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        result.PendingActionCount.Should().Be(2);
    }

    [Test]
    public async Task Handle_NoCandidates_ReturnsZeroCountsWithAllSteps()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = CreateRecruitmentWithSteps(userId);
        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        result.TotalCandidates.Should().Be(0);
        result.PendingActionCount.Should().Be(0);
        result.TotalStale.Should().Be(0);
        result.Steps.Should().HaveCount(2);
        result.Steps.Should().AllSatisfy(s =>
        {
            s.TotalCandidates.Should().Be(0);
            s.PendingCount.Should().Be(0);
            s.StaleCount.Should().Be(0);
            s.OutcomeBreakdown.NotStarted.Should().Be(0);
            s.OutcomeBreakdown.Pass.Should().Be(0);
            s.OutcomeBreakdown.Fail.Should().Be(0);
            s.OutcomeBreakdown.Hold.Should().Be(0);
        });
    }

    [Test]
    public async Task Handle_StaleCandidates_ReturnsPerStepStaleCounts()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = CreateRecruitmentWithSteps(userId);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        // Stale candidate: created 10 days ago, no outcome at current step
        var staleCandidate = Candidate.Create(
            recruitment.Id, "Stale", "stale@test.com", null, null,
            DateTimeOffset.UtcNow.AddDays(-10));
        staleCandidate.AssignToWorkflowStep(steps[0].Id);

        // Fresh candidate: created today, no outcome at current step
        var freshCandidate = Candidate.Create(
            recruitment.Id, "Fresh", "fresh@test.com", null, null,
            DateTimeOffset.UtcNow);
        freshCandidate.AssignToWorkflowStep(steps[0].Id);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(staleCandidate, freshCandidate);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        result.TotalStale.Should().Be(1);
        var screeningStep = result.Steps.First(s => s.StepName == "Screening");
        screeningStep.StaleCount.Should().Be(1);
    }

    [Test]
    public async Task Handle_ValidRequest_IncludesStaleDaysThreshold()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = CreateRecruitmentWithSteps(userId);
        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet();

        _settings = Options.Create(new OverviewSettings { StaleDays = 7 });
        var handler = new GetRecruitmentOverviewQueryHandler(_dbContext, _tenantContext, _settings);

        var result = await handler.Handle(
            new GetRecruitmentOverviewQuery { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        result.StaleDays.Should().Be(7);
    }
}

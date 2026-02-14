using api.Application.Common.Interfaces;
using api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;
using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Screening.Queries.GetCandidateOutcomeHistory;

[TestFixture]
public class GetCandidateOutcomeHistoryQueryHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
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
    public async Task Handle_ValidRequest_ReturnsOrderedHistory()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        candidate.AssignToWorkflowStep(steps[0].Id);
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, userId, "Good", steps);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate);

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, candidate.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].WorkflowStepName.Should().Be("Screening");
        result[0].Outcome.Should().Be(OutcomeStatus.Pass);
        result[0].Reason.Should().Be("Good");
    }

    [Test]
    public async Task Handle_NonMemberUser_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitment = Recruitment.Create("Test", null, creatorId);
        SetupRecruitmentDbSet(recruitment);

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, Guid.NewGuid());

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_CandidateNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet();

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, Guid.NewGuid());

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NoOutcomes_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate);

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, candidate.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_FiltersOutNotStartedOutcomes()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        // Simulate initial placement (NotStarted outcome) + a real outcome
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.NotStarted, userId);
        candidate.AssignToWorkflowStep(steps[0].Id);
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, userId, "Good", steps);

        SetupRecruitmentDbSet(recruitment);
        SetupCandidateDbSet(candidate);

        var handler = new GetCandidateOutcomeHistoryQueryHandler(_dbContext, _tenantContext);
        var query = new GetCandidateOutcomeHistoryQuery(recruitment.Id, candidate.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Outcome.Should().Be(OutcomeStatus.Pass);
    }
}

using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;
using api.Domain.Entities;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.RemoveWorkflowStep;

[TestFixture]
public class RemoveWorkflowStepCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();

        // Default: no candidates with outcomes
        var mockCandidates = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(mockCandidates);
    }

    [Test]
    public async Task Handle_StepWithNoOutcomes_RemovesStep()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        var stepId = recruitment.Steps.First().Id;

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            StepId = stepId,
        };

        await handler.Handle(command, CancellationToken.None);

        recruitment.Steps.Should().BeEmpty();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveWorkflowStepCommand
        {
            RecruitmentId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_StepNotFound_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            StepId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        recruitment.AddStep("Screening", 1);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            StepId = recruitment.Steps.First().Id,
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_StepWithOutcomes_ThrowsStepHasOutcomesException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        var stepId = recruitment.Steps.First().Id;

        // Mark the step as having outcomes
        recruitment.MarkStepHasOutcomes(stepId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            StepId = stepId,
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<StepHasOutcomesException>();
    }
}

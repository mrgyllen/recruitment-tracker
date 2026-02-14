using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;
using api.Domain.Entities;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.RemoveWorkflowStep;

[TestFixture]
public class RemoveWorkflowStepCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();

        // Default: no candidates with outcomes
        var mockCandidates = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(mockCandidates);
    }

    [Test]
    public async Task Handle_StepWithNoOutcomes_RemovesStep()
    {
        var recruitment = Recruitment.Create("Test", null, Guid.NewGuid());
        recruitment.AddStep("Screening", 1);
        var stepId = recruitment.Steps.First().Id;

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext);
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
        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext);
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
        var recruitment = Recruitment.Create("Test", null, Guid.NewGuid());
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new RemoveWorkflowStepCommandHandler(_dbContext);
        var command = new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            StepId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

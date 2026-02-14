using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.ReorderWorkflowSteps;

[TestFixture]
public class ReorderWorkflowStepsCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
    }

    [Test]
    public async Task Handle_ValidReorder_UpdatesStepOrders()
    {
        var recruitment = Recruitment.Create("Test", null, Guid.NewGuid());
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        var step1 = recruitment.Steps.First(s => s.Name == "Screening");
        var step2 = recruitment.Steps.First(s => s.Name == "Interview");

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new ReorderWorkflowStepsCommandHandler(_dbContext);
        var command = new ReorderWorkflowStepsCommand
        {
            RecruitmentId = recruitment.Id,
            Steps =
            [
                new StepOrderDto { StepId = step2.Id, Order = 1 },
                new StepOrderDto { StepId = step1.Id, Order = 2 },
            ],
        };

        await handler.Handle(command, CancellationToken.None);

        recruitment.Steps.First(s => s.Name == "Interview").Order.Should().Be(1);
        recruitment.Steps.First(s => s.Name == "Screening").Order.Should().Be(2);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new ReorderWorkflowStepsCommandHandler(_dbContext);
        var command = new ReorderWorkflowStepsCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Steps =
            [
                new StepOrderDto { StepId = Guid.NewGuid(), Order = 1 },
            ],
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

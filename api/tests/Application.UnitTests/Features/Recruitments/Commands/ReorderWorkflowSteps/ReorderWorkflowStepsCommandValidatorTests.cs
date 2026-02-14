using api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands.ReorderWorkflowSteps;

[TestFixture]
public class ReorderWorkflowStepsCommandValidatorTests
{
    private ReorderWorkflowStepsCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new ReorderWorkflowStepsCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_Passes()
    {
        var command = new ReorderWorkflowStepsCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Steps =
            [
                new StepOrderDto { StepId = Guid.NewGuid(), Order = 1 },
                new StepOrderDto { StepId = Guid.NewGuid(), Order = 2 },
            ],
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyStepsList_Fails()
    {
        var command = new ReorderWorkflowStepsCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Steps = [],
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Steps");
    }

    [Test]
    public void Validate_NonContiguousOrders_Fails()
    {
        var command = new ReorderWorkflowStepsCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Steps =
            [
                new StepOrderDto { StepId = Guid.NewGuid(), Order = 1 },
                new StepOrderDto { StepId = Guid.NewGuid(), Order = 5 },
            ],
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Steps");
    }
}

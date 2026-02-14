using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands.AddWorkflowStep;

[TestFixture]
public class AddWorkflowStepCommandValidatorTests
{
    private AddWorkflowStepCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new AddWorkflowStepCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_Passes()
    {
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Name = "Screening",
            Order = 1,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyName_Fails()
    {
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Name = "",
            Order = 1,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public void Validate_EmptyRecruitmentId_Fails()
    {
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = Guid.Empty,
            Name = "Screening",
            Order = 1,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecruitmentId");
    }

    [Test]
    public void Validate_OrderZero_Fails()
    {
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Name = "Screening",
            Order = 0,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Order");
    }
}

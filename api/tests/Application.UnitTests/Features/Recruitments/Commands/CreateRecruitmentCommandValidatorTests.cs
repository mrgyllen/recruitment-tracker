using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands;

[TestFixture]
public class CreateRecruitmentCommandValidatorTests
{
    private CreateRecruitmentCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new CreateRecruitmentCommandValidator();
    }

    [Test]
    public async Task Validate_EmptyTitle_HasValidationError()
    {
        var command = new CreateRecruitmentCommand
        {
            Title = "",
            Steps = [new WorkflowStepDto { Name = "Screening", Order = 1 }]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public async Task Validate_TitleTooLong_HasValidationError()
    {
        var command = new CreateRecruitmentCommand
        {
            Title = new string('A', 201),
            Steps = []
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public async Task Validate_ValidTitle_Passes()
    {
        var command = new CreateRecruitmentCommand
        {
            Title = "Senior Developer",
            Steps = [new WorkflowStepDto { Name = "Screening", Order = 1 }]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_DuplicateStepNames_HasValidationError()
    {
        var command = new CreateRecruitmentCommand
        {
            Title = "Dev Role",
            Steps =
            [
                new WorkflowStepDto { Name = "Screening", Order = 1 },
                new WorkflowStepDto { Name = "Screening", Order = 2 }
            ]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Steps");
    }

    [Test]
    public async Task Validate_EmptyStepName_HasValidationError()
    {
        var command = new CreateRecruitmentCommand
        {
            Title = "Dev Role",
            Steps = [new WorkflowStepDto { Name = "", Order = 1 }]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Test]
    public async Task Validate_DescriptionTooLong_HasValidationError()
    {
        var command = new CreateRecruitmentCommand
        {
            Title = "Dev Role",
            Description = new string('A', 2001),
            Steps = []
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Test]
    public async Task Validate_EmptySteps_IsValid()
    {
        var command = new CreateRecruitmentCommand
        {
            Title = "Dev Role",
            Steps = []
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}

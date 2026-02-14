using api.Application.Features.Recruitments.Commands.UpdateRecruitment;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands.UpdateRecruitment;

[TestFixture]
public class UpdateRecruitmentCommandValidatorTests
{
    private UpdateRecruitmentCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateRecruitmentCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_Passes()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = "Senior Dev",
            Description = "A description",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyTitle_Fails()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = "",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public void Validate_TitleTooLong_Fails()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = new string('x', 201),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Test]
    public void Validate_EmptyId_Fails()
    {
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.Empty,
            Title = "Valid Title",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

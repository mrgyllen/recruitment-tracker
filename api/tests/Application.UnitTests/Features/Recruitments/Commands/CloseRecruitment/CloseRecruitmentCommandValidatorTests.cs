using api.Application.Features.Recruitments.Commands.CloseRecruitment;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands.CloseRecruitment;

[TestFixture]
public class CloseRecruitmentCommandValidatorTests
{
    private CloseRecruitmentCommandValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new CloseRecruitmentCommandValidator();
    }

    [Test]
    public void Validate_ValidId_Passes()
    {
        var command = new CloseRecruitmentCommand { RecruitmentId = Guid.NewGuid() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyId_Fails()
    {
        var command = new CloseRecruitmentCommand { RecruitmentId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RecruitmentId);
    }
}

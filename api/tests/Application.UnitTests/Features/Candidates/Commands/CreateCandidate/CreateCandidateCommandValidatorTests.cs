using api.Application.Features.Candidates.Commands.CreateCandidate;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Candidates.Commands.CreateCandidate;

[TestFixture]
public class CreateCandidateCommandValidatorTests
{
    private CreateCandidateCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateCandidateCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_Passes()
    {
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "jane@example.com",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyFullName_Fails()
    {
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "",
            Email = "jane@example.com",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Test]
    public void Validate_EmptyEmail_Fails()
    {
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Test]
    public void Validate_InvalidEmailFormat_Fails()
    {
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "not-an-email",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Test]
    public void Validate_EmptyRecruitmentId_Fails()
    {
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.Empty,
            FullName = "Jane Doe",
            Email = "jane@example.com",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecruitmentId");
    }

    [Test]
    public void Validate_FullNameTooLong_Fails()
    {
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = new string('A', 201),
            Email = "jane@example.com",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Test]
    public void Validate_EmailTooLong_Fails()
    {
        var command = new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = new string('a', 246) + "@test.com",
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}

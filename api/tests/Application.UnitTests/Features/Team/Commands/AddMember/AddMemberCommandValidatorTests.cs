using api.Application.Features.Team.Commands.AddMember;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Commands.AddMember;

[TestFixture]
public class AddMemberCommandValidatorTests
{
    private AddMemberCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new AddMemberCommandValidator();
    }

    [Test]
    public void Validate_MissingRecruitmentId_Fails()
    {
        var command = new AddMemberCommand
        {
            RecruitmentId = Guid.Empty,
            UserId = Guid.NewGuid(),
            DisplayName = "Test",
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_MissingUserId_Fails()
    {
        var command = new AddMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            UserId = Guid.Empty,
            DisplayName = "Test",
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ValidInput_Passes()
    {
        var command = new AddMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DisplayName = "Test User",
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}

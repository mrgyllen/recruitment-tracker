using api.Application.Features.Team.Commands.RemoveMember;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Commands.RemoveMember;

[TestFixture]
public class RemoveMemberCommandValidatorTests
{
    private RemoveMemberCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RemoveMemberCommandValidator();
    }

    [Test]
    public void Validate_MissingRecruitmentId_Fails()
    {
        var command = new RemoveMemberCommand
        {
            RecruitmentId = Guid.Empty,
            MemberId = Guid.NewGuid(),
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_MissingMemberId_Fails()
    {
        var command = new RemoveMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            MemberId = Guid.Empty,
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ValidInput_Passes()
    {
        var command = new RemoveMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
        };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}

using api.Application.Features.Import.Commands.ResolveMatchConflict;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Commands.ResolveMatchConflict;

[TestFixture]
public class ResolveMatchConflictCommandValidatorTests
{
    private ResolveMatchConflictCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new ResolveMatchConflictCommandValidator();
    }

    [Test]
    public void Validate_ValidConfirmCommand_Succeeds()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Confirm");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ValidRejectCommand_Succeeds()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Reject");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptySessionId_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.Empty, 0, "Confirm");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_NegativeMatchIndex_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), -1, "Confirm");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_InvalidAction_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Invalid");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_EmptyAction_Fails()
    {
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}

using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Screening.Commands.RecordOutcome;

[TestFixture]
public class RecordOutcomeCommandValidatorTests
{
    private RecordOutcomeCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RecordOutcomeCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_Passes()
    {
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, "Good candidate");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_NullReason_Passes()
    {
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Hold, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_NotStartedOutcome_Fails()
    {
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.NotStarted, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Outcome");
    }

    [Test]
    public void Validate_EmptyRecruitmentId_Fails()
    {
        var command = new RecordOutcomeCommand(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecruitmentId");
    }

    [Test]
    public void Validate_EmptyCandidateId_Fails()
    {
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), OutcomeStatus.Pass, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CandidateId");
    }

    [Test]
    public void Validate_EmptyWorkflowStepId_Fails()
    {
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, OutcomeStatus.Pass, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WorkflowStepId");
    }

    [Test]
    public void Validate_ReasonExceeds500Chars_Fails()
    {
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Pass, new string('A', 501));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Test]
    public void Validate_ReasonExactly500Chars_Passes()
    {
        var command = new RecordOutcomeCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OutcomeStatus.Fail, new string('A', 500));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}

using api.Application.Features.Candidates;
using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Candidates.Mapping;

[TestFixture]
public class CandidateDtoTests
{
    private Recruitment _recruitment = null!;
    private Candidate _candidate = null!;

    [SetUp]
    public void SetUp()
    {
        var userId = Guid.NewGuid();
        _recruitment = Recruitment.Create("Test Recruitment", null, userId);
        _recruitment.AddStep("Screening", 1);
        _recruitment.AddStep("Interview", 2);
        _recruitment.AddStep("Final Review", 3);

        _candidate = Candidate.Create(
            _recruitment.Id, "Alice Test", "alice@example.com", null, null, DateTimeOffset.UtcNow);
    }

    [Test]
    public void From_CandidateWithNoOutcomes_CurrentStepIsFirstStep()
    {
        var steps = _recruitment.Steps.ToList();

        var dto = CandidateDto.From(_candidate, steps);

        var firstStep = steps.OrderBy(s => s.Order).First();
        dto.CurrentWorkflowStepId.Should().Be(firstStep.Id);
        dto.CurrentWorkflowStepName.Should().Be("Screening");
        dto.CurrentOutcomeStatus.Should().Be("NotStarted");
    }

    [Test]
    public void From_CandidateWithPassOutcome_CurrentStepIsNextStep()
    {
        var steps = _recruitment.Steps.ToList();
        var screeningStep = steps.Single(s => s.Name == "Screening");

        _candidate.RecordOutcome(screeningStep.Id, OutcomeStatus.Pass, Guid.NewGuid());

        var dto = CandidateDto.From(_candidate, steps);

        var interviewStep = steps.Single(s => s.Name == "Interview");
        dto.CurrentWorkflowStepId.Should().Be(interviewStep.Id);
        dto.CurrentWorkflowStepName.Should().Be("Interview");
        dto.CurrentOutcomeStatus.Should().Be("NotStarted");
    }

    [Test]
    public void From_CandidateAtLastStepWithPass_CurrentStepIsLastStep()
    {
        var steps = _recruitment.Steps.ToList();
        var screeningStep = steps.Single(s => s.Name == "Screening");
        var interviewStep = steps.Single(s => s.Name == "Interview");
        var finalStep = steps.Single(s => s.Name == "Final Review");

        _candidate.RecordOutcome(screeningStep.Id, OutcomeStatus.Pass, Guid.NewGuid());
        _candidate.RecordOutcome(interviewStep.Id, OutcomeStatus.Pass, Guid.NewGuid());
        _candidate.RecordOutcome(finalStep.Id, OutcomeStatus.Pass, Guid.NewGuid());

        var dto = CandidateDto.From(_candidate, steps);

        dto.CurrentWorkflowStepId.Should().Be(finalStep.Id);
        dto.CurrentWorkflowStepName.Should().Be("Final Review");
        dto.CurrentOutcomeStatus.Should().Be("Pass");
    }

    [Test]
    public void From_CandidateWithFailOutcome_CurrentStepIsSameStep()
    {
        var steps = _recruitment.Steps.ToList();
        var screeningStep = steps.Single(s => s.Name == "Screening");

        _candidate.RecordOutcome(screeningStep.Id, OutcomeStatus.Fail, Guid.NewGuid());

        var dto = CandidateDto.From(_candidate, steps);

        dto.CurrentWorkflowStepId.Should().Be(screeningStep.Id);
        dto.CurrentWorkflowStepName.Should().Be("Screening");
        dto.CurrentOutcomeStatus.Should().Be("Fail");
    }

    [Test]
    public void From_CandidateWithHoldOutcome_CurrentStepIsSameStep()
    {
        var steps = _recruitment.Steps.ToList();
        var screeningStep = steps.Single(s => s.Name == "Screening");

        _candidate.RecordOutcome(screeningStep.Id, OutcomeStatus.Hold, Guid.NewGuid());

        var dto = CandidateDto.From(_candidate, steps);

        dto.CurrentWorkflowStepId.Should().Be(screeningStep.Id);
        dto.CurrentWorkflowStepName.Should().Be("Screening");
        dto.CurrentOutcomeStatus.Should().Be("Hold");
    }

    [Test]
    public void From_CandidateWithNoSteps_CurrentStepIsNull()
    {
        var dto = CandidateDto.From(_candidate, Array.Empty<WorkflowStep>());

        dto.CurrentWorkflowStepId.Should().BeNull();
        dto.CurrentWorkflowStepName.Should().BeNull();
        dto.CurrentOutcomeStatus.Should().BeNull();
    }

    [Test]
    public void From_CandidateWithNoStepsOverload_CurrentStepIsNull()
    {
        var dto = CandidateDto.From(_candidate);

        dto.CurrentWorkflowStepId.Should().BeNull();
        dto.CurrentWorkflowStepName.Should().BeNull();
        dto.CurrentOutcomeStatus.Should().BeNull();
    }

    [Test]
    public void From_MapsBasicFieldsCorrectly()
    {
        var steps = _recruitment.Steps.ToList();

        var dto = CandidateDto.From(_candidate, steps);

        dto.Id.Should().Be(_candidate.Id);
        dto.RecruitmentId.Should().Be(_candidate.RecruitmentId);
        dto.FullName.Should().Be("Alice Test");
        dto.Email.Should().Be("alice@example.com");
    }
}

using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;
using api.Domain.Enums;
using api.Domain.Exceptions;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Screening;

using static Testing;

public class RecordOutcomeTests : BaseTestFixture
{
    private async Task<(Guid RecruitmentId, Guid CandidateId, Guid StepId)> SetUpRecruitmentWithCandidateAndStep()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var step = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice Johnson",
            Email = "alice@example.com",
        });

        return (recruitmentId, candidateId, step.Id);
    }

    [Test]
    public async Task RecordOutcome_ValidPass_RecordsSuccessfully()
    {
        var (recruitmentId, candidateId, stepId) = await SetUpRecruitmentWithCandidateAndStep();

        var result = await SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: stepId,
            Outcome: OutcomeStatus.Pass,
            Reason: "Excellent qualifications"
        ));

        result.CandidateId.Should().Be(candidateId);
        result.WorkflowStepId.Should().Be(stepId);
        result.Outcome.Should().Be(OutcomeStatus.Pass);
        result.Reason.Should().Be("Excellent qualifications");
        result.OutcomeId.Should().NotBeEmpty();
    }

    [Test]
    public async Task RecordOutcome_ValidFail_RecordsAndKeepsCurrentStep()
    {
        var (recruitmentId, candidateId, stepId) = await SetUpRecruitmentWithCandidateAndStep();

        var result = await SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: stepId,
            Outcome: OutcomeStatus.Fail,
            Reason: "Did not meet requirements"
        ));

        result.Outcome.Should().Be(OutcomeStatus.Fail);
        result.IsCompleted.Should().BeFalse();
    }

    [Test]
    public async Task RecordOutcome_PassOnLastStep_MarksCompleted()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Single Step Recruitment",
        });
        var step = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Final Review",
            Order = 1,
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Bob Smith",
            Email = "bob@example.com",
        });

        var result = await SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: step.Id,
            Outcome: OutcomeStatus.Pass,
            Reason: null
        ));

        result.IsCompleted.Should().BeTrue();
    }

    [Test]
    public async Task RecordOutcome_PassWithMultipleSteps_AdvancesToNextStep()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Multi-Step Recruitment",
        });
        var step1 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        var step2 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Interview",
            Order = 2,
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Charlie Brown",
            Email = "charlie@example.com",
        });

        var result = await SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: step1.Id,
            Outcome: OutcomeStatus.Pass,
            Reason: null
        ));

        result.NewCurrentStepId.Should().Be(step2.Id);
        result.IsCompleted.Should().BeFalse();
    }

    [Test]
    public async Task RecordOutcome_InvalidStep_ThrowsNotFoundException()
    {
        var (recruitmentId, candidateId, _) = await SetUpRecruitmentWithCandidateAndStep();

        var act = () => SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: Guid.NewGuid(),
            Outcome: OutcomeStatus.Pass,
            Reason: null
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RecordOutcome_WrongStep_ThrowsInvalidWorkflowTransitionException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Multi-Step Recruitment",
        });
        await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        var step2 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Interview",
            Order = 2,
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice Johnson",
            Email = "alice@example.com",
        });

        var act = () => SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: step2.Id,
            Outcome: OutcomeStatus.Pass,
            Reason: null
        ));

        await act.Should().ThrowAsync<InvalidWorkflowTransitionException>();
    }

    [Test]
    public async Task RecordOutcome_NonMember_ThrowsNotFoundException()
    {
        var (recruitmentId, candidateId, stepId) = await SetUpRecruitmentWithCandidateAndStep();

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: stepId,
            Outcome: OutcomeStatus.Pass,
            Reason: null
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RecordOutcome_NotStartedStatus_ThrowsValidationException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new RecordOutcomeCommand(
            RecruitmentId: Guid.NewGuid(),
            CandidateId: Guid.NewGuid(),
            WorkflowStepId: Guid.NewGuid(),
            Outcome: OutcomeStatus.NotStarted,
            Reason: null
        ));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task RecordOutcome_OutcomePersistedToHistory_VerifiedViaQuery()
    {
        var (recruitmentId, candidateId, stepId) = await SetUpRecruitmentWithCandidateAndStep();

        await SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: stepId,
            Outcome: OutcomeStatus.Hold,
            Reason: "Needs further review"
        ));

        var history = await SendAsync(new GetCandidateOutcomeHistoryQuery(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId
        ));

        history.Should().HaveCount(1);
        history[0].Outcome.Should().Be(OutcomeStatus.Hold);
        history[0].Reason.Should().Be("Needs further review");
        history[0].WorkflowStepName.Should().Be("Screening");
    }
}

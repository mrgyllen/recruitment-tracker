using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Domain.Enums;
using api.Domain.Exceptions;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Recruitments;

using static Testing;

public class RemoveWorkflowStepTests : BaseTestFixture
{
    [Test]
    public async Task RemoveWorkflowStep_NoOutcomes_RemovesStep()
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

        await SendAsync(new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            StepId = step.Id,
        });

        var recruitment = await SendAsync(new GetRecruitmentByIdQuery { Id = recruitmentId });
        recruitment.Steps.Should().BeEmpty();
    }

    [Test]
    public async Task RemoveWorkflowStep_WithOutcomes_ThrowsStepHasOutcomesException()
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

        // Record an outcome on this step
        await SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: step.Id,
            Outcome: OutcomeStatus.Pass,
            Reason: "Good fit"
        ));

        var act = () => SendAsync(new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            StepId = step.Id,
        });

        await act.Should().ThrowAsync<StepHasOutcomesException>();
    }

    [Test]
    public async Task RemoveWorkflowStep_NonMember_ThrowsNotFoundException()
    {
        await RunAsUserAsync("userA@local", Array.Empty<string>());
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

        await RunAsUserAsync("userB@local", Array.Empty<string>());

        var act = () => SendAsync(new RemoveWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            StepId = step.Id,
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

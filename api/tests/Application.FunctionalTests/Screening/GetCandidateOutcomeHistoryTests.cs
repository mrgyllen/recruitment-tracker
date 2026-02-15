using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;
using api.Domain.Enums;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Screening;

using static Testing;

public class GetCandidateOutcomeHistoryTests : BaseTestFixture
{
    [Test]
    public async Task GetCandidateOutcomeHistory_WithRecordedOutcome_ReturnsHistory()
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

        await SendAsync(new RecordOutcomeCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            WorkflowStepId: step.Id,
            Outcome: OutcomeStatus.Pass,
            Reason: "Strong candidate"
        ));

        var history = await SendAsync(new GetCandidateOutcomeHistoryQuery(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId
        ));

        history.Should().HaveCount(1);
        history[0].WorkflowStepName.Should().Be("Screening");
        history[0].Outcome.Should().Be(OutcomeStatus.Pass);
        history[0].Reason.Should().Be("Strong candidate");
        history[0].WorkflowStepId.Should().Be(step.Id);
        history[0].StepOrder.Should().Be(1);
    }

    [Test]
    public async Task GetCandidateOutcomeHistory_NoOutcomes_ReturnsEmptyList()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Bob Smith",
            Email = "bob@example.com",
        });

        var history = await SendAsync(new GetCandidateOutcomeHistoryQuery(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId
        ));

        history.Should().BeEmpty();
    }

    [Test]
    public async Task GetCandidateOutcomeHistory_CandidateNotFound_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var act = () => SendAsync(new GetCandidateOutcomeHistoryQuery(
            RecruitmentId: recruitmentId,
            CandidateId: Guid.NewGuid()
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetCandidateOutcomeHistory_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });
        await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice",
            Email = "alice@example.com",
        });

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new GetCandidateOutcomeHistoryQuery(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

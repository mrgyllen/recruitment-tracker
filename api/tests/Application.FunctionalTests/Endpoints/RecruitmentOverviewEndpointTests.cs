using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Queries.GetRecruitmentOverview;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Endpoints;

using static Testing;

public class RecruitmentOverviewEndpointTests : BaseTestFixture
{
    private Guid _recruitmentId;

    private async Task SetUpRecruitmentWithCandidates()
    {
        await RunAsDefaultUserAsync();
        _recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Overview Test Recruitment",
        });
        await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = _recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = _recruitmentId,
            Name = "Interview",
            Order = 2,
        });
        await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = _recruitmentId,
            FullName = "Alice Johnson",
            Email = "alice@example.com",
        });
        await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = _recruitmentId,
            FullName = "Bob Smith",
            Email = "bob@example.com",
        });
    }

    [Test]
    public async Task GetOverview_Authenticated_ReturnsOkWithOverviewData()
    {
        await SetUpRecruitmentWithCandidates();

        var result = await SendAsync(new GetRecruitmentOverviewQuery
        {
            RecruitmentId = _recruitmentId,
        });

        result.RecruitmentId.Should().Be(_recruitmentId);
        result.TotalCandidates.Should().Be(2);
        result.Steps.Should().HaveCount(2);
        result.Steps[0].StepName.Should().Be("Screening");
        result.Steps[1].StepName.Should().Be("Interview");
        result.StaleDays.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetOverview_NonMember_ReturnsForbidden()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new GetRecruitmentOverviewQuery
        {
            RecruitmentId = recruitmentId,
        });

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetOverview_NoCandidates_ReturnsZeroCountsWithSteps()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Empty Recruitment",
        });
        await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });

        var result = await SendAsync(new GetRecruitmentOverviewQuery
        {
            RecruitmentId = recruitmentId,
        });

        result.TotalCandidates.Should().Be(0);
        result.PendingActionCount.Should().Be(0);
        result.TotalStale.Should().Be(0);
        result.Steps.Should().HaveCount(1);
        result.Steps[0].TotalCandidates.Should().Be(0);
    }

    [Test]
    public async Task GetOverview_InvalidGuid_ReturnsBadRequest()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new GetRecruitmentOverviewQuery
        {
            RecruitmentId = Guid.Empty,
        });

        await act.Should().ThrowAsync<ValidationException>();
    }
}

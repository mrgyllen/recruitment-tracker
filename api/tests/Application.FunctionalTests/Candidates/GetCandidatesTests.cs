using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Candidates.Queries.GetCandidates;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Domain.Enums;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Candidates;

using static Testing;

public class GetCandidatesTests : BaseTestFixture
{
    private Guid _recruitmentId;

    private async Task SetUpRecruitmentWithCandidates()
    {
        await RunAsDefaultUserAsync();
        _recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
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
            Email = "bob@contoso.com",
        });
    }

    [Test]
    public async Task GetCandidates_ValidRequest_ReturnsPaginatedList()
    {
        await SetUpRecruitmentWithCandidates();

        var result = await SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = _recruitmentId,
            Page = 1,
            PageSize = 50,
        });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task GetCandidates_WithSearch_ReturnsFilteredResults()
    {
        await SetUpRecruitmentWithCandidates();

        var result = await SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = _recruitmentId,
            Search = "Alice",
        });

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Alice Johnson");
    }

    [Test]
    public async Task GetCandidates_WithSearchByEmail_ReturnsFilteredResults()
    {
        await SetUpRecruitmentWithCandidates();

        var result = await SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = _recruitmentId,
            Search = "contoso",
        });

        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("bob@contoso.com");
    }

    [Test]
    public async Task GetCandidates_RecruitmentNotFound_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetCandidates_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });

        // Switch to a different user who is not a member
        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = recruitmentId,
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetCandidates_InvalidPageSize_ThrowsValidationException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = Guid.NewGuid(),
            PageSize = 0,
        });

        await act.Should().ThrowAsync<ValidationException>();
    }
}

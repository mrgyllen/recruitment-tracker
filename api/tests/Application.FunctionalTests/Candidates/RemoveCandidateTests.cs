using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Candidates.Commands.RemoveCandidate;
using api.Application.Features.Candidates.Queries.GetCandidates;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Candidates;

using static Testing;

public class RemoveCandidateTests : BaseTestFixture
{
    [Test]
    public async Task Handle_ValidCandidate_RemovesCandidateSuccessfully()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice Johnson",
            Email = "alice@example.com",
        });

        await SendAsync(new RemoveCandidateCommand
        {
            RecruitmentId = recruitmentId,
            CandidateId = candidateId,
        });

        var result = await SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = recruitmentId,
        });
        result.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_NonExistentCandidate_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var act = () => SendAsync(new RemoveCandidateCommand
        {
            RecruitmentId = recruitmentId,
            CandidateId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonExistentRecruitment_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new RemoveCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            CandidateId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice",
            Email = "alice@example.com",
        });

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new RemoveCandidateCommand
        {
            RecruitmentId = recruitmentId,
            CandidateId = candidateId,
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

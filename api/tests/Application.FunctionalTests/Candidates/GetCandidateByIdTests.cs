using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Candidates.Queries.GetCandidateById;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Candidates;

using static Testing;

public class GetCandidateByIdTests : BaseTestFixture
{
    [Test]
    public async Task GetCandidateById_ValidId_ReturnsCandidateDetail()
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
            FullName = "Alice Johnson",
            Email = "alice@example.com",
            PhoneNumber = "+1-555-0101",
            Location = "New York, NY",
        });

        var result = await SendAsync(new GetCandidateByIdQuery
        {
            RecruitmentId = recruitmentId,
            CandidateId = candidateId,
        });

        result.Id.Should().Be(candidateId);
        result.FullName.Should().Be("Alice Johnson");
        result.Email.Should().Be("alice@example.com");
        result.PhoneNumber.Should().Be("+1-555-0101");
        result.Location.Should().Be("New York, NY");
        result.CurrentWorkflowStepName.Should().Be("Screening");
        result.CurrentOutcomeStatus.Should().Be("NotStarted");
        result.OutcomeHistory.Should().BeEmpty();
    }

    [Test]
    public async Task GetCandidateById_NotFound_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var act = () => SendAsync(new GetCandidateByIdQuery
        {
            RecruitmentId = recruitmentId,
            CandidateId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetCandidateById_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice",
            Email = "alice@example.com",
        });

        // Switch to a different user who is not a member
        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new GetCandidateByIdQuery
        {
            RecruitmentId = recruitmentId,
            CandidateId = candidateId,
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetCandidateById_EmptyCandidateId_ThrowsValidationException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new GetCandidateByIdQuery
        {
            RecruitmentId = Guid.NewGuid(),
            CandidateId = Guid.Empty,
        });

        await act.Should().ThrowAsync<ValidationException>();
    }
}

using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Candidates.Queries.GetCandidates;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Domain.Entities;
using api.Domain.Exceptions;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Candidates;

using static Testing;

public class CreateCandidateTests : BaseTestFixture
{
    [Test]
    public async Task Handle_ValidCandidate_CreatesCandidateAndReturnsId()
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
            FullName = "Jane Doe",
            Email = "jane@example.com",
            PhoneNumber = "+1-555-0199",
            Location = "Seattle, WA",
        });

        candidateId.Should().NotBeEmpty();

        var candidates = await SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = recruitmentId,
        });
        candidates.Items.Should().HaveCount(1);
        candidates.Items[0].FullName.Should().Be("Jane Doe");
        candidates.Items[0].Email.Should().Be("jane@example.com");
    }

    [Test]
    public async Task Handle_NonExistentRecruitment_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "jane@example.com",
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_DuplicateEmail_ThrowsDuplicateCandidateException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Jane Doe",
            Email = "jane@example.com",
        });

        var act = () => SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Jane Doe Copy",
            Email = "jane@example.com",
        });

        await act.Should().ThrowAsync<DuplicateCandidateException>();
    }

    [Test]
    public async Task Handle_AssignsFirstWorkflowStep_WhenStepsExist()
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
        await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Interview",
            Order = 2,
        });

        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice",
            Email = "alice@example.com",
        });

        var candidate = await FindAsync<Candidate>(candidateId);
        candidate.Should().NotBeNull();
        candidate!.CurrentWorkflowStepId.Should().NotBeNull();
    }

    [Test]
    public async Task Handle_EmptyEmail_ThrowsValidationException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "",
        });

        await act.Should().ThrowAsync<ValidationException>();
    }
}

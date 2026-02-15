using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CloseRecruitment;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Domain.Exceptions;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Recruitments;

using static Testing;

public class AddWorkflowStepTests : BaseTestFixture
{
    [Test]
    public async Task AddWorkflowStep_ValidRequest_AddsStepAndReturnsDto()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var stepResult = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Technical Interview",
            Order = 1,
        });

        stepResult.Name.Should().Be("Technical Interview");
        stepResult.Order.Should().Be(1);
        stepResult.Id.Should().NotBeEmpty();

        var recruitment = await SendAsync(new GetRecruitmentByIdQuery { Id = recruitmentId });
        recruitment.Steps.Should().HaveCount(1);
        recruitment.Steps.Should().ContainSingle(s => s.Name == "Technical Interview");
    }

    [Test]
    public async Task AddWorkflowStep_DuplicateName_ThrowsDuplicateStepNameException()
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

        var act = () => SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 2,
        });

        await act.Should().ThrowAsync<DuplicateStepNameException>();
    }

    [Test]
    public async Task AddWorkflowStep_ClosedRecruitment_ThrowsRecruitmentClosedException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Closed Recruitment",
        });
        await SendAsync(new CloseRecruitmentCommand { RecruitmentId = recruitmentId });

        var act = () => SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Late Step",
            Order = 1,
        });

        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }

    [Test]
    public async Task AddWorkflowStep_NonMember_ThrowsNotFoundException()
    {
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });

        await RunAsUserAsync("userB@local", Array.Empty<string>());

        var act = () => SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Sneaky Step",
            Order = 1,
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

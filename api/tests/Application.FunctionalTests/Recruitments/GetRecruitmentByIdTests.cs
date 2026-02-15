using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Recruitments;

using static Testing;

public class GetRecruitmentByIdTests : BaseTestFixture
{
    [Test]
    public async Task GetRecruitmentById_ValidId_ReturnsRecruitmentWithSteps()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
            Description = "Test Description",
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

        var result = await SendAsync(new GetRecruitmentByIdQuery { Id = recruitmentId });

        result.Id.Should().Be(recruitmentId);
        result.Title.Should().Be("Test Recruitment");
        result.Description.Should().Be("Test Description");
        result.Status.Should().Be("Active");
        result.Steps.Should().HaveCount(2);
        result.Steps.Should().ContainSingle(s => s.Name == "Screening" && s.Order == 1);
        result.Steps.Should().ContainSingle(s => s.Name == "Interview" && s.Order == 2);
        result.Members.Should().HaveCount(1);
    }

    [Test]
    public async Task GetRecruitmentById_NonExistentId_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new GetRecruitmentByIdQuery { Id = Guid.NewGuid() });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetRecruitmentById_NonMember_ThrowsNotFoundException()
    {
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });

        await RunAsUserAsync("userB@local", Array.Empty<string>());

        var act = () => SendAsync(new GetRecruitmentByIdQuery { Id = recruitmentId });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

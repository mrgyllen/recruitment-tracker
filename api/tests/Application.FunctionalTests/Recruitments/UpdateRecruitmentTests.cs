using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Commands.UpdateRecruitment;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Recruitments;

using static Testing;

public class UpdateRecruitmentTests : BaseTestFixture
{
    [Test]
    public async Task UpdateRecruitment_ValidRequest_UpdatesDetails()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Original Title",
            Description = "Original Description",
        });

        await SendAsync(new UpdateRecruitmentCommand
        {
            Id = recruitmentId,
            Title = "Updated Title",
            Description = "Updated Description",
            JobRequisitionId = "REQ-001",
        });

        var result = await SendAsync(new GetRecruitmentByIdQuery { Id = recruitmentId });

        result.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated Description");
        result.JobRequisitionId.Should().Be("REQ-001");
    }

    [Test]
    public async Task UpdateRecruitment_NonMember_ThrowsNotFoundException()
    {
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Original Title",
        });

        await RunAsUserAsync("userB@local", Array.Empty<string>());

        var act = () => SendAsync(new UpdateRecruitmentCommand
        {
            Id = recruitmentId,
            Title = "Hacked Title",
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task UpdateRecruitment_NonExistentId_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = "Doesn't Matter",
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

using api.Application.Features.Recruitments.Commands.CloseRecruitment;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Domain.Exceptions;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Recruitments;

using static Testing;

public class CloseRecruitmentTests : BaseTestFixture
{
    [Test]
    public async Task CloseRecruitment_ActiveRecruitment_ClosesSuccessfully()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Recruitment To Close",
        });

        await SendAsync(new CloseRecruitmentCommand { RecruitmentId = recruitmentId });

        var result = await SendAsync(new GetRecruitmentByIdQuery { Id = recruitmentId });

        result.Status.Should().Be("Closed");
        result.ClosedAt.Should().NotBeNull();
    }

    [Test]
    public async Task CloseRecruitment_AlreadyClosed_ThrowsRecruitmentClosedException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Already Closed",
        });

        await SendAsync(new CloseRecruitmentCommand { RecruitmentId = recruitmentId });

        var act = () => SendAsync(new CloseRecruitmentCommand { RecruitmentId = recruitmentId });

        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }

    [Test]
    public async Task CloseRecruitment_NonMember_ThrowsNotFoundException()
    {
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Recruitment To Close",
        });

        await RunAsUserAsync("userB@local", Array.Empty<string>());

        var act = () => SendAsync(new CloseRecruitmentCommand { RecruitmentId = recruitmentId });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

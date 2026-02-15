using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Queries.GetRecruitments;

namespace api.Application.FunctionalTests.Recruitments;

using static Testing;

public class GetRecruitmentsTests : BaseTestFixture
{
    [Test]
    public async Task GetRecruitments_UserWithRecruitments_ReturnsOwnRecruitments()
    {
        await RunAsDefaultUserAsync();
        await SendAsync(new CreateRecruitmentCommand { Title = "My Recruitment 1" });
        await SendAsync(new CreateRecruitmentCommand { Title = "My Recruitment 2" });

        var result = await SendAsync(new GetRecruitmentsQuery { Page = 1, PageSize = 50 });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Select(i => i.Title).Should().Contain("My Recruitment 1").And.Contain("My Recruitment 2");
    }

    [Test]
    public async Task GetRecruitments_UserNotMemberOfAny_ReturnsEmptyList()
    {
        // User A creates a recruitment
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        await SendAsync(new CreateRecruitmentCommand { Title = "User A Recruitment" });

        // Switch to User B who has no recruitments
        await RunAsUserAsync("userB@local", Array.Empty<string>());

        var result = await SendAsync(new GetRecruitmentsQuery { Page = 1, PageSize = 50 });

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task GetRecruitments_MultipleUsers_ReturnsOnlyOwnRecruitments()
    {
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        await SendAsync(new CreateRecruitmentCommand { Title = "User A Recruitment" });

        await RunAsUserAsync("userB@local", Array.Empty<string>());
        await SendAsync(new CreateRecruitmentCommand { Title = "User B Recruitment" });

        var result = await SendAsync(new GetRecruitmentsQuery { Page = 1, PageSize = 50 });

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("User B Recruitment");
    }

    [Test]
    public async Task GetRecruitments_IncludesStepAndMemberCounts()
    {
        await RunAsDefaultUserAsync();
        await SendAsync(new CreateRecruitmentCommand
        {
            Title = "With Steps",
            Steps =
            [
                new WorkflowStepDto { Name = "Screening", Order = 1 },
                new WorkflowStepDto { Name = "Interview", Order = 2 },
            ],
        });

        var result = await SendAsync(new GetRecruitmentsQuery { Page = 1, PageSize = 50 });

        result.Items.Should().HaveCount(1);
        result.Items[0].StepCount.Should().Be(2);
        result.Items[0].MemberCount.Should().Be(1); // Creator is auto-added as member
    }
}

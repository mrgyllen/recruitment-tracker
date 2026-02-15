using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Team.Commands.AddMember;
using api.Application.Features.Team.Queries.GetMembers;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Team;

using static Testing;

public class GetMembersTests : BaseTestFixture
{
    [Test]
    public async Task GetMembers_ValidRecruitment_ReturnsCreatorAsMember()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var result = await SendAsync(new GetMembersQuery
        {
            RecruitmentId = recruitmentId,
        });

        result.Members.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Members[0].Role.Should().Be("Recruiting Leader");
        result.Members[0].IsCreator.Should().BeTrue();
    }

    [Test]
    public async Task GetMembers_WithAddedMembers_ReturnsAllMembers()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        await SendAsync(new AddMemberCommand
        {
            RecruitmentId = recruitmentId,
            UserId = Guid.NewGuid(),
            DisplayName = "Jane Doe",
        });

        var result = await SendAsync(new GetMembersQuery
        {
            RecruitmentId = recruitmentId,
        });

        result.Members.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Members.Should().Contain(m => m.DisplayName == "Jane Doe" && m.Role == "SME/Collaborator");
    }

    [Test]
    public async Task GetMembers_NonExistentRecruitment_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new GetMembersQuery
        {
            RecruitmentId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetMembers_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new GetMembersQuery
        {
            RecruitmentId = recruitmentId,
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

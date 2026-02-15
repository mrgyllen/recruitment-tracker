using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Team.Commands.AddMember;
using api.Application.Features.Team.Commands.RemoveMember;
using api.Application.Features.Team.Queries.GetMembers;
using api.Domain.Exceptions;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Team;

using static Testing;

public class RemoveMemberTests : BaseTestFixture
{
    [Test]
    public async Task RemoveMember_ValidRequest_RemovesMemberSuccessfully()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var newUserId = Guid.NewGuid();
        var memberId = await SendAsync(new AddMemberCommand
        {
            RecruitmentId = recruitmentId,
            UserId = newUserId,
            DisplayName = "To Be Removed",
        });

        await SendAsync(new RemoveMemberCommand
        {
            RecruitmentId = recruitmentId,
            MemberId = memberId,
        });

        var members = await SendAsync(new GetMembersQuery
        {
            RecruitmentId = recruitmentId,
        });
        members.Members.Should().HaveCount(1);
        members.Members.Should().NotContain(m => m.UserId == newUserId);
    }

    [Test]
    public async Task RemoveMember_NonExistentMember_ThrowsDomainRuleViolationException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var act = () => SendAsync(new RemoveMemberCommand
        {
            RecruitmentId = recruitmentId,
            MemberId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Test]
    public async Task RemoveMember_Creator_ThrowsDomainRuleViolationException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var members = await SendAsync(new GetMembersQuery
        {
            RecruitmentId = recruitmentId,
        });
        var creatorMemberId = members.Members.First(m => m.IsCreator).Id;

        var act = () => SendAsync(new RemoveMemberCommand
        {
            RecruitmentId = recruitmentId,
            MemberId = creatorMemberId,
        });

        await act.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Test]
    public async Task RemoveMember_NonExistentRecruitment_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new RemoveMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RemoveMember_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new RemoveMemberCommand
        {
            RecruitmentId = recruitmentId,
            MemberId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

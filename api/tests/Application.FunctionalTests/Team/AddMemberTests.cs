using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Team.Commands.AddMember;
using api.Application.Features.Team.Queries.GetMembers;
using api.Domain.Exceptions;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Team;

using static Testing;

public class AddMemberTests : BaseTestFixture
{
    [Test]
    public async Task AddMember_ValidRequest_AddsMemberSuccessfully()
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
            DisplayName = "New Member",
        });

        memberId.Should().NotBeEmpty();

        var members = await SendAsync(new GetMembersQuery
        {
            RecruitmentId = recruitmentId,
        });
        members.Members.Should().HaveCount(2);
        members.Members.Should().Contain(m => m.UserId == newUserId && m.DisplayName == "New Member");
    }

    [Test]
    public async Task AddMember_DuplicateUser_ThrowsDomainRuleViolationException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var newUserId = Guid.NewGuid();
        await SendAsync(new AddMemberCommand
        {
            RecruitmentId = recruitmentId,
            UserId = newUserId,
            DisplayName = "First Add",
        });

        var act = () => SendAsync(new AddMemberCommand
        {
            RecruitmentId = recruitmentId,
            UserId = newUserId,
            DisplayName = "Duplicate Add",
        });

        await act.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Test]
    public async Task AddMember_NonExistentRecruitment_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new AddMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DisplayName = "Someone",
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task AddMember_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Private Recruitment",
        });

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new AddMemberCommand
        {
            RecruitmentId = recruitmentId,
            UserId = Guid.NewGuid(),
            DisplayName = "Intruder",
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task AddMember_EmptyUserId_ThrowsValidationException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new AddMemberCommand
        {
            RecruitmentId = Guid.NewGuid(),
            UserId = Guid.Empty,
            DisplayName = "Someone",
        });

        await act.Should().ThrowAsync<ValidationException>();
    }
}

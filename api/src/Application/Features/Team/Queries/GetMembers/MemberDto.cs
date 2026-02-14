using api.Domain.Entities;

namespace api.Application.Features.Team.Queries.GetMembers;

public record MemberDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public string Role { get; init; } = null!;
    public bool IsCreator { get; init; }
    public DateTimeOffset InvitedAt { get; init; }

    public static MemberDto From(RecruitmentMember member, bool isCreator) =>
        new()
        {
            Id = member.Id,
            UserId = member.UserId,
            DisplayName = member.DisplayName,
            Role = member.Role,
            IsCreator = isCreator,
            InvitedAt = member.InvitedAt,
        };
}

using api.Domain.Common;

namespace api.Domain.Entities;

public class RecruitmentMember : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public DateTimeOffset InvitedAt { get; private set; }

    private RecruitmentMember() { } // EF Core

    internal static RecruitmentMember Create(Guid recruitmentId, Guid userId, string role, string? displayName = null)
    {
        return new RecruitmentMember
        {
            RecruitmentId = recruitmentId,
            UserId = userId,
            Role = role,
            DisplayName = displayName,
            InvitedAt = DateTimeOffset.UtcNow,
        };
    }
}

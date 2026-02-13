using api.Domain.Common;

namespace api.Domain.Entities;

public class RecruitmentMember : GuidEntity
{
    public Guid RecruitmentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;
    public DateTimeOffset InvitedAt { get; private set; }

    private RecruitmentMember() { } // EF Core

    internal static RecruitmentMember Create(Guid recruitmentId, Guid userId, string role)
    {
        return new RecruitmentMember
        {
            RecruitmentId = recruitmentId,
            UserId = userId,
            Role = role,
            InvitedAt = DateTimeOffset.UtcNow,
        };
    }
}

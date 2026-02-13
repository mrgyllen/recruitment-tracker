namespace api.Domain.Events;

public class MembershipChangedEvent : BaseEvent
{
    public Guid RecruitmentId { get; }
    public Guid UserId { get; }
    public string ChangeType { get; }

    public MembershipChangedEvent(Guid recruitmentId, Guid userId, string changeType)
    {
        RecruitmentId = recruitmentId;
        UserId = userId;
        ChangeType = changeType;
    }
}

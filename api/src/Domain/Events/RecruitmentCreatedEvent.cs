namespace api.Domain.Events;

public class RecruitmentCreatedEvent : BaseEvent
{
    public Guid RecruitmentId { get; }

    public RecruitmentCreatedEvent(Guid recruitmentId)
    {
        RecruitmentId = recruitmentId;
    }
}

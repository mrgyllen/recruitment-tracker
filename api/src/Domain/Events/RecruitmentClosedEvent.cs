namespace api.Domain.Events;

public class RecruitmentClosedEvent : BaseEvent
{
    public Guid RecruitmentId { get; }

    public RecruitmentClosedEvent(Guid recruitmentId)
    {
        RecruitmentId = recruitmentId;
    }
}

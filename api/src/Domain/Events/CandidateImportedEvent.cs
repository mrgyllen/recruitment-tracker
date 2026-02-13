namespace api.Domain.Events;

public class CandidateImportedEvent : BaseEvent
{
    public Guid CandidateId { get; }
    public Guid RecruitmentId { get; }

    public CandidateImportedEvent(Guid candidateId, Guid recruitmentId)
    {
        CandidateId = candidateId;
        RecruitmentId = recruitmentId;
    }
}

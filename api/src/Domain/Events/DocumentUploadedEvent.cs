namespace api.Domain.Events;

public class DocumentUploadedEvent : BaseEvent
{
    public Guid CandidateId { get; }
    public Guid DocumentId { get; }

    public DocumentUploadedEvent(Guid candidateId, Guid documentId)
    {
        CandidateId = candidateId;
        DocumentId = documentId;
    }
}

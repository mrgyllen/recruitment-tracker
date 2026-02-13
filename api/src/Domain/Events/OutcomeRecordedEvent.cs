namespace api.Domain.Events;

public class OutcomeRecordedEvent : BaseEvent
{
    public Guid CandidateId { get; }
    public Guid WorkflowStepId { get; }

    public OutcomeRecordedEvent(Guid candidateId, Guid workflowStepId)
    {
        CandidateId = candidateId;
        WorkflowStepId = workflowStepId;
    }
}

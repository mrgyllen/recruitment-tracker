using api.Domain.Common;
using api.Domain.Enums;

namespace api.Domain.Entities;

public class CandidateOutcome : GuidEntity
{
    public Guid CandidateId { get; private set; }
    public Guid WorkflowStepId { get; private set; }
    public OutcomeStatus Status { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public Guid RecordedByUserId { get; private set; }

    private CandidateOutcome() { } // EF Core

    internal static CandidateOutcome Create(
        Guid candidateId, Guid workflowStepId, OutcomeStatus status, Guid recordedByUserId)
    {
        return new CandidateOutcome
        {
            CandidateId = candidateId,
            WorkflowStepId = workflowStepId,
            Status = status,
            RecordedAt = DateTimeOffset.UtcNow,
            RecordedByUserId = recordedByUserId,
        };
    }
}

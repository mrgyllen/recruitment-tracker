using api.Domain.Entities;
using api.Domain.Enums;

namespace api.Application.Features.Screening.Commands.RecordOutcome;

public record OutcomeResultDto
{
    public Guid OutcomeId { get; init; }
    public Guid CandidateId { get; init; }
    public Guid WorkflowStepId { get; init; }
    public OutcomeStatus Outcome { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedBy { get; init; }
    public Guid? NewCurrentStepId { get; init; }
    public bool IsCompleted { get; init; }

    public static OutcomeResultDto From(Candidate candidate, Guid stepId)
    {
        var outcome = candidate.Outcomes.First(o => o.WorkflowStepId == stepId);
        return new OutcomeResultDto
        {
            OutcomeId = outcome.Id,
            CandidateId = candidate.Id,
            WorkflowStepId = stepId,
            Outcome = outcome.Status,
            Reason = outcome.Reason,
            RecordedAt = outcome.RecordedAt,
            RecordedBy = outcome.RecordedByUserId,
            NewCurrentStepId = candidate.CurrentWorkflowStepId,
            IsCompleted = candidate.IsCompleted,
        };
    }
}

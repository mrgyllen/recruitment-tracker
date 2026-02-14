using api.Domain.Enums;

namespace api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;

public record OutcomeHistoryDto
{
    public Guid WorkflowStepId { get; init; }
    public string WorkflowStepName { get; init; } = null!;
    public int StepOrder { get; init; }
    public OutcomeStatus Outcome { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public Guid RecordedByUserId { get; init; }
}

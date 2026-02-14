using api.Domain.Enums;

namespace api.Application.Features.Screening.Commands.RecordOutcome;

public record RecordOutcomeCommand(
    Guid RecruitmentId,
    Guid CandidateId,
    Guid WorkflowStepId,
    OutcomeStatus Outcome,
    string? Reason
) : IRequest<OutcomeResultDto>;

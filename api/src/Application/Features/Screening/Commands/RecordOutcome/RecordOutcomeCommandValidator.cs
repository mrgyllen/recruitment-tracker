using api.Domain.Enums;

namespace api.Application.Features.Screening.Commands.RecordOutcome;

public class RecordOutcomeCommandValidator : AbstractValidator<RecordOutcomeCommand>
{
    public RecordOutcomeCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.WorkflowStepId).NotEmpty();
        RuleFor(x => x.Outcome)
            .IsInEnum()
            .Must(o => o != OutcomeStatus.NotStarted)
            .WithMessage("Outcome must be Pass, Fail, or Hold.");
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => x.Reason is not null);
    }
}

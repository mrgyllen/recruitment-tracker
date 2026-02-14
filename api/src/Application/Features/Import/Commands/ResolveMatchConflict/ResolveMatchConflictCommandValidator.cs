namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public class ResolveMatchConflictCommandValidator
    : AbstractValidator<ResolveMatchConflictCommand>
{
    public ResolveMatchConflictCommandValidator()
    {
        RuleFor(x => x.ImportSessionId).NotEmpty();
        RuleFor(x => x.MatchIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Action).NotEmpty()
            .Must(a => a == "Confirm" || a == "Reject")
            .WithMessage("Action must be 'Confirm' or 'Reject'");
    }
}

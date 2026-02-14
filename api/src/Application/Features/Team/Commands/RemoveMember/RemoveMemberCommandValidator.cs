namespace api.Application.Features.Team.Commands.RemoveMember;

public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.RecruitmentId)
            .NotEmpty().WithMessage("Recruitment ID is required.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");
    }
}

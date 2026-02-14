namespace api.Application.Features.Candidates.Commands.RemoveCandidate;

public class RemoveCandidateCommandValidator : AbstractValidator<RemoveCandidateCommand>
{
    public RemoveCandidateCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}

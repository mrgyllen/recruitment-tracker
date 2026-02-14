namespace api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;

public class GetCandidateOutcomeHistoryQueryValidator : AbstractValidator<GetCandidateOutcomeHistoryQuery>
{
    public GetCandidateOutcomeHistoryQueryValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}

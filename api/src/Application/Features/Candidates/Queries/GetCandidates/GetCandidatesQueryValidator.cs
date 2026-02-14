namespace api.Application.Features.Candidates.Queries.GetCandidates;

public class GetCandidatesQueryValidator : AbstractValidator<GetCandidatesQuery>
{
    public GetCandidatesQueryValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200)
            .When(x => x.Search is not null);
    }
}

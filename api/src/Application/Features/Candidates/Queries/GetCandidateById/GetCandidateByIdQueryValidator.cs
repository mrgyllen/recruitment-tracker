namespace api.Application.Features.Candidates.Queries.GetCandidateById;

public class GetCandidateByIdQueryValidator : AbstractValidator<GetCandidateByIdQuery>
{
    public GetCandidateByIdQueryValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
    }
}

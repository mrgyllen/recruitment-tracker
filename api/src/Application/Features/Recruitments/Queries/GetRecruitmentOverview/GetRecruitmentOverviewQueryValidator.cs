namespace api.Application.Features.Recruitments.Queries.GetRecruitmentOverview;

public class GetRecruitmentOverviewQueryValidator : AbstractValidator<GetRecruitmentOverviewQuery>
{
    public GetRecruitmentOverviewQueryValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
    }
}

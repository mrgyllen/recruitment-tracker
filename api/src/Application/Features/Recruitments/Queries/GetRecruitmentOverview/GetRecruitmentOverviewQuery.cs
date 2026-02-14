namespace api.Application.Features.Recruitments.Queries.GetRecruitmentOverview;

public record GetRecruitmentOverviewQuery : IRequest<RecruitmentOverviewDto>
{
    public Guid RecruitmentId { get; init; }
}

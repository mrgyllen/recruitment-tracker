namespace api.Application.Features.Recruitments.Queries.GetRecruitments;

public record GetRecruitmentsQuery : IRequest<PaginatedRecruitmentListDto>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

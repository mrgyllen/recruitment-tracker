namespace api.Application.Features.Candidates.Queries.GetCandidates;

public record GetCandidatesQuery : IRequest<PaginatedCandidateListDto>
{
    public Guid RecruitmentId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

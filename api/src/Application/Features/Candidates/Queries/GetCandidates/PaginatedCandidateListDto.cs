namespace api.Application.Features.Candidates.Queries.GetCandidates;

public record PaginatedCandidateListDto
{
    public List<CandidateDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

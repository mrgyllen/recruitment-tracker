using api.Domain.Enums;

namespace api.Application.Features.Candidates.Queries.GetCandidates;

public record GetCandidatesQuery : IRequest<PaginatedCandidateListDto>
{
    public Guid RecruitmentId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? Search { get; init; }
    public Guid? StepId { get; init; }
    public OutcomeStatus? OutcomeStatus { get; init; }
    public bool? StaleOnly { get; init; }
}

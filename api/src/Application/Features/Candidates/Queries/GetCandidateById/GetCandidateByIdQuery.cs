namespace api.Application.Features.Candidates.Queries.GetCandidateById;

public record GetCandidateByIdQuery : IRequest<CandidateDetailDto>
{
    public Guid RecruitmentId { get; init; }
    public Guid CandidateId { get; init; }
}

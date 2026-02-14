namespace api.Application.Features.Candidates.Commands.RemoveCandidate;

public class RemoveCandidateCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
    public Guid CandidateId { get; init; }
}

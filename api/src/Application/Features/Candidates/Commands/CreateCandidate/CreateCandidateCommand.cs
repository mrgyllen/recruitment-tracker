namespace api.Application.Features.Candidates.Commands.CreateCandidate;

public record CreateCandidateCommand : IRequest<Guid>
{
    public Guid RecruitmentId { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset? DateApplied { get; init; }
}

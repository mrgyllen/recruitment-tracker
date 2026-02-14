namespace api.Application.Features.Team.Commands.AddMember;

public record AddMemberCommand : IRequest<Guid>
{
    public Guid RecruitmentId { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = null!;
}

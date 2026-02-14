namespace api.Application.Features.Team.Commands.RemoveMember;

public record RemoveMemberCommand : IRequest
{
    public Guid RecruitmentId { get; init; }
    public Guid MemberId { get; init; }
}

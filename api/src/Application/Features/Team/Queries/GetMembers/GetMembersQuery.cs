namespace api.Application.Features.Team.Queries.GetMembers;

public record GetMembersQuery : IRequest<MembersListDto>
{
    public Guid RecruitmentId { get; init; }
}

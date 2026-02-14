namespace api.Application.Features.Team.Queries.GetMembers;

public record MembersListDto
{
    public List<MemberDto> Members { get; init; } = [];
    public int TotalCount { get; init; }
}

using api.Application.Features.Team.Commands.AddMember;
using api.Application.Features.Team.Commands.RemoveMember;
using api.Application.Features.Team.Queries.GetMembers;
using api.Application.Features.Team.Queries.SearchDirectory;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class TeamEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet("/members", GetMembers);
        group.MapGet("/directory-search", SearchDirectory);
        group.MapPost("/members", AddMember);
        group.MapDelete("/members/{memberId:guid}", RemoveMember);
    }

    private static async Task<IResult> GetMembers(
        ISender sender,
        Guid recruitmentId)
    {
        var result = await sender.Send(new GetMembersQuery { RecruitmentId = recruitmentId });
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchDirectory(
        ISender sender,
        Guid recruitmentId,
        string q)
    {
        var result = await sender.Send(new SearchDirectoryQuery { SearchTerm = q });
        return Results.Ok(result);
    }

    private static async Task<IResult> AddMember(
        ISender sender,
        Guid recruitmentId,
        AddMemberCommand command)
    {
        var memberId = await sender.Send(command with { RecruitmentId = recruitmentId });
        return Results.Created(
            $"/api/recruitments/{recruitmentId}/members/{memberId}",
            new { id = memberId });
    }

    private static async Task<IResult> RemoveMember(
        ISender sender,
        Guid recruitmentId,
        Guid memberId)
    {
        await sender.Send(new RemoveMemberCommand { RecruitmentId = recruitmentId, MemberId = memberId });
        return Results.NoContent();
    }
}

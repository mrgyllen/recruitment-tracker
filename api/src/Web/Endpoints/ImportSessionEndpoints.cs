using api.Application.Features.Import.Commands.ResolveMatchConflict;
using api.Application.Features.Import.Queries.GetImportSession;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class ImportSessionEndpoints : EndpointGroupBase
{
    public override string? GroupName => "import-sessions";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", GetImportSession);
        group.MapPost("/{id:guid}/resolve-match", ResolveMatchConflict);
    }

    private static async Task<IResult> GetImportSession(
        ISender sender,
        Guid id)
    {
        var result = await sender.Send(new GetImportSessionQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> ResolveMatchConflict(
        ISender sender,
        Guid id,
        ResolveMatchConflictRequest request)
    {
        var result = await sender.Send(new ResolveMatchConflictCommand(
            id, request.MatchIndex, request.Action));
        return Results.Ok(result);
    }
}

public record ResolveMatchConflictRequest(int MatchIndex, string Action);

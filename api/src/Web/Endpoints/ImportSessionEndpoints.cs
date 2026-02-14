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
    }

    private static async Task<IResult> GetImportSession(
        ISender sender,
        Guid id)
    {
        var result = await sender.Send(new GetImportSessionQuery(id));
        return Results.Ok(result);
    }
}

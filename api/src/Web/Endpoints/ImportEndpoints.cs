using api.Application.Features.Import.Commands.StartImport;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class ImportEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}/import";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", StartImport)
            .DisableAntiforgery()
            .RequireRateLimiting(RateLimitPolicies.FileUpload);
    }

    private static async Task<IResult> StartImport(
        ISender sender,
        Guid recruitmentId,
        IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var result = await sender.Send(new StartImportCommand(
            recruitmentId,
            ms.ToArray(),
            file.FileName,
            file.Length));

        return Results.Accepted(
            result.StatusUrl,
            result);
    }
}

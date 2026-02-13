using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Application.Features.Recruitments.Queries.GetRecruitments;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class RecruitmentEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateRecruitment);
        group.MapGet("/{id:guid}", GetRecruitmentById);
        group.MapGet("/", GetRecruitments);
    }

    private static async Task<IResult> CreateRecruitment(
        ISender sender,
        CreateRecruitmentCommand command)
    {
        var id = await sender.Send(command);
        return Results.Created($"/api/recruitments/{id}", new { id });
    }

    private static async Task<IResult> GetRecruitmentById(
        ISender sender,
        Guid id)
    {
        var result = await sender.Send(new GetRecruitmentByIdQuery { Id = id });
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecruitments(
        ISender sender,
        int page = 1,
        int pageSize = 50)
    {
        var result = await sender.Send(new GetRecruitmentsQuery { Page = page, PageSize = pageSize });
        return Results.Ok(result);
    }
}

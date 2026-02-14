using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Commands.RemoveWorkflowStep;
using api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;
using api.Application.Features.Recruitments.Commands.UpdateRecruitment;
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
        group.MapPut("/{id:guid}", UpdateRecruitment);
        group.MapPost("/{id:guid}/steps", AddWorkflowStep);
        group.MapDelete("/{id:guid}/steps/{stepId:guid}", RemoveWorkflowStep);
        group.MapPut("/{id:guid}/steps/reorder", ReorderWorkflowSteps);
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

    private static async Task<IResult> UpdateRecruitment(
        ISender sender,
        Guid id,
        UpdateRecruitmentCommand command)
    {
        await sender.Send(command with { Id = id });
        return Results.NoContent();
    }

    private static async Task<IResult> AddWorkflowStep(
        ISender sender,
        Guid id,
        AddWorkflowStepCommand command)
    {
        var result = await sender.Send(command with { RecruitmentId = id });
        return Results.Created($"/api/recruitments/{id}/steps/{result.Id}", result);
    }

    private static async Task<IResult> RemoveWorkflowStep(
        ISender sender,
        Guid id,
        Guid stepId)
    {
        await sender.Send(new RemoveWorkflowStepCommand { RecruitmentId = id, StepId = stepId });
        return Results.NoContent();
    }

    private static async Task<IResult> ReorderWorkflowSteps(
        ISender sender,
        Guid id,
        ReorderWorkflowStepsCommand command)
    {
        await sender.Send(command with { RecruitmentId = id });
        return Results.NoContent();
    }
}

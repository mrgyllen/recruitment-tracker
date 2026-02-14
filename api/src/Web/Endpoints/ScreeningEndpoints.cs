using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;
using api.Domain.Enums;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class ScreeningEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}/candidates/{candidateId:guid}/screening";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/outcome", RecordOutcome);
        group.MapGet("/history", GetOutcomeHistory);
    }

    private static async Task<IResult> RecordOutcome(
        Guid recruitmentId,
        Guid candidateId,
        RecordOutcomeRequest request,
        ISender sender)
    {
        var command = new RecordOutcomeCommand(
            recruitmentId, candidateId,
            request.WorkflowStepId, request.Outcome, request.Reason);
        var result = await sender.Send(command);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetOutcomeHistory(
        Guid recruitmentId,
        Guid candidateId,
        ISender sender)
    {
        var query = new GetCandidateOutcomeHistoryQuery(recruitmentId, candidateId);
        var result = await sender.Send(query);
        return Results.Ok(result);
    }
}

public record RecordOutcomeRequest(
    Guid WorkflowStepId,
    OutcomeStatus Outcome,
    string? Reason);

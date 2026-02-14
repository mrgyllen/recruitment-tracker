using api.Application.Features.Candidates.Commands.AssignDocument;
using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Candidates.Commands.RemoveCandidate;
using api.Application.Features.Candidates.Commands.UploadDocument;
using api.Application.Features.Candidates.Queries.GetCandidates;
using api.Web.Infrastructure;
using MediatR;

namespace api.Web.Endpoints;

public class CandidateEndpoints : EndpointGroupBase
{
    public override string? GroupName => "recruitments/{recruitmentId:guid}/candidates";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateCandidate);
        group.MapDelete("/{candidateId:guid}", RemoveCandidate);
        group.MapGet("/", GetCandidates);
        group.MapPost("/{candidateId:guid}/document", UploadDocument)
            .DisableAntiforgery()
            .RequireRateLimiting(RateLimitPolicies.FileUpload);
        group.MapPost("/{candidateId:guid}/document/assign", AssignDocument);
    }

    private static async Task<IResult> CreateCandidate(
        ISender sender,
        Guid recruitmentId,
        CreateCandidateCommand command)
    {
        var id = await sender.Send(command with { RecruitmentId = recruitmentId });
        return Results.Created(
            $"/api/recruitments/{recruitmentId}/candidates/{id}",
            new { id });
    }

    private static async Task<IResult> RemoveCandidate(
        ISender sender,
        Guid recruitmentId,
        Guid candidateId)
    {
        await sender.Send(new RemoveCandidateCommand
        {
            RecruitmentId = recruitmentId,
            CandidateId = candidateId
        });
        return Results.NoContent();
    }

    private static async Task<IResult> GetCandidates(
        ISender sender,
        Guid recruitmentId,
        int page = 1,
        int pageSize = 50)
    {
        var result = await sender.Send(new GetCandidatesQuery
        {
            RecruitmentId = recruitmentId,
            Page = page,
            PageSize = pageSize
        });
        return Results.Ok(result);
    }

    private static async Task<IResult> UploadDocument(
        ISender sender,
        Guid recruitmentId,
        Guid candidateId,
        IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await sender.Send(new UploadDocumentCommand(
            recruitmentId, candidateId, stream, file.FileName, file.Length));
        return Results.Ok(result);
    }

    private static async Task<IResult> AssignDocument(
        ISender sender,
        Guid recruitmentId,
        Guid candidateId,
        AssignDocumentCommand command)
    {
        var result = await sender.Send(command with
        {
            RecruitmentId = recruitmentId,
            CandidateId = candidateId
        });
        return Results.Ok(result);
    }
}

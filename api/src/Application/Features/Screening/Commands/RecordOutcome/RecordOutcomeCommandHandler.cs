using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Screening.Commands.RecordOutcome;

public class RecordOutcomeCommandHandler(
    IApplicationDbContext context,
    ITenantContext tenantContext)
    : IRequestHandler<RecordOutcomeCommand, OutcomeResultDto>
{
    public async Task<OutcomeResultDto> Handle(RecordOutcomeCommand request, CancellationToken ct)
    {
        var recruitment = await context.Recruitments
            .Include(r => r.Members)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        if (!recruitment.Members.Any(m => m.UserId == tenantContext.UserGuid))
            throw new ForbiddenAccessException();

        if (recruitment.Status == RecruitmentStatus.Closed)
            throw new api.Domain.Exceptions.RecruitmentClosedException(recruitment.Id);

        if (!recruitment.Steps.Any(s => s.Id == request.WorkflowStepId))
            throw new NotFoundException(nameof(WorkflowStep), request.WorkflowStepId);

        var candidate = await context.Candidates
            .Include(c => c.Outcomes)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        candidate.RecordOutcome(
            request.WorkflowStepId,
            request.Outcome,
            tenantContext.UserGuid!.Value,
            request.Reason,
            orderedSteps);

        await context.SaveChangesAsync(ct);

        return OutcomeResultDto.From(candidate, request.WorkflowStepId);
    }
}

using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;

public class GetCandidateOutcomeHistoryQueryHandler(
    IApplicationDbContext context,
    ITenantContext tenantContext)
    : IRequestHandler<GetCandidateOutcomeHistoryQuery, List<OutcomeHistoryDto>>
{
    public async Task<List<OutcomeHistoryDto>> Handle(
        GetCandidateOutcomeHistoryQuery request, CancellationToken ct)
    {
        var recruitment = await context.Recruitments
            .Include(r => r.Members)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        if (!recruitment.Members.Any(m => m.UserId == tenantContext.UserGuid))
            throw new ForbiddenAccessException();

        var candidate = await context.Candidates
            .Include(c => c.Outcomes)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        var stepLookup = recruitment.Steps.ToDictionary(s => s.Id);

        return candidate.Outcomes
            .Where(o => o.Status != OutcomeStatus.NotStarted)
            .Select(o =>
            {
                stepLookup.TryGetValue(o.WorkflowStepId, out var step);
                return new OutcomeHistoryDto
                {
                    WorkflowStepId = o.WorkflowStepId,
                    WorkflowStepName = step?.Name ?? "Unknown",
                    StepOrder = step?.Order ?? 0,
                    Outcome = o.Status,
                    Reason = o.Reason,
                    RecordedAt = o.RecordedAt,
                    RecordedByUserId = o.RecordedByUserId,
                };
            })
            .OrderBy(h => h.StepOrder)
            .ToList();
    }
}

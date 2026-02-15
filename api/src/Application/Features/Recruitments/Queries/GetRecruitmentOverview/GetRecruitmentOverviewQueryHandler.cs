using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Entities;
using api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Queries.GetRecruitmentOverview;

public class GetRecruitmentOverviewQueryHandler : IRequestHandler<GetRecruitmentOverviewQuery, RecruitmentOverviewDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly OverviewSettings _settings;

    public GetRecruitmentOverviewQueryHandler(
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        IOptions<OverviewSettings> settings)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _settings = settings.Value;
    }

    public async Task<RecruitmentOverviewDto> Handle(
        GetRecruitmentOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var recruitment = await _dbContext.Recruitments
            .Include(r => r.Members)
            .Include(r => r.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken);

        if (recruitment is null)
        {
            throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);
        }

        var userId = _tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        var staleCutoff = DateTimeOffset.UtcNow.AddDays(-_settings.StaleDays);

        var candidateStepData = await _dbContext.Candidates
            .Where(c => c.RecruitmentId == request.RecruitmentId)
            .Where(c => c.CurrentWorkflowStepId != null)
            .GroupBy(c => c.CurrentWorkflowStepId)
            .Select(g => new
            {
                StepId = g.Key,
                TotalCandidates = g.Count(),
                PendingCount = g.Count(c =>
                    !c.Outcomes.Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId)),
                StaleCount = g.Count(c =>
                    !c.Outcomes.Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId) &&
                    c.DateApplied < staleCutoff),
                PassCount = g.Count(c =>
                    c.Outcomes.Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId &&
                                       o.Status == OutcomeStatus.Pass)),
                FailCount = g.Count(c =>
                    c.Outcomes.Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId &&
                                       o.Status == OutcomeStatus.Fail)),
                HoldCount = g.Count(c =>
                    c.Outcomes.Any(o => o.WorkflowStepId == c.CurrentWorkflowStepId &&
                                       o.Status == OutcomeStatus.Hold)),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var stepDataLookup = candidateStepData.ToDictionary(s => s.StepId!.Value);

        var steps = recruitment.Steps
            .OrderBy(s => s.Order)
            .Select(s =>
            {
                var hasData = stepDataLookup.TryGetValue(s.Id, out var data);
                return new WorkflowStepOverviewDto
                {
                    StepId = s.Id,
                    StepName = s.Name,
                    StepOrder = s.Order,
                    TotalCandidates = hasData ? data!.TotalCandidates : 0,
                    PendingCount = hasData ? data!.PendingCount : 0,
                    StaleCount = hasData ? data!.StaleCount : 0,
                    OutcomeBreakdown = new OutcomeBreakdownDto
                    {
                        NotStarted = hasData ? data!.PendingCount : 0,
                        Pass = hasData ? data!.PassCount : 0,
                        Fail = hasData ? data!.FailCount : 0,
                        Hold = hasData ? data!.HoldCount : 0,
                    },
                };
            })
            .ToList();

        return new RecruitmentOverviewDto
        {
            RecruitmentId = recruitment.Id,
            TotalCandidates = steps.Sum(s => s.TotalCandidates),
            PendingActionCount = steps.Sum(s => s.PendingCount),
            TotalStale = steps.Sum(s => s.StaleCount),
            StaleDays = _settings.StaleDays,
            Steps = steps,
        };
    }
}

using api.Application.Common.Interfaces;
using api.Domain.Entities;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Queries.GetCandidateById;

public class GetCandidateByIdQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetCandidateByIdQuery, CandidateDetailDto>
{
    private const string ContainerName = "documents";
    private static readonly TimeSpan SasValidity = TimeSpan.FromMinutes(15);

    public async Task<CandidateDetailDto> Handle(
        GetCandidateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        var candidate = await dbContext.Candidates
            .Include(c => c.Documents)
            .Include(c => c.Outcomes)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == request.CandidateId && c.RecruitmentId == request.RecruitmentId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        var (currentStep, outcomeStatus) = CandidateDto.ComputeCurrentStep(candidate, orderedSteps);

        var stepLookup = orderedSteps.ToDictionary(s => s.Id);

        return new CandidateDetailDto
        {
            Id = candidate.Id,
            RecruitmentId = candidate.RecruitmentId,
            FullName = candidate.FullName!,
            Email = candidate.Email!,
            PhoneNumber = candidate.PhoneNumber,
            Location = candidate.Location,
            DateApplied = candidate.DateApplied,
            CreatedAt = candidate.CreatedAt,
            CurrentWorkflowStepId = currentStep?.Id,
            CurrentWorkflowStepName = currentStep?.Name,
            CurrentOutcomeStatus = outcomeStatus?.ToString(),
            Documents = candidate.Documents.Select(d => new DocumentDetailDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                SasUrl = blobStorage.GenerateSasUri(ContainerName, d.BlobStorageUrl, SasValidity).ToString(),
                UploadedAt = d.UploadedAt,
            }).ToList(),
            OutcomeHistory = candidate.Outcomes
                .OrderBy(o => stepLookup.TryGetValue(o.WorkflowStepId, out var s) ? s.Order : 0)
                .ThenBy(o => o.RecordedAt)
                .Select(o =>
                {
                    stepLookup.TryGetValue(o.WorkflowStepId, out var step);
                    return new OutcomeHistoryDto
                    {
                        WorkflowStepId = o.WorkflowStepId,
                        WorkflowStepName = step?.Name ?? "Unknown",
                        StepOrder = step?.Order ?? 0,
                        Status = o.Status.ToString(),
                        RecordedAt = o.RecordedAt,
                        RecordedByUserId = o.RecordedByUserId,
                    };
                }).ToList(),
        };
    }
}

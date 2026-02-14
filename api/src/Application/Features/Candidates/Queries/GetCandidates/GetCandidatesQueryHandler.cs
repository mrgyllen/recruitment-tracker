using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Queries.GetCandidates;

public class GetCandidatesQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetCandidatesQuery, PaginatedCandidateListDto>
{
    private const string ContainerName = "documents";
    private static readonly TimeSpan SasValidity = TimeSpan.FromMinutes(15);
    public async Task<PaginatedCandidateListDto> Handle(
        GetCandidatesQuery request,
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

        var orderedSteps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var query = dbContext.Candidates
            .Include(c => c.Documents)
            .Include(c => c.Outcomes)
            .Where(c => c.RecruitmentId == request.RecruitmentId)
            .AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            query = query.Where(c =>
                c.FullName!.Contains(searchTerm) ||
                c.Email!.Contains(searchTerm));
        }

        // In-memory filtering: current step is derived from outcome history
        // (not a DB column), so step/outcome filters can't be translated to SQL.
        // Acceptable at current scale (max ~150 candidates per recruitment).
        var allCandidates = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        // Apply step and outcome filters in memory (current step is derived)
        IEnumerable<Candidate> filtered = allCandidates;

        if (request.StepId.HasValue || request.OutcomeStatus.HasValue)
        {
            filtered = allCandidates.Where(c =>
            {
                var (step, status) = CandidateDto.ComputeCurrentStep(c, orderedSteps);

                if (request.StepId.HasValue && step?.Id != request.StepId.Value)
                    return false;

                if (request.OutcomeStatus.HasValue && status != request.OutcomeStatus.Value)
                    return false;

                return true;
            });
        }

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        var items = filteredList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PaginatedCandidateListDto
        {
            Items = items.Select(c =>
            {
                var doc = c.Documents.FirstOrDefault();
                var sasUrl = doc is not null
                    ? blobStorage.GenerateSasUri(ContainerName, doc.BlobStorageUrl, SasValidity).ToString()
                    : null;
                return CandidateDto.From(c, orderedSteps, sasUrl);
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}

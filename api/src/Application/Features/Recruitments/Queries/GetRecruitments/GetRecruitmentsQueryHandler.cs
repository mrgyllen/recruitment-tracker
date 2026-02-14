using api.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Application.Features.Recruitments.Queries.GetRecruitments;

public class GetRecruitmentsQueryHandler : IRequestHandler<GetRecruitmentsQuery, PaginatedRecruitmentListDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetRecruitmentsQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<PaginatedRecruitmentListDto> Handle(GetRecruitmentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _tenantContext.UserGuid;

        var query = _dbContext.Recruitments
            .Include(r => r.Steps)
            .Include(r => r.Members)
            .Where(r => userId != null && r.Members.Any(m => m.UserId == userId))
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedRecruitmentListDto
        {
            Items = items.Select(RecruitmentListItemDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}

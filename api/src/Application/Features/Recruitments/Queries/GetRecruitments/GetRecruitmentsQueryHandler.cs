using api.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Application.Features.Recruitments.Queries.GetRecruitments;

public class GetRecruitmentsQueryHandler : IRequestHandler<GetRecruitmentsQuery, PaginatedRecruitmentListDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetRecruitmentsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedRecruitmentListDto> Handle(GetRecruitmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Recruitments
            .Include(r => r.Steps)
            .Include(r => r.Members)
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

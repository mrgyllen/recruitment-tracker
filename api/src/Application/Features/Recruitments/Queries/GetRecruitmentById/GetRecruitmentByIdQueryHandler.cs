using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Recruitments.Queries.GetRecruitmentById;

public class GetRecruitmentByIdQueryHandler : IRequestHandler<GetRecruitmentByIdQuery, RecruitmentDetailDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetRecruitmentByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RecruitmentDetailDto> Handle(GetRecruitmentByIdQuery request, CancellationToken cancellationToken)
    {
        var recruitment = await _dbContext.Recruitments
            .Include(r => r.Steps)
            .Include(r => r.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (recruitment is null)
        {
            throw new NotFoundException(nameof(Recruitment), request.Id);
        }

        return RecruitmentDetailDto.From(recruitment);
    }
}

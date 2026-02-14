using api.Application.Common.Interfaces;
using api.Domain.Entities;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Queries.GetImportSession;

public class GetImportSessionQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetImportSessionQuery, ImportSessionDto>
{
    public async Task<ImportSessionDto> Handle(
        GetImportSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportSession), request.Id);

        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == session.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), session.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        return ImportSessionDto.From(session);
    }
}

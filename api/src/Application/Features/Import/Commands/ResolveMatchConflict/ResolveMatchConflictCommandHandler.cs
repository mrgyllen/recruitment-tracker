using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.ValueObjects;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Commands.ResolveMatchConflict;

public class ResolveMatchConflictCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<ResolveMatchConflictCommand, ResolveMatchResultDto>
{
    public async Task<ResolveMatchResultDto> Handle(
        ResolveMatchConflictCommand request,
        CancellationToken cancellationToken)
    {
        var session = await dbContext.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportSession), request.ImportSessionId);

        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == session.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), session.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        ImportRowResult row;
        if (request.Action == "Confirm")
        {
            row = session.ConfirmMatch(request.MatchIndex);
        }
        else
        {
            row = session.RejectMatch(request.MatchIndex);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResolveMatchResultDto(
            request.MatchIndex,
            row.Resolution!,
            row.CandidateEmail);
    }
}

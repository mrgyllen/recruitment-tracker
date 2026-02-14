using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Team.Commands.RemoveMember;

public class RemoveMemberCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(
        RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        recruitment.RemoveMember(request.MemberId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

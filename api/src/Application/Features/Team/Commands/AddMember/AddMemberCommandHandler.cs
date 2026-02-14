using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Team.Commands.AddMember;

public class AddMemberCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<AddMemberCommand, Guid>
{
    public async Task<Guid> Handle(
        AddMemberCommand request, CancellationToken cancellationToken)
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

        recruitment.AddMember(request.UserId, "SME/Collaborator", request.DisplayName);
        await dbContext.SaveChangesAsync(cancellationToken);

        return recruitment.Members.First(m => m.UserId == request.UserId).Id;
    }
}

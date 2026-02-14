using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Team.Queries.GetMembers;

public class GetMembersQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetMembersQuery, MembersListDto>
{
    public async Task<MembersListDto> Handle(
        GetMembersQuery request, CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenAccessException();
        }

        var members = recruitment.Members
            .Select(m => MemberDto.From(m, m.UserId == recruitment.CreatedByUserId))
            .OrderBy(m => m.InvitedAt)
            .ToList();

        return new MembersListDto
        {
            Members = members,
            TotalCount = members.Count,
        };
    }
}

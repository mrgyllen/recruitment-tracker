using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Commands.RemoveCandidate;

public class RemoveCandidateCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<RemoveCandidateCommand>
{
    public async Task Handle(
        RemoveCandidateCommand request,
        CancellationToken cancellationToken)
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

        if (recruitment.Status == RecruitmentStatus.Closed)
        {
            throw new RecruitmentClosedException(recruitment.Id);
        }

        var candidate = await dbContext.Candidates
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        dbContext.Candidates.Remove(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

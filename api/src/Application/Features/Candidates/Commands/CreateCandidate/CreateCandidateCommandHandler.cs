using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Commands.CreateCandidate;

public class CreateCandidateCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<CreateCandidateCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCandidateCommand request,
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

        if (recruitment.Status == RecruitmentStatus.Closed)
        {
            throw new RecruitmentClosedException(recruitment.Id);
        }

        var emailExists = await dbContext.Candidates
            .AnyAsync(c => c.RecruitmentId == request.RecruitmentId
                && c.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            throw new DuplicateCandidateException(request.Email, request.RecruitmentId);
        }

        var dateApplied = request.DateApplied ?? DateTimeOffset.UtcNow;
        var candidate = Candidate.Create(
            request.RecruitmentId,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.Location,
            dateApplied);

        var firstStep = recruitment.Steps.OrderBy(s => s.Order).FirstOrDefault();
        if (firstStep is not null)
        {
            candidate.RecordOutcome(firstStep.Id, OutcomeStatus.NotStarted, userId.Value);
        }

        dbContext.Candidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return candidate.Id;
    }
}

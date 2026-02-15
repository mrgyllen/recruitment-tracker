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

            // AC7: Update matched candidate's profile from import data
            if (row.MatchedCandidateId is not null)
            {
                var candidate = await dbContext.Candidates
                    .FirstOrDefaultAsync(c => c.Id == row.MatchedCandidateId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Candidate), row.MatchedCandidateId);

                candidate.UpdateProfile(
                    row.FullName ?? candidate.FullName!,
                    row.PhoneNumber,
                    row.Location,
                    row.DateApplied ?? DateTimeOffset.UtcNow);
            }
        }
        else
        {
            row = session.RejectMatch(request.MatchIndex);

            // AC8: Create new candidate from import data (skip if email already exists)
            var email = row.CandidateEmail ?? "unknown@import.local";
            var emailExists = await dbContext.Candidates
                .AnyAsync(c => c.RecruitmentId == session.RecruitmentId
                    && c.Email == email, cancellationToken);

            if (!emailExists)
            {
                var newCandidate = Candidate.Create(
                    session.RecruitmentId,
                    row.FullName ?? "Unknown",
                    email,
                    row.PhoneNumber,
                    row.Location,
                    row.DateApplied ?? DateTimeOffset.UtcNow);

                dbContext.Candidates.Add(newCandidate);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResolveMatchResultDto(
            request.MatchIndex,
            row.Resolution!,
            row.CandidateEmail);
    }
}

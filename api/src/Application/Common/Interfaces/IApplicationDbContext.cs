using api.Domain.Entities;

namespace api.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Recruitment> Recruitments { get; }
    DbSet<Candidate> Candidates { get; }
    DbSet<ImportSession> ImportSessions { get; }
    DbSet<AuditEntry> AuditEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

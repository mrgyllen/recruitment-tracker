using System.Reflection;
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Recruitment> Recruitments => Set<Recruitment>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<ImportSession> ImportSessions => Set<ImportSession>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter on Recruitment -- defense-in-depth for handler auth checks
        builder.Entity<Recruitment>().HasQueryFilter(r =>
            _tenantContext.IsServiceContext ||
            (_tenantContext.RecruitmentId != null && r.Id == _tenantContext.RecruitmentId) ||
            (_tenantContext.UserGuid != null &&
             r.Members.Any(m => m.UserId == _tenantContext.UserGuid))
        );

        // Global query filter on ImportSession -- scoped via Recruitment membership
        builder.Entity<ImportSession>().HasQueryFilter(s =>
            _tenantContext.IsServiceContext ||
            (_tenantContext.RecruitmentId != null && s.RecruitmentId == _tenantContext.RecruitmentId) ||
            (_tenantContext.UserGuid != null &&
             EF.Property<Recruitment>(s, "Recruitment").Members
                .Any(m => m.UserId == _tenantContext.UserGuid))
        );

        // Global query filter on Candidate -- the security boundary
        builder.Entity<Candidate>().HasQueryFilter(c =>
            // Service context bypasses all filters (GDPR job)
            _tenantContext.IsServiceContext ||
            // Import service scoped to specific recruitment
            (_tenantContext.RecruitmentId != null && c.RecruitmentId == _tenantContext.RecruitmentId) ||
            // Web user: only candidates in recruitments where user is a member
            (_tenantContext.UserGuid != null &&
             EF.Property<Recruitment>(c, "Recruitment").Members
                .Any(m => m.UserId == _tenantContext.UserGuid))
        );
    }
}

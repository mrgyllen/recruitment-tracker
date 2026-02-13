using System.Reflection;
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
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

        // Global query filter on Candidate -- the security boundary
        builder.Entity<Candidate>().HasQueryFilter(c =>
            // Service context bypasses all filters (GDPR job)
            _tenantContext.IsServiceContext ||
            // Import service scoped to specific recruitment
            (_tenantContext.RecruitmentId != null && c.RecruitmentId == _tenantContext.RecruitmentId) ||
            // Web user: only candidates in recruitments where user is a member
            (_tenantContext.UserId != null &&
             EF.Property<Recruitment>(c, "Recruitment").Members
                .Any(m => m.UserId.ToString() == _tenantContext.UserId))
        );
    }
}

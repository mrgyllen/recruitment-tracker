using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using Testcontainers.MsSql;

namespace api.Infrastructure.IntegrationTests.Data;

[TestFixture]
public class TenantContextFilterTests
{
    private MsSqlContainer _sqlContainer = null!;
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _sqlContainer.StartAsync();
        _connectionString = _sqlContainer.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _sqlContainer.DisposeAsync();
    }

    private ApplicationDbContext CreateDbContext(ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new ApplicationDbContext(options, tenantContext);
    }

    private async Task SeedDatabase()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsServiceContext.Returns(true); // Bypass filter for seeding

        using var ctx = CreateDbContext(tenantContext);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();

        // Create Recruitment A with User A as member
        var recruitmentA = Recruitment.Create("Recruitment A", null, Guid.Parse("aaaa0000-0000-0000-0000-000000000001"));
        ctx.Recruitments.Add(recruitmentA);

        // Create Recruitment B with User B as member
        var recruitmentB = Recruitment.Create("Recruitment B", null, Guid.Parse("bbbb0000-0000-0000-0000-000000000002"));
        ctx.Recruitments.Add(recruitmentB);

        await ctx.SaveChangesAsync(CancellationToken.None);

        // Add candidates to each recruitment (bypass filter via service context)
        var candidateA = Candidate.Create(recruitmentA.Id, "Alice", "alice@a.com", null, null, DateTimeOffset.UtcNow);
        var candidateB = Candidate.Create(recruitmentB.Id, "Bob", "bob@b.com", null, null, DateTimeOffset.UtcNow);
        ctx.Candidates.Add(candidateA);
        ctx.Candidates.Add(candidateB);

        await ctx.SaveChangesAsync(CancellationToken.None);
    }

    [SetUp]
    public async Task SetUp()
    {
        await SeedDatabase();
    }

    [Test]
    public async Task UserInRecruitmentA_CannotSeeCandidatesFromRecruitmentB()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.UserId.Returns("aaaa0000-0000-0000-0000-000000000001");
        tenantContext.IsServiceContext.Returns(false);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().OnlyContain(c => c.Email == "alice@a.com");
    }

    [Test]
    public async Task ImportService_ScopedToRecruitment()
    {
        var serviceCtx = Substitute.For<ITenantContext>();
        serviceCtx.IsServiceContext.Returns(true);

        // Get recruitment A's ID
        using var seedCtx = CreateDbContext(serviceCtx);
        var recruitmentA = await seedCtx.Recruitments.FirstAsync(r => r.Title == "Recruitment A");

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.RecruitmentId.Returns(recruitmentA.Id);
        tenantContext.IsServiceContext.Returns(false);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().OnlyContain(c => c.Email == "alice@a.com");
    }

    [Test]
    public async Task GdprService_CanQueryAllRecruitments()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsServiceContext.Returns(true);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().HaveCount(2);
    }

    [Test]
    public async Task MisconfiguredContext_ReturnsZeroResults()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.UserId.Returns((string?)null);
        tenantContext.RecruitmentId.Returns((Guid?)null);
        tenantContext.IsServiceContext.Returns(false);

        using var ctx = CreateDbContext(tenantContext);
        var candidates = await ctx.Candidates.ToListAsync();

        candidates.Should().BeEmpty();
    }
}

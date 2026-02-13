using api.Application.Common.Behaviours;
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Common.Behaviours;

// Test command/query types
public record TestCommand : IRequest<string>;
public record TestQuery : IRequest<string>;

public class AuditBehaviourTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.UserId.Returns("user-123");

        // Mock DbSet<AuditEntry>
        var auditEntries = Substitute.For<DbSet<AuditEntry>>();
        _dbContext.AuditEntries.Returns(auditEntries);
    }

    [Test]
    public async Task Handle_Command_CreatesAuditEntry()
    {
        var behaviour = new AuditBehaviour<TestCommand, string>(_dbContext, _tenantContext);
        var request = new TestCommand();

        await behaviour.Handle(request, (_) => Task.FromResult("ok"), CancellationToken.None);

        _dbContext.AuditEntries.Received(1).Add(Arg.Any<AuditEntry>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_Query_DoesNotCreateAuditEntry()
    {
        var behaviour = new AuditBehaviour<TestQuery, string>(_dbContext, _tenantContext);
        var request = new TestQuery();

        await behaviour.Handle(request, (_) => Task.FromResult("ok"), CancellationToken.None);

        _dbContext.AuditEntries.DidNotReceive().Add(Arg.Any<AuditEntry>());
    }
}

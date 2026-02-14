using api.Application.Common.Interfaces;
using api.Application.Features.Import.Commands.ResolveMatchConflict;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Import.Commands.ResolveMatchConflict;

[TestFixture]
public class ResolveMatchConflictCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
    }

    [Test]
    public async Task Handle_ConfirmMatch_SetsResolutionToConfirmed()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var session = ImportSession.Create(recruitment.Id, userId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(1, "flagged@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Confirm");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Action.Should().Be("Confirmed");
        result.MatchIndex.Should().Be(0);
        result.CandidateEmail.Should().Be("flagged@test.com");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RejectMatch_SetsResolutionToRejected()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var session = ImportSession.Create(recruitment.Id, userId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(1, "flagged@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Reject");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Action.Should().Be("Rejected");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_SessionNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        var sessionMockSet = new List<ImportSession>().AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(Guid.NewGuid(), 0, "Confirm");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var session = ImportSession.Create(recruitment.Id, creatorId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(1, "flagged@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Confirm");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

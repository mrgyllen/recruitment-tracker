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

        // No matched candidate (MatchedCandidateId is null) — skip UpdateProfile
        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Confirm");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Action.Should().Be("Confirmed");
        result.MatchIndex.Should().Be(0);
        result.CandidateEmail.Should().Be("flagged@test.com");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ConfirmMatch_UpdatesMatchedCandidateProfile()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        // Create existing candidate that was matched
        var matchedCandidate = Candidate.Create(
            recruitment.Id, "Old Name", "flagged@test.com",
            "+46700000000", "Malmo", DateTimeOffset.UtcNow.AddDays(-30));
        var candidateMockSet = new List<Candidate> { matchedCandidate }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var dateApplied = new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero);
        var session = ImportSession.Create(recruitment.Id, userId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(
            1, "flagged@test.com", ImportRowAction.Flagged, null,
            "Anna Svensson", "+46701234567", "Stockholm", dateApplied, matchedCandidate.Id));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Confirm");

        await handler.Handle(command, CancellationToken.None);

        matchedCandidate.FullName.Should().Be("Anna Svensson");
        matchedCandidate.PhoneNumber.Should().Be("+46701234567");
        matchedCandidate.Location.Should().Be("Stockholm");
        matchedCandidate.DateApplied.Should().Be(dateApplied);
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

        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Reject");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Action.Should().Be("Rejected");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RejectMatch_CreatesNewCandidate()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var dateApplied = new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero);
        var session = ImportSession.Create(recruitment.Id, userId, "test.xlsx");
        session.AddRowResult(new ImportRowResult(
            1, "flagged@test.com", ImportRowAction.Flagged, null,
            "Anna Svensson", "+46701234567", "Stockholm", dateApplied, Guid.NewGuid()));
        session.MarkCompleted(0, 0, 0, 1);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var addedCandidates = new List<Candidate>();
        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);
        _dbContext.Candidates.When(x => x.Add(Arg.Any<Candidate>()))
            .Do(callInfo => addedCandidates.Add(callInfo.Arg<Candidate>()));

        var handler = new ResolveMatchConflictCommandHandler(_dbContext, _tenantContext);
        var command = new ResolveMatchConflictCommand(session.Id, 0, "Reject");

        await handler.Handle(command, CancellationToken.None);

        addedCandidates.Should().HaveCount(1);
        var newCandidate = addedCandidates[0];
        newCandidate.FullName.Should().Be("Anna Svensson");
        newCandidate.Email.Should().Be("flagged@test.com");
        newCandidate.PhoneNumber.Should().Be("+46701234567");
        newCandidate.Location.Should().Be("Stockholm");
        newCandidate.DateApplied.Should().Be(dateApplied);
        newCandidate.RecruitmentId.Should().Be(recruitment.Id);
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

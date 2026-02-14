using api.Application.Common.Interfaces;
using api.Application.Features.Import.Queries.GetImportSession;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Import.Queries.GetImportSession;

[TestFixture]
public class GetImportSessionQueryHandlerTests
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
    public async Task Handle_ExistingSession_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var session = ImportSession.Create(recruitment.Id, userId, "test.xlsx");
        var mockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(mockSet);

        var handler = new GetImportSessionQueryHandler(_dbContext, _tenantContext);
        var query = new GetImportSessionQuery(session.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(session.Id);
        result.SourceFileName.Should().Be("test.xlsx");
        result.Status.Should().Be("Processing");
    }

    [Test]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<ImportSession>().AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(mockSet);

        var handler = new GetImportSessionQueryHandler(_dbContext, _tenantContext);
        var query = new GetImportSessionQuery(Guid.NewGuid());

        var act = () => handler.Handle(query, CancellationToken.None);

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
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var handler = new GetImportSessionQueryHandler(_dbContext, _tenantContext);
        var query = new GetImportSessionQuery(session.Id);

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

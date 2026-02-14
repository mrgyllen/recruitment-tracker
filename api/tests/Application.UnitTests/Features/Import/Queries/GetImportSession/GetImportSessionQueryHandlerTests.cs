using api.Application.Common.Interfaces;
using api.Application.Features.Import.Queries.GetImportSession;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Import.Queries.GetImportSession;

[TestFixture]
public class GetImportSessionQueryHandlerTests
{
    private IApplicationDbContext _dbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
    }

    [Test]
    public async Task Handle_ExistingSession_ReturnsDto()
    {
        var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.xlsx");
        var mockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(mockSet);

        var handler = new GetImportSessionQueryHandler(_dbContext);
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
        var mockSet = new List<ImportSession>().AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(mockSet);

        var handler = new GetImportSessionQueryHandler(_dbContext);
        var query = new GetImportSessionQuery(Guid.NewGuid());

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

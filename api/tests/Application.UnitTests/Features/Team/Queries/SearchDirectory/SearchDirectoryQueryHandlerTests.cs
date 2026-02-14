using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Application.Features.Team.Queries.SearchDirectory;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Team.Queries.SearchDirectory;

[TestFixture]
public class SearchDirectoryQueryHandlerTests
{
    private IDirectoryService _directoryService = null!;
    private SearchDirectoryQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _directoryService = Substitute.For<IDirectoryService>();
        _handler = new SearchDirectoryQueryHandler(_directoryService);
    }

    [Test]
    public async Task Handle_ValidSearch_ReturnsMappedResults()
    {
        var users = new List<DirectoryUser>
        {
            new(Guid.NewGuid(), "Erik Leader", "erik@test.com"),
            new(Guid.NewGuid(), "Sara Specialist", "sara@test.com"),
        };
        _directoryService.SearchUsersAsync("eri", Arg.Any<CancellationToken>())
            .Returns(users);

        var result = await _handler.Handle(
            new SearchDirectoryQuery { SearchTerm = "eri" }, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("Erik Leader");
        result[0].Email.Should().Be("erik@test.com");
    }

    [Test]
    public async Task Handle_NoResults_ReturnsEmptyList()
    {
        _directoryService.SearchUsersAsync("xyz", Arg.Any<CancellationToken>())
            .Returns(new List<DirectoryUser>());

        var result = await _handler.Handle(
            new SearchDirectoryQuery { SearchTerm = "xyz" }, CancellationToken.None);

        result.Should().BeEmpty();
    }
}

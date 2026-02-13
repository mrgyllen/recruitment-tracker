using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Queries.GetRecruitments;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Queries;

[TestFixture]
public class GetRecruitmentsQueryTests
{
    private IApplicationDbContext _dbContext = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
    }

    [Test]
    public async Task Handle_WithRecruitments_ReturnsPaginatedList()
    {
        var r1 = Recruitment.Create("Role A", null, Guid.NewGuid());
        var r2 = Recruitment.Create("Role B", null, Guid.NewGuid());

        var mockSet = new List<Recruitment> { r1, r2 }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentsQueryHandler(_dbContext);
        var query = new GetRecruitmentsQuery { Page = 1, PageSize = 50 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Test]
    public async Task Handle_NoRecruitments_ReturnsEmptyList()
    {
        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentsQueryHandler(_dbContext);
        var query = new GetRecruitmentsQuery { Page = 1, PageSize = 50 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_Pagination_RespectsPageSize()
    {
        var recruitments = Enumerable.Range(1, 5)
            .Select(i => Recruitment.Create($"Role {i}", null, Guid.NewGuid()))
            .ToList();

        var mockSet = recruitments.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentsQueryHandler(_dbContext);
        var query = new GetRecruitmentsQuery { Page = 1, PageSize = 2 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
    }
}

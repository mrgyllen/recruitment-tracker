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
    private ITenantContext _tenantContext = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
    }

    [Test]
    public async Task Handle_ReturnsOnlyRecruitmentsWhereUserIsMember()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var myRecruitment = Recruitment.Create("My Role", null, userId);
        var otherRecruitment = Recruitment.Create("Other Role", null, otherUserId);

        var mockSet = new List<Recruitment> { myRecruitment, otherRecruitment }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentsQueryHandler(_dbContext, _tenantContext);
        var query = new GetRecruitmentsQuery { Page = 1, PageSize = 50 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("My Role");
        result.TotalCount.Should().Be(1);
    }

    [Test]
    public async Task Handle_NoMemberships_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var otherRecruitment = Recruitment.Create("Other Role", null, otherUserId);

        var mockSet = new List<Recruitment> { otherRecruitment }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentsQueryHandler(_dbContext, _tenantContext);
        var query = new GetRecruitmentsQuery { Page = 1, PageSize = 50 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_Pagination_RespectsPageSize()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitments = Enumerable.Range(1, 5)
            .Select(i => Recruitment.Create($"Role {i}", null, userId))
            .ToList();

        var mockSet = recruitments.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentsQueryHandler(_dbContext, _tenantContext);
        var query = new GetRecruitmentsQuery { Page = 1, PageSize = 2 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
    }
}

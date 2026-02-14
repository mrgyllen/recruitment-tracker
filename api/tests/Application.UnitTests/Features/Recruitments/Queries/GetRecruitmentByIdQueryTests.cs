using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Queries;

[TestFixture]
public class GetRecruitmentByIdQueryTests
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
    public async Task Handle_MemberAccessesRecruitment_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test Role", "A description", userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentByIdQueryHandler(_dbContext, _tenantContext);
        var query = new GetRecruitmentByIdQuery { Id = recruitment.Id };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Id.Should().Be(recruitment.Id);
        result.Title.Should().Be("Test Role");
        result.Description.Should().Be("A description");
        result.Steps.Should().HaveCount(2);
        result.Steps.First().Name.Should().Be("Screening");
    }

    [Test]
    public async Task Handle_NonExistingRecruitment_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentByIdQueryHandler(_dbContext, _tenantContext);
        var query = new GetRecruitmentByIdQuery { Id = Guid.NewGuid() };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test Role", null, creatorId);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentByIdQueryHandler(_dbContext, _tenantContext);
        var query = new GetRecruitmentByIdQuery { Id = recruitment.Id };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

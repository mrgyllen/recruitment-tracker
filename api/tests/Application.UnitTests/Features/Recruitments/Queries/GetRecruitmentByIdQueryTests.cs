using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Queries;

[TestFixture]
public class GetRecruitmentByIdQueryTests
{
    private IApplicationDbContext _dbContext = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
    }

    [Test]
    public async Task Handle_ExistingRecruitment_ReturnsDto()
    {
        var recruitment = Recruitment.Create("Test Role", "A description", Guid.NewGuid());
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);

        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentByIdQueryHandler(_dbContext);
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
        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new GetRecruitmentByIdQueryHandler(_dbContext);
        var query = new GetRecruitmentByIdQuery { Id = Guid.NewGuid() };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

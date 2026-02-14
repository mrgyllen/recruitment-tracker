using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.CloseRecruitment;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.CloseRecruitment;

[TestFixture]
public class CloseRecruitmentCommandHandlerTests
{
    private IApplicationDbContext _dbContext;
    private ITenantContext _tenantContext;

    [SetUp]
    public void Setup()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
    }

    [Test]
    public async Task Handle_ValidRequest_ClosesRecruitment()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(creatorId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        await handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        recruitment.Status.Should().Be(RecruitmentStatus.Closed);
        recruitment.ClosedAt.Should().NotBeNull();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_AlreadyClosed_ThrowsRecruitmentClosedException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);
        recruitment.Close();

        _tenantContext.UserGuid.Returns(creatorId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var recruitment = Recruitment.Create("Test", null, creatorId);

        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = recruitment.Id },
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());
        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new CloseRecruitmentCommandHandler(_dbContext, _tenantContext);
        var act = () => handler.Handle(
            new CloseRecruitmentCommand { RecruitmentId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

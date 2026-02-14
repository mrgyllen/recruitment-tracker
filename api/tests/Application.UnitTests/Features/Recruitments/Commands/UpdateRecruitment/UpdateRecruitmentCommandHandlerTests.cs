using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.UpdateRecruitment;
using api.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.UpdateRecruitment;

[TestFixture]
public class UpdateRecruitmentCommandHandlerTests
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
    public async Task Handle_ValidCommand_UpdatesRecruitment()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Old Title", "Old Desc", userId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new UpdateRecruitmentCommandHandler(_dbContext, _tenantContext);
        var command = new UpdateRecruitmentCommand
        {
            Id = recruitment.Id,
            Title = "New Title",
            Description = "New Desc",
            JobRequisitionId = "REQ-001",
        };

        await handler.Handle(command, CancellationToken.None);

        recruitment.Title.Should().Be("New Title");
        recruitment.Description.Should().Be("New Desc");
        recruitment.JobRequisitionId.Should().Be("REQ-001");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new UpdateRecruitmentCommandHandler(_dbContext, _tenantContext);
        var command = new UpdateRecruitmentCommand
        {
            Id = Guid.NewGuid(),
            Title = "New Title",
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Title", null, creatorId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new UpdateRecruitmentCommandHandler(_dbContext, _tenantContext);
        var command = new UpdateRecruitmentCommand
        {
            Id = recruitment.Id,
            Title = "New Title",
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

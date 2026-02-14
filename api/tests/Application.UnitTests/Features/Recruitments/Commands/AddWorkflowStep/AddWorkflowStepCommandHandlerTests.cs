using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Domain.Entities;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Recruitments.Commands.AddWorkflowStep;

[TestFixture]
public class AddWorkflowStepCommandHandlerTests
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
    public async Task Handle_ValidCommand_AddsStepAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            Name = "Screening",
            Order = 1,
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Screening");
        result.Order.Should().Be(1);
        result.Id.Should().NotBeEmpty();
        recruitment.Steps.Should().HaveCount(1);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var mockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = Guid.NewGuid(),
            Name = "Screening",
            Order = 1,
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_DuplicateStepName_ThrowsDuplicateStepNameException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            Name = "Screening",
            Order = 2,
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateStepNameException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        var mockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(mockSet);

        var handler = new AddWorkflowStepCommandHandler(_dbContext, _tenantContext);
        var command = new AddWorkflowStepCommand
        {
            RecruitmentId = recruitment.Id,
            Name = "Screening",
            Order = 1,
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}

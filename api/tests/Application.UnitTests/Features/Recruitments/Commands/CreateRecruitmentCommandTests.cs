using api.Application.Common.Interfaces;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Recruitments.Commands;

[TestFixture]
public class CreateRecruitmentCommandTests
{
    private IApplicationDbContext _dbContext = null!;
    private IUser _user = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();

        var recruitments = new List<Recruitment>();
        var mockSet = Substitute.For<DbSet<Recruitment>>();
        mockSet.When(x => x.Add(Arg.Any<Recruitment>()))
            .Do(ci => recruitments.Add(ci.Arg<Recruitment>()));
        _dbContext.Recruitments.Returns(mockSet);

        _user = Substitute.For<IUser>();
        _user.Id.Returns(Guid.NewGuid().ToString());
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesRecruitmentWithTitle()
    {
        var handler = new CreateRecruitmentCommandHandler(_dbContext, _user);
        var command = new CreateRecruitmentCommand
        {
            Title = "Senior .NET Developer",
            Description = "A recruitment for senior devs",
            Steps = [new WorkflowStepDto { Name = "Screening", Order = 1 }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _dbContext.Recruitments.Received(1).Add(Arg.Is<Recruitment>(r =>
            r.Title == "Senior .NET Developer"));
        await _dbContext.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Test]
    public async Task Handle_ValidCommand_AddsWorkflowSteps()
    {
        var handler = new CreateRecruitmentCommandHandler(_dbContext, _user);
        var command = new CreateRecruitmentCommand
        {
            Title = "Dev Role",
            Steps =
            [
                new WorkflowStepDto { Name = "Screening", Order = 1 },
                new WorkflowStepDto { Name = "Interview", Order = 2 }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        _dbContext.Recruitments.Received(1).Add(Arg.Is<Recruitment>(r =>
            r.Steps.Count == 2));
    }

    [Test]
    public async Task Handle_ValidCommand_SetsCreatorAsMember()
    {
        var userId = Guid.NewGuid().ToString();
        _user.Id.Returns(userId);
        var handler = new CreateRecruitmentCommandHandler(_dbContext, _user);
        var command = new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
            Steps = [new WorkflowStepDto { Name = "Screening", Order = 1 }]
        };

        await handler.Handle(command, CancellationToken.None);

        _dbContext.Recruitments.Received(1).Add(Arg.Is<Recruitment>(r =>
            r.Members.Count == 1 &&
            r.Members.First().UserId == Guid.Parse(userId)));
    }

    [Test]
    public async Task Handle_NoSteps_CreatesRecruitmentWithoutSteps()
    {
        var handler = new CreateRecruitmentCommandHandler(_dbContext, _user);
        var command = new CreateRecruitmentCommand
        {
            Title = "No Steps Role",
            Steps = []
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _dbContext.Recruitments.Received(1).Add(Arg.Is<Recruitment>(r =>
            r.Steps.Count == 0));
    }

    [Test]
    public async Task Handle_ReturnsNewRecruitmentId()
    {
        var handler = new CreateRecruitmentCommandHandler(_dbContext, _user);
        var command = new CreateRecruitmentCommand
        {
            Title = "Returns Id",
            Steps = []
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Test]
    public async Task Handle_WithJobRequisitionId_PersistsIt()
    {
        var handler = new CreateRecruitmentCommandHandler(_dbContext, _user);
        var command = new CreateRecruitmentCommand
        {
            Title = "Dev Role",
            JobRequisitionId = "REQ-2026-042",
            Steps = []
        };

        await handler.Handle(command, CancellationToken.None);

        _dbContext.Recruitments.Received(1).Add(Arg.Is<Recruitment>(r =>
            r.JobRequisitionId == "REQ-2026-042"));
    }
}

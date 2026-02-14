using System.Threading.Channels;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Application.Features.Import.Commands.StartImport;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Import.Commands.StartImport;

[TestFixture]
public class StartImportCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private ChannelWriter<ImportRequest> _channelWriter = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();

        var channel = Channel.CreateUnbounded<ImportRequest>();
        _channelWriter = channel.Writer;
    }

    [Test]
    public async Task Handle_ValidFile_CreatesSessionAndWritesToChannel()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var importSessionMockSet = new List<ImportSession>().AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(importSessionMockSet);

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(
            recruitment.Id,
            new byte[] { 1, 2, 3 },
            "workday.xlsx",
            3);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ImportSessionId.Should().NotBeEmpty();
        result.StatusUrl.Should().Contain("/api/import-sessions/");
        _dbContext.ImportSessions.Received(1).Add(Arg.Any<ImportSession>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitmentMockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(Guid.NewGuid(), new byte[] { 1 }, "test.xlsx", 1);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(recruitment.Id, new byte[] { 1 }, "test.xlsx", 1);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_ClosedRecruitment_ThrowsRecruitmentClosedException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.Close();
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new StartImportCommandHandler(_dbContext, _tenantContext, _channelWriter);
        var command = new StartImportCommand(recruitment.Id, new byte[] { 1 }, "test.xlsx", 1);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }
}

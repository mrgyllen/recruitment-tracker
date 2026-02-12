using api.Application.Common.Behaviours;
using api.Application.Common.Interfaces;
using api.Application.TodoItems.Commands.CreateTodoItem;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private ILogger<CreateTodoItemCommand> _logger = null!;
    private IUser _user = null!;
    private IIdentityService _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<CreateTodoItemCommand>>();
        _user = Substitute.For<IUser>();
        _identityService = Substitute.For<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Id.Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand>(_logger, _user, _identityService);

        await requestLogger.Process(new CreateTodoItemCommand { ListId = 1, Title = "title" }, new CancellationToken());

        await _identityService.Received(1).GetUserNameAsync(Arg.Any<string>());
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand>(_logger, _user, _identityService);

        await requestLogger.Process(new CreateTodoItemCommand { ListId = 1, Title = "title" }, new CancellationToken());

        await _identityService.DidNotReceive().GetUserNameAsync(Arg.Any<string>());
    }
}

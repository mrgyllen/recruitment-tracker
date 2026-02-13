using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

public class ImportSessionTests
{
    private ImportSession CreateSession()
    {
        return ImportSession.Create(Guid.NewGuid(), Guid.NewGuid());
    }

    [Test]
    public void Create_SetsProcessingStatus()
    {
        var session = CreateSession();

        session.Status.Should().Be(ImportSessionStatus.Processing);
    }

    [Test]
    public void MarkCompleted_SetsCompletedStatusAndCounts()
    {
        var session = CreateSession();

        session.MarkCompleted(8, 2);

        session.Status.Should().Be(ImportSessionStatus.Completed);
        session.TotalRows.Should().Be(10);
        session.SuccessfulRows.Should().Be(8);
        session.FailedRows.Should().Be(2);
        session.CompletedAt.Should().NotBeNull();
    }

    [Test]
    public void MarkFailed_SetsFailedStatusAndReason()
    {
        var session = CreateSession();

        session.MarkFailed("Invalid file format");

        session.Status.Should().Be(ImportSessionStatus.Failed);
        session.FailureReason.Should().Be("Invalid file format");
        session.CompletedAt.Should().NotBeNull();
    }

    [Test]
    public void MarkCompleted_WhenAlreadyCompleted_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkCompleted(10, 0);

        var act = () => session.MarkCompleted(5, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkCompleted_WhenFailed_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkFailed("error");

        var act = () => session.MarkCompleted(5, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkFailed_WhenAlreadyCompleted_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkCompleted(10, 0);

        var act = () => session.MarkFailed("error");

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkFailed_WhenAlreadyFailed_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkFailed("error 1");

        var act = () => session.MarkFailed("error 2");

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }
}

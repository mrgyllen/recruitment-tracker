using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using api.Domain.ValueObjects;
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
    public void Create_WithSourceFileName_RecordsSourceFileName()
    {
        var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "workday-export.xlsx");

        session.SourceFileName.Should().Be("workday-export.xlsx");
    }

    [Test]
    public void Create_WithoutSourceFileName_DefaultsToEmpty()
    {
        var session = CreateSession();

        session.SourceFileName.Should().BeEmpty();
    }

    [Test]
    public void MarkCompleted_UpdatesAllCounts()
    {
        var session = CreateSession();

        session.MarkCompleted(created: 5, updated: 3, errored: 1, flagged: 2);

        session.Status.Should().Be(ImportSessionStatus.Completed);
        session.CreatedCount.Should().Be(5);
        session.UpdatedCount.Should().Be(3);
        session.ErroredCount.Should().Be(1);
        session.FlaggedCount.Should().Be(2);
        session.TotalRows.Should().Be(11);
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
    public void MarkFailed_TruncatesLongReason()
    {
        var session = CreateSession();
        var longReason = new string('x', 3000);

        session.MarkFailed(longReason);

        session.FailureReason.Should().HaveLength(2000);
    }

    [Test]
    public void AddRowResult_WhenProcessing_AddsResult()
    {
        var session = CreateSession();
        var rowResult = new ImportRowResult(1, "alice@example.com", ImportRowAction.Created, null);

        session.AddRowResult(rowResult);

        session.RowResults.Should().HaveCount(1);
        session.RowResults.First().Should().Be(rowResult);
    }

    [Test]
    public void AddRowResult_WhenCompleted_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkCompleted(1, 0, 0, 0);

        var act = () => session.AddRowResult(
            new ImportRowResult(1, "a@b.com", ImportRowAction.Created, null));

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkCompleted_WhenAlreadyCompleted_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkCompleted(10, 0, 0, 0);

        var act = () => session.MarkCompleted(5, 0, 0, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkCompleted_WhenFailed_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkFailed("error");

        var act = () => session.MarkCompleted(5, 0, 0, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void MarkFailed_WhenAlreadyCompleted_ThrowsInvalidWorkflowTransitionException()
    {
        var session = CreateSession();
        session.MarkCompleted(10, 0, 0, 0);

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

    [Test]
    public void ConfirmMatch_FlaggedRow_SetsResolutionToConfirmed()
    {
        var session = CreateSession();
        session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);

        var result = session.ConfirmMatch(0);

        result.Resolution.Should().Be("Confirmed");
    }

    [Test]
    public void RejectMatch_FlaggedRow_SetsResolutionToRejected()
    {
        var session = CreateSession();
        session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);

        var result = session.RejectMatch(0);

        result.Resolution.Should().Be("Rejected");
    }

    [Test]
    public void ConfirmMatch_NonFlaggedRow_Throws()
    {
        var session = CreateSession();
        session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Created, null));
        session.MarkCompleted(1, 0, 0, 0);

        var act = () => session.ConfirmMatch(0);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ConfirmMatch_AlreadyResolved_Throws()
    {
        var session = CreateSession();
        session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));
        session.MarkCompleted(0, 0, 0, 1);
        session.ConfirmMatch(0);

        var act = () => session.ConfirmMatch(0);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ConfirmMatch_ProcessingSession_Throws()
    {
        var session = CreateSession();
        session.AddRowResult(new ImportRowResult(1, "a@test.com", ImportRowAction.Flagged, null));

        var act = () => session.ConfirmMatch(0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void ConfirmMatch_InvalidIndex_Throws()
    {
        var session = CreateSession();
        session.MarkCompleted(0, 0, 0, 0);

        var act = () => session.ConfirmMatch(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

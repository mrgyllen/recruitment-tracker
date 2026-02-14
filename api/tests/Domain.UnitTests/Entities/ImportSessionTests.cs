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

    // === PDF splitting progress tests ===

    [Test]
    public void SetPdfSplitProgress_WhenProcessing_UpdatesFields()
    {
        var session = CreateSession();

        session.SetPdfSplitProgress(10, 5, 1);

        session.PdfTotalCandidates.Should().Be(10);
        session.PdfSplitCandidates.Should().Be(5);
        session.PdfSplitErrors.Should().Be(1);
    }

    [Test]
    public void SetPdfSplitProgress_WhenNotProcessing_Throws()
    {
        var session = CreateSession();
        session.MarkCompleted(1, 0, 0, 0);

        var act = () => session.SetPdfSplitProgress(10, 5, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void SetOriginalBundleUrl_StoresUrl()
    {
        var session = CreateSession();

        session.SetOriginalBundleUrl("recruitments/abc/bundles/original.pdf");

        session.OriginalBundleBlobUrl.Should().Be("recruitments/abc/bundles/original.pdf");
    }

    [Test]
    public void AddImportDocument_WhenProcessing_CreatesChildEntity()
    {
        var session = CreateSession();

        session.AddImportDocument("Anna Svensson", "https://blob/cv.pdf", "WD12345");

        session.ImportDocuments.Should().HaveCount(1);
        var doc = session.ImportDocuments.First();
        doc.CandidateName.Should().Be("Anna Svensson");
        doc.BlobStorageUrl.Should().Be("https://blob/cv.pdf");
        doc.WorkdayCandidateId.Should().Be("WD12345");
        doc.ImportSessionId.Should().Be(session.Id);
    }

    [Test]
    public void AddImportDocument_WhenNotProcessing_Throws()
    {
        var session = CreateSession();
        session.MarkCompleted(1, 0, 0, 0);

        var act = () => session.AddImportDocument("Name", "url", null);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void ClearImportDocuments_RemovesAllDocuments()
    {
        var session = CreateSession();
        session.AddImportDocument("Anna", "url1", "WD1");
        session.AddImportDocument("Bob", "url2", "WD2");

        session.ClearImportDocuments();

        session.ImportDocuments.Should().BeEmpty();
    }

    [Test]
    public void UpdateImportDocumentMatch_AutoMatched_UpdatesDocStatus()
    {
        var session = CreateSession();
        session.AddImportDocument("Alice", "blob://cv.pdf", null);
        var doc = session.ImportDocuments.First();
        var candidateId = Guid.NewGuid();

        session.UpdateImportDocumentMatch(doc.Id, candidateId, ImportDocumentMatchStatus.AutoMatched);

        doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.AutoMatched);
        doc.MatchedCandidateId.Should().Be(candidateId);
    }

    [Test]
    public void UpdateImportDocumentMatch_Unmatched_SetsStatusCorrectly()
    {
        var session = CreateSession();
        session.AddImportDocument("Unknown", "blob://cv.pdf", null);
        var doc = session.ImportDocuments.First();

        session.UpdateImportDocumentMatch(doc.Id, null, ImportDocumentMatchStatus.Unmatched);

        doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.Unmatched);
        doc.MatchedCandidateId.Should().BeNull();
    }

    [Test]
    public void UpdateImportDocumentMatch_InvalidDocumentId_Throws()
    {
        var session = CreateSession();

        var act = () => session.UpdateImportDocumentMatch(Guid.NewGuid(), null, ImportDocumentMatchStatus.Unmatched);

        act.Should().Throw<ArgumentException>();
    }
}

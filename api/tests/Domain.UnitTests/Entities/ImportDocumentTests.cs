using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

[TestFixture]
public class ImportDocumentTests
{
    [Test]
    public void Create_SetsAllProperties()
    {
        var importSessionId = Guid.NewGuid();

        var doc = ImportDocument.Create(
            importSessionId, "Anna Svensson",
            "https://blob/cv.pdf", "WD12345");

        doc.ImportSessionId.Should().Be(importSessionId);
        doc.CandidateName.Should().Be("Anna Svensson");
        doc.BlobStorageUrl.Should().Be("https://blob/cv.pdf");
        doc.WorkdayCandidateId.Should().Be("WD12345");
        doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.Pending);
        doc.MatchedCandidateId.Should().BeNull();
    }

    [Test]
    public void Create_WithoutWorkdayId_SetsNull()
    {
        var doc = ImportDocument.Create(
            Guid.NewGuid(), "Bob Smith",
            "https://blob/cv.pdf", null);

        doc.WorkdayCandidateId.Should().BeNull();
    }

    [Test]
    public void MarkAutoMatched_SetsStatusAndCandidateId()
    {
        var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");
        session.AddImportDocument("Alice Johnson", "blob://cv.pdf", null);
        var doc = session.ImportDocuments.First();
        var candidateId = Guid.NewGuid();

        doc.MarkAutoMatched(candidateId);

        doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.AutoMatched);
        doc.MatchedCandidateId.Should().Be(candidateId);
    }

    [Test]
    public void MarkUnmatched_SetsStatusToUnmatched()
    {
        var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");
        session.AddImportDocument("Unknown", "blob://cv.pdf", null);
        var doc = session.ImportDocuments.First();

        doc.MarkUnmatched();

        doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.Unmatched);
        doc.MatchedCandidateId.Should().BeNull();
    }

    [Test]
    public void MarkManuallyAssigned_SetsStatusAndCandidateId()
    {
        var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");
        session.AddImportDocument("Person", "blob://cv.pdf", null);
        var doc = session.ImportDocuments.First();
        var candidateId = Guid.NewGuid();

        doc.MarkManuallyAssigned(candidateId);

        doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.ManuallyAssigned);
        doc.MatchedCandidateId.Should().Be(candidateId);
    }
}

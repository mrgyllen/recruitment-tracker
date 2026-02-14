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
}

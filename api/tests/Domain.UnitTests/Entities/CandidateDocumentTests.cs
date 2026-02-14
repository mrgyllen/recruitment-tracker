using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

[TestFixture]
public class CandidateDocumentTests
{
    [Test]
    public void Create_WithWorkdayId_SetsProperties()
    {
        var candidateId = Guid.NewGuid();

        var doc = CandidateDocument.Create(
            candidateId, "CV", "https://blob/cv.pdf",
            "WD12345", DocumentSource.BundleSplit);

        doc.CandidateId.Should().Be(candidateId);
        doc.DocumentType.Should().Be("CV");
        doc.BlobStorageUrl.Should().Be("https://blob/cv.pdf");
        doc.WorkdayCandidateId.Should().Be("WD12345");
        doc.DocumentSource.Should().Be(DocumentSource.BundleSplit);
    }

    [Test]
    public void Create_WithoutWorkdayId_DefaultsToNull()
    {
        var doc = CandidateDocument.Create(
            Guid.NewGuid(), "CV", "https://blob/cv.pdf");

        doc.WorkdayCandidateId.Should().BeNull();
        doc.DocumentSource.Should().Be(DocumentSource.IndividualUpload);
    }
}

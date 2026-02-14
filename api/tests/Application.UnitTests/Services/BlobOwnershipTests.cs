using api.Infrastructure.Services;
using Azure.Storage.Blobs;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Services;

[TestFixture]
public class BlobOwnershipTests
{
    private BlobStorageService _sut = null!;
    private readonly Guid _recruitmentId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    [SetUp]
    public void SetUp()
    {
        var blobServiceClient = Substitute.For<BlobServiceClient>();
        _sut = new BlobStorageService(blobServiceClient);
    }

    [Test]
    public void VerifyBlobOwnership_WithValidPath_ReturnsTrue()
    {
        var blobUrl = $"{_recruitmentId}/candidates/doc-123.pdf";

        var result = _sut.VerifyBlobOwnership("documents", blobUrl, _recruitmentId);

        result.Should().BeTrue();
    }

    [Test]
    public void VerifyBlobOwnership_WithWrongRecruitmentId_ReturnsFalse()
    {
        var wrongRecruitmentId = Guid.NewGuid();
        var blobUrl = $"{wrongRecruitmentId}/candidates/doc-123.pdf";

        var result = _sut.VerifyBlobOwnership("documents", blobUrl, _recruitmentId);

        result.Should().BeFalse();
    }

    [Test]
    public void VerifyBlobOwnership_WithPathTraversal_ReturnsFalse()
    {
        var otherRecruitmentId = Guid.NewGuid();
        var blobUrl = $"{_recruitmentId}/../{otherRecruitmentId}/candidates/doc-123.pdf";

        var result = _sut.VerifyBlobOwnership("documents", blobUrl, _recruitmentId);

        result.Should().BeFalse();
    }

    [Test]
    public void VerifyBlobOwnership_WithNestedPathTraversal_ReturnsFalse()
    {
        var blobUrl = $"../../etc/passwd";

        var result = _sut.VerifyBlobOwnership("documents", blobUrl, _recruitmentId);

        result.Should().BeFalse();
    }

    [Test]
    public void VerifyBlobOwnership_WithEmptyPath_ReturnsFalse()
    {
        var result = _sut.VerifyBlobOwnership("documents", "", _recruitmentId);

        result.Should().BeFalse();
    }

    [Test]
    public void VerifyBlobOwnership_CaseInsensitive_ReturnsTrue()
    {
        var blobUrl = $"{_recruitmentId.ToString().ToUpperInvariant()}/candidates/doc.pdf";

        var result = _sut.VerifyBlobOwnership("documents", blobUrl, _recruitmentId);

        result.Should().BeTrue();
    }
}

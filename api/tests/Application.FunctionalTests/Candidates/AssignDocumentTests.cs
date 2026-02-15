using api.Application.Features.Candidates.Commands.AssignDocument;
using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Candidates;

using static Testing;

public class AssignDocumentTests : BaseTestFixture
{
    [Test]
    public async Task Handle_ValidDocument_AssignsDocumentToCandidate()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice Johnson",
            Email = "alice@example.com",
        });

        var blobUrl = $"{recruitmentId}/cvs/test-doc.pdf";
        var result = await SendAsync(new AssignDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            DocumentBlobUrl: blobUrl,
            DocumentName: "test-doc.pdf",
            ImportSessionId: null
        ));

        result.Should().NotBeNull();
        result.CandidateId.Should().Be(candidateId);
        result.DocumentType.Should().Be("CV");
        result.BlobStorageUrl.Should().Be(blobUrl);
    }

    [Test]
    public async Task Handle_NonExistentCandidate_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var blobUrl = $"{recruitmentId}/cvs/test-doc.pdf";
        var act = () => SendAsync(new AssignDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: Guid.NewGuid(),
            DocumentBlobUrl: blobUrl,
            DocumentName: "test-doc.pdf",
            ImportSessionId: null
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice",
            Email = "alice@example.com",
        });

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var blobUrl = $"{recruitmentId}/cvs/test-doc.pdf";
        var act = () => SendAsync(new AssignDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            DocumentBlobUrl: blobUrl,
            DocumentName: "test-doc.pdf",
            ImportSessionId: null
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_ReplacesExistingDocument_WhenCandidateAlreadyHasCV()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice Johnson",
            Email = "alice@example.com",
        });

        var firstBlobUrl = $"{recruitmentId}/cvs/first-doc.pdf";
        await SendAsync(new AssignDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            DocumentBlobUrl: firstBlobUrl,
            DocumentName: "first-doc.pdf",
            ImportSessionId: null
        ));

        var secondBlobUrl = $"{recruitmentId}/cvs/second-doc.pdf";
        var result = await SendAsync(new AssignDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            DocumentBlobUrl: secondBlobUrl,
            DocumentName: "second-doc.pdf",
            ImportSessionId: null
        ));

        result.BlobStorageUrl.Should().Be(secondBlobUrl);
        result.DocumentType.Should().Be("CV");
    }
}

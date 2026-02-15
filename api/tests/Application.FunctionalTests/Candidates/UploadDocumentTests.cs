using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Candidates.Commands.UploadDocument;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Candidates;

using static Testing;

public class UploadDocumentTests : BaseTestFixture
{
    [Test]
    public async Task Handle_ValidUpload_UploadsDocumentAndReturnDto()
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

        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF magic bytes
        var result = await SendAsync(new UploadDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            FileStream: stream,
            FileName: "resume.pdf",
            FileSize: stream.Length
        ));

        result.Should().NotBeNull();
        result.CandidateId.Should().Be(candidateId);
        result.DocumentType.Should().Be("CV");
        result.BlobStorageUrl.Should().Contain(recruitmentId.ToString());
    }

    [Test]
    public async Task Handle_NonExistentCandidate_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var act = () => SendAsync(new UploadDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: Guid.NewGuid(),
            FileStream: stream,
            FileName: "resume.pdf",
            FileSize: stream.Length
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

        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var act = () => SendAsync(new UploadDocumentCommand(
            RecruitmentId: recruitmentId,
            CandidateId: candidateId,
            FileStream: stream,
            FileName: "resume.pdf",
            FileSize: stream.Length
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonPdfFile_ThrowsValidationException()
    {
        await RunAsDefaultUserAsync();

        using var stream = new MemoryStream(new byte[] { 0x00 });
        var act = () => SendAsync(new UploadDocumentCommand(
            RecruitmentId: Guid.NewGuid(),
            CandidateId: Guid.NewGuid(),
            FileStream: stream,
            FileName: "resume.docx",
            FileSize: stream.Length
        ));

        await act.Should().ThrowAsync<ValidationException>();
    }
}

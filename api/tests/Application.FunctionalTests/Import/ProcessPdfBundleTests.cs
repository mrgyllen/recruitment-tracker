using api.Application.Features.Import.Commands.ProcessPdfBundle;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Domain.Entities;
using api.Domain.Enums;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Import;

using static Testing;

public class ProcessPdfBundleTests : BaseTestFixture
{
    [Test]
    public async Task Handle_ValidSession_ProcessesBundleAndMarksCompleted()
    {
        var userId = await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var session = ImportSession.Create(recruitmentId, Guid.Parse(userId), "bundle.pdf");
        await AddAsync(session);

        using var pdfStream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        await SendAsync(new ProcessPdfBundleCommand(
            ImportSessionId: session.Id,
            RecruitmentId: recruitmentId,
            PdfStream: pdfStream
        ));

        var updated = await FindAsync<ImportSession>(session.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ImportSessionStatus.Completed);
        updated.OriginalBundleBlobUrl.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Handle_NonExistentSession_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        using var pdfStream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var act = () => SendAsync(new ProcessPdfBundleCommand(
            ImportSessionId: Guid.NewGuid(),
            RecruitmentId: Guid.NewGuid(),
            PdfStream: pdfStream
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

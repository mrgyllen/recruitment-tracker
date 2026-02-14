using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Entities;
using Microsoft.Extensions.Logging;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Import.Commands.ProcessPdfBundle;

public class ProcessPdfBundleCommandHandler(
    IApplicationDbContext dbContext,
    IPdfSplitter pdfSplitter,
    IBlobStorageService blobStorage,
    ILogger<ProcessPdfBundleCommandHandler> logger)
    : IRequestHandler<ProcessPdfBundleCommand>
{
    private const string ContainerName = "documents";

    public async Task Handle(ProcessPdfBundleCommand request, CancellationToken cancellationToken)
    {
        var session = await dbContext.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ImportSession), request.ImportSessionId);

        // 1. Upload original bundle as fallback (AC5)
        var bundleBlobName = $"{request.RecruitmentId}/bundles/{session.Id}_original.pdf";
        request.PdfStream.Position = 0;
        await blobStorage.UploadAsync(ContainerName, bundleBlobName,
            request.PdfStream, "application/pdf", cancellationToken);
        session.SetOriginalBundleUrl(bundleBlobName);

        // 2. Split the PDF (AC2, AC3)
        request.PdfStream.Position = 0;
        var progressReporter = new Progress<PdfSplitProgress>(p =>
        {
            session.SetPdfSplitProgress(p.TotalCandidates, p.CompletedCandidates, 0);
        });

        var result = await pdfSplitter.SplitBundleAsync(request.PdfStream, progressReporter, cancellationToken);

        if (!result.Success)
        {
            session.MarkFailed($"PDF splitting failed: {result.ErrorMessage}");
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // 3. Upload splits and create ImportDocument tracking records (AC4, AC6)
        int successCount = 0, errorCount = 0;
        foreach (var entry in result.Entries)
        {
            if (entry.PdfBytes is null)
            {
                errorCount++;
                logger.LogWarning("Failed to split PDF for candidate {CandidateName}: {Error}",
                    entry.CandidateName, entry.ErrorMessage);
                continue;
            }

            try
            {
                var docId = Guid.NewGuid();
                var blobName = $"{request.RecruitmentId}/cvs/{docId}.pdf";
                using var stream = new MemoryStream(entry.PdfBytes);
                var blobUrl = await blobStorage.UploadAsync(ContainerName, blobName,
                    stream, "application/pdf", cancellationToken);

                session.AddImportDocument(
                    entry.CandidateName, blobUrl, entry.WorkdayCandidateId);
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                logger.LogWarning(ex, "Failed to upload split PDF for {CandidateName}",
                    entry.CandidateName);
            }
        }

        session.SetPdfSplitProgress(result.Entries.Count, successCount, errorCount);
        session.MarkCompleted(successCount, 0, errorCount, 0);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

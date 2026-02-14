using api.Application.Common.Models;

namespace api.Application.Common.Interfaces;

public interface IPdfSplitter
{
    Task<PdfSplitResult> SplitBundleAsync(
        Stream pdfStream,
        IProgress<PdfSplitProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

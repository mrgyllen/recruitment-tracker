using System.Text.RegularExpressions;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Writer;

namespace api.Infrastructure.Services;

public partial class PdfSplitterService : IPdfSplitter
{
    public Task<PdfSplitResult> SplitBundleAsync(
        Stream pdfStream,
        IProgress<PdfSplitProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(pdfStream);

        if (!document.TryGetBookmarks(out var bookmarks) ||
            bookmarks.Roots.Count == 0)
        {
            return Task.FromResult(new PdfSplitResult(
                false, [],
                "PDF bundle has no table of contents (bookmarks). Cannot determine candidate boundaries."));
        }

        var tocEntries = ParseTocEntries(bookmarks, document.NumberOfPages);

        if (tocEntries.Count == 0)
        {
            return Task.FromResult(new PdfSplitResult(
                false, [],
                "PDF bookmarks found but none have valid page destinations."));
        }

        var entries = new List<PdfSplitEntry>();

        for (int i = 0; i < tocEntries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var toc = tocEntries[i];

            try
            {
                var builder = new PdfDocumentBuilder();
                for (int page = toc.StartPage; page <= toc.EndPage; page++)
                {
                    builder.AddPage(document, page);
                }
                var pdfBytes = builder.Build();
                entries.Add(new PdfSplitEntry(
                    toc.CandidateName, toc.WorkdayCandidateId,
                    toc.StartPage, toc.EndPage, pdfBytes, null));
            }
            catch (Exception ex)
            {
                entries.Add(new PdfSplitEntry(
                    toc.CandidateName, toc.WorkdayCandidateId,
                    toc.StartPage, toc.EndPage, null, ex.Message));
            }

            progress?.Report(new PdfSplitProgress(
                tocEntries.Count, i + 1, toc.CandidateName));
        }

        return Task.FromResult(new PdfSplitResult(true, entries, null));
    }

    private static List<TocEntry> ParseTocEntries(
        Bookmarks bookmarks, int totalPages)
    {
        var entries = new List<TocEntry>();

        // Filter to DocumentBookmarkNode which has PageNumber;
        // other node types (UriBookmarkNode, etc.) are skipped.
        var roots = bookmarks.GetNodes()
            .OfType<DocumentBookmarkNode>()
            .OrderBy(b => b.PageNumber)
            .ToList();

        for (int i = 0; i < roots.Count; i++)
        {
            var name = roots[i].Title;
            var startPage = roots[i].PageNumber;
            var endPage = i + 1 < roots.Count
                ? roots[i + 1].PageNumber - 1
                : totalPages;

            var (candidateName, workdayId) = ParseCandidateInfo(name);
            entries.Add(new TocEntry(candidateName, workdayId, startPage, endPage));
        }

        return entries;
    }

    private static (string Name, string? WorkdayId) ParseCandidateInfo(string title)
    {
        var match = CandidateInfoRegex().Match(title);
        if (match.Success)
            return (match.Groups[1].Value.Trim(), match.Groups[2].Value);
        return (title.Trim(), null);
    }

    [GeneratedRegex(@"^(.+?)\s*\((\w+)\)\s*$")]
    private static partial Regex CandidateInfoRegex();

    private record TocEntry(
        string CandidateName, string? WorkdayCandidateId,
        int StartPage, int EndPage);
}

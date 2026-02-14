using api.Application.Common.Models;
using api.Infrastructure.Services;
using FluentAssertions;
using NUnit.Framework;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Outline.Destinations;
using UglyToad.PdfPig.Writer;

namespace api.Application.UnitTests.Features.Import.Services;

[TestFixture]
public class PdfSplitterServiceTests
{
    private PdfSplitterService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new PdfSplitterService();
    }

    private static Stream CreatePdfWithBookmarks(params (string title, int pageNumber)[] bookmarks)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        // Create 6 pages with minimal content
        for (int i = 0; i < 6; i++)
        {
            var pageBuilder = builder.AddPage(PageSize.A4);
            pageBuilder.AddText($"Page {i + 1}", 12, new PdfPoint(72, 720), font);
        }

        // Build bookmark tree using PdfPig's Bookmarks model
        var bookmarkNodes = new List<BookmarkNode>();
        foreach (var (title, pageNumber) in bookmarks)
        {
            var destination = new ExplicitDestination(
                pageNumber,
                ExplicitDestinationType.FitPage,
                ExplicitDestinationCoordinates.Empty);
            bookmarkNodes.Add(new DocumentBookmarkNode(
                title, 0, destination, Array.Empty<BookmarkNode>()));
        }

        builder.Bookmarks = new Bookmarks(bookmarkNodes);

        var bytes = builder.Build();
        return new MemoryStream(bytes);
    }

    private static Stream CreatePdfWithoutBookmarks()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var pageBuilder = builder.AddPage(PageSize.A4);
        pageBuilder.AddText("No bookmarks", 12, new PdfPoint(72, 720), font);
        var bytes = builder.Build();
        return new MemoryStream(bytes);
    }

    [Test]
    public async Task SplitBundleAsync_ValidBundle_ReturnsSplitEntries()
    {
        using var pdf = CreatePdfWithBookmarks(
            ("Svensson, Anna (WD001)", 1),
            ("Johansson, Erik (WD002)", 3),
            ("Lindberg, Sara (WD003)", 5));

        var result = await _service.SplitBundleAsync(pdf);

        result.Success.Should().BeTrue();
        result.Entries.Should().HaveCount(3);
        result.Entries[0].CandidateName.Should().Be("Svensson, Anna");
        result.Entries[0].WorkdayCandidateId.Should().Be("WD001");
        result.Entries[0].StartPage.Should().Be(1);
        result.Entries[0].EndPage.Should().Be(2);
        result.Entries[0].PdfBytes.Should().NotBeNull();
        result.Entries[1].CandidateName.Should().Be("Johansson, Erik");
        result.Entries[1].WorkdayCandidateId.Should().Be("WD002");
        result.Entries[1].StartPage.Should().Be(3);
        result.Entries[1].EndPage.Should().Be(4);
        result.Entries[2].CandidateName.Should().Be("Lindberg, Sara");
        result.Entries[2].WorkdayCandidateId.Should().Be("WD003");
        result.Entries[2].StartPage.Should().Be(5);
        result.Entries[2].EndPage.Should().Be(6);
    }

    [Test]
    public async Task SplitBundleAsync_NoBookmarks_ReturnsFailure()
    {
        using var pdf = CreatePdfWithoutBookmarks();

        var result = await _service.SplitBundleAsync(pdf);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no table of contents");
    }

    [Test]
    public async Task SplitBundleAsync_BookmarkWithoutWorkdayId_SetsNameOnly()
    {
        using var pdf = CreatePdfWithBookmarks(
            ("Plain Name", 1),
            ("Another Name (WD999)", 4));

        var result = await _service.SplitBundleAsync(pdf);

        result.Success.Should().BeTrue();
        result.Entries[0].CandidateName.Should().Be("Plain Name");
        result.Entries[0].WorkdayCandidateId.Should().BeNull();
        result.Entries[1].WorkdayCandidateId.Should().Be("WD999");
    }

    [Test]
    public async Task SplitBundleAsync_ReportsProgress()
    {
        using var pdf = CreatePdfWithBookmarks(
            ("Alice (WD1)", 1),
            ("Bob (WD2)", 4));

        var progressReports = new List<PdfSplitProgress>();
        var progress = new Progress<PdfSplitProgress>(p => progressReports.Add(p));

        await _service.SplitBundleAsync(pdf, progress);

        // Progress<T> reports asynchronously; give it a moment
        await Task.Delay(100);

        progressReports.Should().HaveCount(2);
        progressReports[0].TotalCandidates.Should().Be(2);
        progressReports[0].CompletedCandidates.Should().Be(1);
        progressReports[1].CompletedCandidates.Should().Be(2);
    }

    [Test]
    public async Task SplitBundleAsync_SplitPdfsAreReadable()
    {
        using var pdf = CreatePdfWithBookmarks(
            ("Candidate A (WD001)", 1),
            ("Candidate B (WD002)", 4));

        var result = await _service.SplitBundleAsync(pdf);

        result.Success.Should().BeTrue();

        // Verify the first split PDF is a valid PDF with 3 pages (pages 1-3)
        result.Entries[0].PdfBytes.Should().NotBeNull();
        using var splitDoc = UglyToad.PdfPig.PdfDocument.Open(result.Entries[0].PdfBytes!);
        splitDoc.NumberOfPages.Should().Be(3);

        // Verify the second split PDF has 3 pages (pages 4-6)
        result.Entries[1].PdfBytes.Should().NotBeNull();
        using var splitDoc2 = UglyToad.PdfPig.PdfDocument.Open(result.Entries[1].PdfBytes!);
        splitDoc2.NumberOfPages.Should().Be(3);
    }
}

using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Application.Features.Import.Commands.ProcessPdfBundle;
using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;

namespace api.Application.UnitTests.Features.Import.Commands.ProcessPdfBundle;

[TestFixture]
public class ProcessPdfBundleCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private IPdfSplitter _pdfSplitter = null!;
    private IBlobStorageService _blobStorage = null!;
    private ILogger<ProcessPdfBundleCommandHandler> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _pdfSplitter = Substitute.For<IPdfSplitter>();
        _blobStorage = Substitute.For<IBlobStorageService>();
        _logger = Substitute.For<ILogger<ProcessPdfBundleCommandHandler>>();
    }

    private static ImportSession CreateProcessingSession(Guid recruitmentId)
    {
        return ImportSession.Create(recruitmentId, Guid.NewGuid());
    }

    [Test]
    public async Task Handle_ValidBundle_SplitsAndUploadsAll()
    {
        var recruitmentId = Guid.NewGuid();
        var session = CreateProcessingSession(recruitmentId);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var entries = new List<PdfSplitEntry>
        {
            new("Anna", "WD001", 1, 2, new byte[] { 1, 2, 3 }, null),
            new("Bob", "WD002", 3, 4, new byte[] { 4, 5, 6 }, null),
            new("Sara", "WD003", 5, 6, new byte[] { 7, 8, 9 }, null),
        };
        _pdfSplitter.SplitBundleAsync(Arg.Any<Stream>(), Arg.Any<IProgress<PdfSplitProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PdfSplitResult(true, entries, null));

        _blobStorage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://blob/url");

        using var pdfStream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var command = new ProcessPdfBundleCommand(session.Id, recruitmentId, pdfStream);
        var handler = new ProcessPdfBundleCommandHandler(_dbContext, _pdfSplitter, _blobStorage, _logger);

        await handler.Handle(command, CancellationToken.None);

        session.ImportDocuments.Should().HaveCount(3);
        session.Status.Should().Be(ImportSessionStatus.Completed);
        session.PdfSplitCandidates.Should().Be(3);
        session.PdfSplitErrors.Should().Be(0);
        // 1 original bundle + 3 splits = 4 uploads
        await _blobStorage.Received(4).UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_PartialFailure_StoresSuccessfulSplits()
    {
        var recruitmentId = Guid.NewGuid();
        var session = CreateProcessingSession(recruitmentId);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        var entries = new List<PdfSplitEntry>
        {
            new("Anna", "WD001", 1, 2, new byte[] { 1 }, null),
            new("Bob", "WD002", 3, 4, null, "Corrupt page range"),
            new("Sara", "WD003", 5, 6, new byte[] { 2 }, null),
        };
        _pdfSplitter.SplitBundleAsync(Arg.Any<Stream>(), Arg.Any<IProgress<PdfSplitProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PdfSplitResult(true, entries, null));

        _blobStorage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://blob/url");

        using var pdfStream = new MemoryStream(new byte[] { 1 });
        var command = new ProcessPdfBundleCommand(session.Id, recruitmentId, pdfStream);
        var handler = new ProcessPdfBundleCommandHandler(_dbContext, _pdfSplitter, _blobStorage, _logger);

        await handler.Handle(command, CancellationToken.None);

        session.ImportDocuments.Should().HaveCount(2);
        session.Status.Should().Be(ImportSessionStatus.Completed);
        session.PdfSplitCandidates.Should().Be(2);
        session.PdfSplitErrors.Should().Be(1);
    }

    [Test]
    public async Task Handle_NoToc_MarksSessionFailed()
    {
        var recruitmentId = Guid.NewGuid();
        var session = CreateProcessingSession(recruitmentId);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        _pdfSplitter.SplitBundleAsync(Arg.Any<Stream>(), Arg.Any<IProgress<PdfSplitProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PdfSplitResult(false, [], "No TOC found"));

        _blobStorage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://blob/url");

        using var pdfStream = new MemoryStream(new byte[] { 1 });
        var command = new ProcessPdfBundleCommand(session.Id, recruitmentId, pdfStream);
        var handler = new ProcessPdfBundleCommandHandler(_dbContext, _pdfSplitter, _blobStorage, _logger);

        await handler.Handle(command, CancellationToken.None);

        session.Status.Should().Be(ImportSessionStatus.Failed);
        session.FailureReason.Should().Contain("No TOC found");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_OriginalBundleStoredBeforeSplitting()
    {
        var recruitmentId = Guid.NewGuid();
        var session = CreateProcessingSession(recruitmentId);
        var sessionMockSet = new List<ImportSession> { session }.AsQueryable().BuildMockDbSet();
        _dbContext.ImportSessions.Returns(sessionMockSet);

        _pdfSplitter.SplitBundleAsync(Arg.Any<Stream>(), Arg.Any<IProgress<PdfSplitProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new PdfSplitResult(true, [], null));

        _blobStorage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://blob/original");

        using var pdfStream = new MemoryStream(new byte[] { 1 });
        var command = new ProcessPdfBundleCommand(session.Id, recruitmentId, pdfStream);
        var handler = new ProcessPdfBundleCommandHandler(_dbContext, _pdfSplitter, _blobStorage, _logger);

        await handler.Handle(command, CancellationToken.None);

        session.OriginalBundleBlobUrl.Should().NotBeNull();
        // Verify the original bundle blob name follows convention
        await _blobStorage.Received().UploadAsync(
            "documents",
            Arg.Is<string>(s => s.Contains("bundles") && s.Contains("_original.pdf")),
            Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>());
    }
}

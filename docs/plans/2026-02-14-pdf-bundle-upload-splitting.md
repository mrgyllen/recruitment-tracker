# PDF Bundle Upload & Splitting Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable upload of Workday CV bundle PDFs with automatic splitting into individual per-candidate documents stored in Azure Blob Storage, tracked as ImportDocument records on ImportSession.

**Architecture:** Extend ImportSession aggregate with ImportDocument child entity and PDF progress fields. Implement PdfSplitterService (UglyToad.PdfPig) for TOC parsing and page extraction. Implement BlobStorageService (Azure.Storage.Blobs) for document storage. Add ProcessPdfBundleCommand to orchestrate the split-upload-track pipeline. Wire into existing ImportPipelineHostedService channel consumer.

**Tech Stack:** .NET 10, EF Core 10, MediatR 13, UglyToad.PdfPig, Azure.Storage.Blobs, NUnit, NSubstitute, FluentAssertions

---

## Task 1: Domain -- Add DocumentSource enum and ImportDocumentMatchStatus enum

**Mode:** N/A (enum definitions, no behavior)

**Files:**
- Create: `api/src/Domain/Enums/DocumentSource.cs`
- Create: `api/src/Domain/Enums/ImportDocumentMatchStatus.cs`

**Step 1: Create DocumentSource enum**

```csharp
// api/src/Domain/Enums/DocumentSource.cs
namespace api.Domain.Enums;

public enum DocumentSource
{
    BundleSplit,
    IndividualUpload,
}
```

**Step 2: Create ImportDocumentMatchStatus enum**

```csharp
// api/src/Domain/Enums/ImportDocumentMatchStatus.cs
namespace api.Domain.Enums;

public enum ImportDocumentMatchStatus
{
    Pending,
    AutoMatched,
    Unmatched,
    ManuallyAssigned,
}
```

**Step 3: Build to verify**

Run: `dotnet build api`
Expected: Build succeeded. 0 Warning(s) 0 Error(s)

**Step 4: Commit**

```bash
git add api/src/Domain/Enums/DocumentSource.cs api/src/Domain/Enums/ImportDocumentMatchStatus.cs
git commit -m "feat(3.4): add DocumentSource and ImportDocumentMatchStatus enums"
```

---

## Task 2: Domain -- Extend CandidateDocument with Workday metadata

**Mode:** Test-first

**Files:**
- Modify: `api/src/Domain/Entities/CandidateDocument.cs`
- Modify: `api/src/Infrastructure/Data/Configurations/CandidateDocumentConfiguration.cs`
- Create: `api/tests/Domain.UnitTests/Entities/CandidateDocumentTests.cs`

**Step 1: Write failing tests**

```csharp
// api/tests/Domain.UnitTests/Entities/CandidateDocumentTests.cs
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
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~CandidateDocumentTests" -v n`
Expected: FAIL -- Create overload with 5 params does not exist

**Step 3: Implement CandidateDocument extension**

Modify `api/src/Domain/Entities/CandidateDocument.cs`:

```csharp
using api.Domain.Common;
using api.Domain.Enums;

namespace api.Domain.Entities;

public class CandidateDocument : GuidEntity
{
    public Guid CandidateId { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public string BlobStorageUrl { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }
    public string? WorkdayCandidateId { get; private set; }
    public DocumentSource DocumentSource { get; private set; }

    private CandidateDocument() { } // EF Core

    internal static CandidateDocument Create(
        Guid candidateId, string documentType, string blobStorageUrl,
        string? workdayCandidateId = null,
        DocumentSource documentSource = DocumentSource.IndividualUpload)
    {
        return new CandidateDocument
        {
            CandidateId = candidateId,
            DocumentType = documentType,
            BlobStorageUrl = blobStorageUrl,
            UploadedAt = DateTimeOffset.UtcNow,
            WorkdayCandidateId = workdayCandidateId,
            DocumentSource = documentSource,
        };
    }
}
```

**Step 4: Update EF configuration**

Modify `api/src/Infrastructure/Data/Configurations/CandidateDocumentConfiguration.cs`:

```csharp
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class CandidateDocumentConfiguration : IEntityTypeConfiguration<CandidateDocument>
{
    public void Configure(EntityTypeBuilder<CandidateDocument> builder)
    {
        builder.ToTable("CandidateDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.BlobStorageUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(d => d.WorkdayCandidateId)
            .HasMaxLength(50);

        builder.Property(d => d.DocumentSource)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(d => d.CandidateId)
            .HasDatabaseName("IX_CandidateDocuments_CandidateId");

        builder.Ignore(d => d.DomainEvents);
    }
}
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~CandidateDocumentTests" -v n`
Expected: 2/2 pass

**Step 6: Run full domain test suite**

Run: `dotnet test api/tests/Domain.UnitTests -v n`
Expected: All pass (existing `Candidate.AttachDocument` tests still work since the new params have defaults)

**Step 7: Commit**

```bash
git add api/src/Domain/Entities/CandidateDocument.cs \
  api/src/Infrastructure/Data/Configurations/CandidateDocumentConfiguration.cs \
  api/tests/Domain.UnitTests/Entities/CandidateDocumentTests.cs
git commit -m "feat(3.4): extend CandidateDocument with WorkdayCandidateId and DocumentSource"
```

---

## Task 3: Domain -- Add ImportDocument child entity

**Mode:** Test-first

**Files:**
- Create: `api/src/Domain/Entities/ImportDocument.cs`
- Create: `api/tests/Domain.UnitTests/Entities/ImportDocumentTests.cs`

**Step 1: Write failing test**

```csharp
// api/tests/Domain.UnitTests/Entities/ImportDocumentTests.cs
using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Entities;

[TestFixture]
public class ImportDocumentTests
{
    [Test]
    public void Create_SetsAllProperties()
    {
        var importSessionId = Guid.NewGuid();

        var doc = ImportDocument.Create(
            importSessionId, "Anna Svensson",
            "https://blob/cv.pdf", "WD12345");

        doc.ImportSessionId.Should().Be(importSessionId);
        doc.CandidateName.Should().Be("Anna Svensson");
        doc.BlobStorageUrl.Should().Be("https://blob/cv.pdf");
        doc.WorkdayCandidateId.Should().Be("WD12345");
        doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.Pending);
        doc.MatchedCandidateId.Should().BeNull();
    }

    [Test]
    public void Create_WithoutWorkdayId_SetsNull()
    {
        var doc = ImportDocument.Create(
            Guid.NewGuid(), "Bob Smith",
            "https://blob/cv.pdf", null);

        doc.WorkdayCandidateId.Should().BeNull();
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportDocumentTests" -v n`
Expected: FAIL -- ImportDocument does not exist

**Step 3: Create ImportDocument entity**

```csharp
// api/src/Domain/Entities/ImportDocument.cs
using api.Domain.Common;
using api.Domain.Enums;

namespace api.Domain.Entities;

public class ImportDocument : GuidEntity
{
    public Guid ImportSessionId { get; private set; }
    public string CandidateName { get; private set; } = null!;
    public string BlobStorageUrl { get; private set; } = null!;
    public string? WorkdayCandidateId { get; private set; }
    public ImportDocumentMatchStatus MatchStatus { get; private set; }
    public Guid? MatchedCandidateId { get; private set; }

    private ImportDocument() { } // EF Core

    internal static ImportDocument Create(
        Guid importSessionId,
        string candidateName,
        string blobStorageUrl,
        string? workdayCandidateId)
    {
        return new ImportDocument
        {
            ImportSessionId = importSessionId,
            CandidateName = candidateName,
            BlobStorageUrl = blobStorageUrl,
            WorkdayCandidateId = workdayCandidateId,
            MatchStatus = ImportDocumentMatchStatus.Pending,
        };
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportDocumentTests" -v n`
Expected: 2/2 pass

**Step 5: Commit**

```bash
git add api/src/Domain/Entities/ImportDocument.cs \
  api/tests/Domain.UnitTests/Entities/ImportDocumentTests.cs
git commit -m "feat(3.4): add ImportDocument child entity with match status tracking"
```

---

## Task 4: Domain -- Extend ImportSession with PDF progress and ImportDocument collection

**Mode:** Test-first

**Files:**
- Modify: `api/src/Domain/Entities/ImportSession.cs`
- Modify: `api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs`
- Create: `api/src/Infrastructure/Data/Configurations/ImportDocumentConfiguration.cs`
- Modify: `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs` (add new tests at bottom)

**Step 1: Write failing tests**

Add to the bottom of `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`:

```csharp
    // === PDF splitting progress tests ===

    [Test]
    public void SetPdfSplitProgress_WhenProcessing_UpdatesFields()
    {
        var session = CreateSession();

        session.SetPdfSplitProgress(10, 5, 1);

        session.PdfTotalCandidates.Should().Be(10);
        session.PdfSplitCandidates.Should().Be(5);
        session.PdfSplitErrors.Should().Be(1);
    }

    [Test]
    public void SetPdfSplitProgress_WhenNotProcessing_Throws()
    {
        var session = CreateSession();
        session.MarkCompleted(1, 0, 0, 0);

        var act = () => session.SetPdfSplitProgress(10, 5, 0);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void SetOriginalBundleUrl_StoresUrl()
    {
        var session = CreateSession();

        session.SetOriginalBundleUrl("recruitments/abc/bundles/original.pdf");

        session.OriginalBundleBlobUrl.Should().Be("recruitments/abc/bundles/original.pdf");
    }

    [Test]
    public void AddImportDocument_WhenProcessing_CreatesChildEntity()
    {
        var session = CreateSession();

        session.AddImportDocument("Anna Svensson", "https://blob/cv.pdf", "WD12345");

        session.ImportDocuments.Should().HaveCount(1);
        var doc = session.ImportDocuments.First();
        doc.CandidateName.Should().Be("Anna Svensson");
        doc.BlobStorageUrl.Should().Be("https://blob/cv.pdf");
        doc.WorkdayCandidateId.Should().Be("WD12345");
        doc.ImportSessionId.Should().Be(session.Id);
    }

    [Test]
    public void AddImportDocument_WhenNotProcessing_Throws()
    {
        var session = CreateSession();
        session.MarkCompleted(1, 0, 0, 0);

        var act = () => session.AddImportDocument("Name", "url", null);

        act.Should().Throw<InvalidWorkflowTransitionException>();
    }

    [Test]
    public void ClearImportDocuments_RemovesAllDocuments()
    {
        var session = CreateSession();
        session.AddImportDocument("Anna", "url1", "WD1");
        session.AddImportDocument("Bob", "url2", "WD2");

        session.ClearImportDocuments();

        session.ImportDocuments.Should().BeEmpty();
    }
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportSessionTests" -v n`
Expected: FAIL -- SetPdfSplitProgress, PdfTotalCandidates, etc. do not exist

**Step 3: Extend ImportSession entity**

Add these properties and methods to `api/src/Domain/Entities/ImportSession.cs`. Add after the existing `_rowResults` field (before the private constructor):

New using at top:
```csharp
// No new usings needed -- ImportDocument is in same namespace
```

New fields/properties (add after line 24 `public IReadOnlyCollection<ImportRowResult> RowResults => ...`):

```csharp
    public int? PdfTotalCandidates { get; private set; }
    public int? PdfSplitCandidates { get; private set; }
    public int PdfSplitErrors { get; private set; }
    public string? OriginalBundleBlobUrl { get; private set; }

    private readonly List<ImportDocument> _importDocuments = new();
    public IReadOnlyCollection<ImportDocument> ImportDocuments => _importDocuments.AsReadOnly();
```

New methods (add after RejectMatch, before EnsureProcessing):

```csharp
    public void SetPdfSplitProgress(int total, int completed, int errors)
    {
        EnsureProcessing();
        PdfTotalCandidates = total;
        PdfSplitCandidates = completed;
        PdfSplitErrors = errors;
    }

    public void SetOriginalBundleUrl(string url)
    {
        OriginalBundleBlobUrl = url;
    }

    public void AddImportDocument(string candidateName, string blobStorageUrl, string? workdayCandidateId)
    {
        EnsureProcessing();
        _importDocuments.Add(ImportDocument.Create(Id, candidateName, blobStorageUrl, workdayCandidateId));
    }

    public void ClearImportDocuments()
    {
        _importDocuments.Clear();
    }
```

**Step 4: Create ImportDocumentConfiguration**

```csharp
// api/src/Infrastructure/Data/Configurations/ImportDocumentConfiguration.cs
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class ImportDocumentConfiguration : IEntityTypeConfiguration<ImportDocument>
{
    public void Configure(EntityTypeBuilder<ImportDocument> builder)
    {
        builder.ToTable("ImportDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.CandidateName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.BlobStorageUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(d => d.WorkdayCandidateId)
            .HasMaxLength(50);

        builder.Property(d => d.MatchStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(d => d.ImportSessionId)
            .HasDatabaseName("IX_ImportDocuments_ImportSessionId");

        builder.Ignore(d => d.DomainEvents);
    }
}
```

**Step 5: Update ImportSessionConfiguration**

Add to `api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs`, inside the `Configure` method, before the `Ignore(DomainEvents)` line:

```csharp
        builder.Property(s => s.OriginalBundleBlobUrl)
            .HasMaxLength(2048);

        builder.HasMany(s => s.ImportDocuments)
            .WithOne()
            .HasForeignKey(d => d.ImportSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF about the backing field for the ImportDocuments collection
        builder.Navigation(s => s.ImportDocuments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Step 6: Run tests to verify they pass**

Run: `dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~ImportSessionTests" -v n`
Expected: All pass (existing + 6 new)

**Step 7: Run full domain test suite**

Run: `dotnet test api/tests/Domain.UnitTests -v n`
Expected: All pass

**Step 8: Commit**

```bash
git add api/src/Domain/Entities/ImportSession.cs \
  api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs \
  api/src/Infrastructure/Data/Configurations/ImportDocumentConfiguration.cs \
  api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs
git commit -m "feat(3.4): extend ImportSession with PDF progress fields and ImportDocument collection"
```

---

## Task 5: Application -- Add IPdfSplitter, IBlobStorageService interfaces and value objects

**Mode:** N/A (interface definitions + record types, no behavior)

**Files:**
- Create: `api/src/Application/Common/Interfaces/IPdfSplitter.cs`
- Create: `api/src/Application/Common/Interfaces/IBlobStorageService.cs`
- Create: `api/src/Application/Common/Models/PdfSplitResult.cs`

**Step 1: Create IPdfSplitter interface**

```csharp
// api/src/Application/Common/Interfaces/IPdfSplitter.cs
using api.Application.Common.Models;

namespace api.Application.Common.Interfaces;

public interface IPdfSplitter
{
    Task<PdfSplitResult> SplitBundleAsync(
        Stream pdfStream,
        IProgress<PdfSplitProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

**Step 2: Create IBlobStorageService interface**

```csharp
// api/src/Application/Common/Interfaces/IBlobStorageService.cs
namespace api.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Uri GenerateSasUri(
        string containerName,
        string blobName,
        TimeSpan validity);
}
```

**Step 3: Create PdfSplitResult value objects**

```csharp
// api/src/Application/Common/Models/PdfSplitResult.cs
namespace api.Application.Common.Models;

public record PdfSplitResult(
    bool Success,
    IReadOnlyList<PdfSplitEntry> Entries,
    string? ErrorMessage);

public record PdfSplitEntry(
    string CandidateName,
    string? WorkdayCandidateId,
    int StartPage,
    int EndPage,
    byte[]? PdfBytes,
    string? ErrorMessage);

public record PdfSplitProgress(
    int TotalCandidates,
    int CompletedCandidates,
    string? CurrentCandidateName);
```

**Step 4: Build to verify**

Run: `dotnet build api`
Expected: Build succeeded. 0 Warning(s) 0 Error(s)

**Step 5: Commit**

```bash
git add api/src/Application/Common/Interfaces/IPdfSplitter.cs \
  api/src/Application/Common/Interfaces/IBlobStorageService.cs \
  api/src/Application/Common/Models/PdfSplitResult.cs
git commit -m "feat(3.4): add IPdfSplitter and IBlobStorageService interfaces with value objects"
```

---

## Task 6: Infrastructure -- Install NuGet packages

**Mode:** N/A

**Files:**
- Modify: `api/Directory.Packages.props`
- Modify: `api/src/Infrastructure/Infrastructure.csproj`

**Step 1: Add package versions to Directory.Packages.props**

Add inside the `<ItemGroup>` in `api/Directory.Packages.props`:

```xml
    <PackageVersion Include="UglyToad.PdfPig" Version="0.1.10" />
    <PackageVersion Include="Azure.Storage.Blobs" Version="12.24.0" />
```

**Step 2: Add package references to Infrastructure.csproj**

Add to the `<ItemGroup>` in `api/src/Infrastructure/Infrastructure.csproj`:

```xml
    <PackageReference Include="UglyToad.PdfPig" />
    <PackageReference Include="Azure.Storage.Blobs" />
```

**Step 3: Restore and build**

Run: `dotnet restore api && dotnet build api`
Expected: Build succeeded. 0 Warning(s) 0 Error(s)

**Step 4: Commit**

```bash
git add api/Directory.Packages.props api/src/Infrastructure/Infrastructure.csproj
git commit -m "chore(3.4): add UglyToad.PdfPig and Azure.Storage.Blobs packages"
```

---

## Task 7: Infrastructure -- Implement PdfSplitterService

**Mode:** Spike then test-first (PdfPig is new library; write implementation first, then test with programmatic PDFs)

**Files:**
- Create: `api/src/Infrastructure/Services/PdfSplitterService.cs`
- Create: `api/tests/Domain.UnitTests/Services/PdfSplitterServiceTests.cs`

**Step 1: Implement PdfSplitterService**

```csharp
// api/src/Infrastructure/Services/PdfSplitterService.cs
using System.Text.RegularExpressions;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using UglyToad.PdfPig;
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
            !bookmarks.GetNodes().Any())
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
        UglyToad.PdfPig.Outline.Bookmarks bookmarks, int totalPages)
    {
        var entries = new List<TocEntry>();
        var roots = bookmarks.GetNodes()
            .Where(b => b.PageNumber.HasValue)
            .OrderBy(b => b.PageNumber!.Value)
            .ToList();

        for (int i = 0; i < roots.Count; i++)
        {
            var name = roots[i].Title;
            var startPage = roots[i].PageNumber!.Value;
            var endPage = i + 1 < roots.Count
                ? roots[i + 1].PageNumber!.Value - 1
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
```

**Step 2: Build to verify**

Run: `dotnet build api`
Expected: Build succeeded.

> **Note:** `TryGetBookmarks` returns a `Bookmarks` object. Use `bookmarks.GetNodes()` to iterate all bookmark nodes. Each node has `Title`, `PageNumber`, and `Children`. We use top-level nodes only (GetNodes flattens the tree, but we filter by `PageNumber.HasValue`). The exact PdfPig API may differ slightly by version -- if `GetNodes()` doesn't compile, check the installed version's API. `bookmarks.Roots` may be the correct property in some versions.

**Step 3: Write tests using programmatic PDFs**

The tests create minimal PDFs with bookmarks using PdfPig's `PdfDocumentBuilder`, then pass them to the service. This avoids needing fixture files.

```csharp
// api/tests/Domain.UnitTests/Services/PdfSplitterServiceTests.cs
using api.Application.Common.Models;
using api.Infrastructure.Services;
using FluentAssertions;
using NUnit.Framework;
using UglyToad.PdfPig.Writer;

namespace api.Domain.UnitTests.Services;

[TestFixture]
public class PdfSplitterServiceTests
{
    private PdfSplitterService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new PdfSplitterService();
    }

    private static Stream CreatePdfWithBookmarks(params (string title, int pageIndex)[] bookmarks)
    {
        var builder = new PdfDocumentBuilder();

        // Create 6 pages with minimal content
        for (int i = 0; i < 6; i++)
        {
            var pageBuilder = builder.AddPage(PageSize.A4);
            // Add minimal content so pages are valid
            var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
            pageBuilder.AddText($"Page {i + 1}", 12, new UglyToad.PdfPig.Core.PdfPoint(72, 720), font);
        }

        foreach (var (title, pageIndex) in bookmarks)
        {
            builder.AddBookmark(title, pageIndex);
        }

        var bytes = builder.Build();
        return new MemoryStream(bytes);
    }

    private static Stream CreatePdfWithoutBookmarks()
    {
        var builder = new PdfDocumentBuilder();
        var pageBuilder = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        pageBuilder.AddText("No bookmarks", 12, new UglyToad.PdfPig.Core.PdfPoint(72, 720), font);
        var bytes = builder.Build();
        return new MemoryStream(bytes);
    }

    [Test]
    public async Task SplitBundleAsync_ValidBundle_ReturnsSplitEntries()
    {
        using var pdf = CreatePdfWithBookmarks(
            ("Svensson, Anna (WD001)", 0),
            ("Johansson, Erik (WD002)", 2),
            ("Lindberg, Sara (WD003)", 4));

        var result = await _service.SplitBundleAsync(pdf);

        result.Success.Should().BeTrue();
        result.Entries.Should().HaveCount(3);
        result.Entries[0].CandidateName.Should().Be("Svensson, Anna");
        result.Entries[0].WorkdayCandidateId.Should().Be("WD001");
        result.Entries[0].PdfBytes.Should().NotBeNull();
        result.Entries[1].CandidateName.Should().Be("Johansson, Erik");
        result.Entries[2].CandidateName.Should().Be("Lindberg, Sara");
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
            ("Plain Name", 0),
            ("Another Name (WD999)", 3));

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
            ("Alice (WD1)", 0),
            ("Bob (WD2)", 3));

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
}
```

> **IMPORTANT:** The `PdfDocumentBuilder.AddBookmark` API may not exist in all PdfPig versions. If it doesn't compile, the tests need adaptation -- check the installed PdfPig version's API. The bookmark creation API in PdfPig is `builder.AddBookmark(string title, int pageIndex, DocumentBookmarkNode? parent = null)` where `pageIndex` is 0-based. If this method is not available in the installed version, you may need to use a test PDF file as a fixture instead. In that case, create a minimal PDF with bookmarks using any tool and place it in `api/tests/Domain.UnitTests/TestData/`.

> **Also:** The `PdfSplitterService` references `bookmarks.GetNodes()`. PdfPig's `Bookmarks` type provides `GetNodes()` which returns a flat enumeration. If the API surface differs, adapt to use `bookmarks.Roots` and recursively flatten. Check the installed PdfPig API.

**Step 4: Add Domain.UnitTests project reference to Infrastructure (for testing PdfSplitterService)**

The PdfSplitterService lives in Infrastructure, so its test file needs a reference. Either:
- Add to `api/tests/Domain.UnitTests/Domain.UnitTests.csproj`:
  ```xml
  <ProjectReference Include="..\..\src\Infrastructure\Infrastructure.csproj" />
  ```
- OR create a separate test project. For simplicity, add the reference since Domain.UnitTests already has the test infrastructure.

**Step 5: Run tests**

Run: `dotnet test api/tests/Domain.UnitTests --filter "FullyQualifiedName~PdfSplitterServiceTests" -v n`
Expected: All pass

> **Fallback:** If PdfPig's builder API doesn't support `AddBookmark`, the tests for "valid bundle" won't work with programmatic PDFs. In that case:
> 1. Create a test PDF fixture file with bookmarks (using any PDF tool)
> 2. Place it at `api/tests/Domain.UnitTests/TestData/workday-bundle-sample.pdf`
> 3. Embed as a resource or read from disk in tests
> 4. The "no bookmarks" test can still use `CreatePdfWithoutBookmarks()`

**Step 6: Run full domain test suite**

Run: `dotnet test api/tests/Domain.UnitTests -v n`
Expected: All pass

**Step 7: Commit**

```bash
git add api/src/Infrastructure/Services/PdfSplitterService.cs \
  api/tests/Domain.UnitTests/Services/PdfSplitterServiceTests.cs \
  api/tests/Domain.UnitTests/Domain.UnitTests.csproj
git commit -m "feat(3.4): implement PdfSplitterService with TOC parsing and page extraction"
```

---

## Task 8: Infrastructure -- Implement BlobStorageService

**Mode:** Spike (Azure SDK integration; no integration tests without Azurite)

**Files:**
- Create: `api/src/Infrastructure/Services/BlobStorageService.cs`

**Step 1: Implement BlobStorageService**

```csharp
// api/src/Infrastructure/Services/BlobStorageService.cs
using api.Application.Common.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace api.Infrastructure.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken: cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public Uri GenerateSasUri(
        string containerName,
        string blobName,
        TimeSpan validity)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(validity));
    }
}
```

**Step 2: Build to verify**

Run: `dotnet build api`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add api/src/Infrastructure/Services/BlobStorageService.cs
git commit -m "feat(3.4): implement BlobStorageService with Azure.Storage.Blobs SDK"
```

> **Note:** Integration tests against Azurite are deferred. The service is simple SDK wrapper code and will be tested through the orchestration command handler's unit tests with mocked IBlobStorageService.

---

## Task 9: Infrastructure -- Register new services in DI

**Mode:** N/A

**Files:**
- Modify: `api/src/Infrastructure/DependencyInjection.cs`

**Step 1: Register services**

Add to `api/src/Infrastructure/DependencyInjection.cs`, in the `AddInfrastructureServices` method, after the existing import pipeline services block:

```csharp
        // PDF splitting and blob storage services
        builder.Services.AddScoped<IPdfSplitter, PdfSplitterService>();
        builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

        // Azure Blob Storage client
        var blobConnectionString = builder.Configuration.GetValue<string>("BlobStorage:ConnectionString")
            ?? "UseDevelopmentStorage=true";
        builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
```

Add the required using at the top:
```csharp
using Azure.Storage.Blobs;
```

**Step 2: Add blob storage config to appsettings.Development.json**

Check if the file exists and add BlobStorage section. If `appsettings.Development.json` doesn't have the section, add:

```json
{
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "documents",
    "SasTokenValidityMinutes": 15
  }
}
```

**Step 3: Build to verify**

Run: `dotnet build api`
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add api/src/Infrastructure/DependencyInjection.cs
git commit -m "feat(3.4): register PdfSplitter and BlobStorage services in DI"
```

---

## Task 10: Application -- Add ProcessPdfBundleCommand and handler

**Mode:** Test-first

**Files:**
- Create: `api/src/Application/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommand.cs`
- Create: `api/src/Application/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommandHandler.cs`
- Create: `api/tests/Application.UnitTests/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommandHandlerTests.cs`

**Step 1: Create the command**

```csharp
// api/src/Application/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommand.cs
namespace api.Application.Features.Import.Commands.ProcessPdfBundle;

public record ProcessPdfBundleCommand(
    Guid ImportSessionId,
    Guid RecruitmentId,
    Stream PdfStream) : IRequest;
```

**Step 2: Write failing tests**

```csharp
// api/tests/Application.UnitTests/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommandHandlerTests.cs
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

    private ImportSession CreateProcessingSession(Guid recruitmentId)
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
```

**Step 3: Run tests to verify they fail**

Run: `dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~ProcessPdfBundleCommandHandlerTests" -v n`
Expected: FAIL -- ProcessPdfBundleCommandHandler does not exist

**Step 4: Implement the handler**

```csharp
// api/src/Application/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommandHandler.cs
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
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~ProcessPdfBundleCommandHandlerTests" -v n`
Expected: All 4 pass

**Step 6: Commit**

```bash
git add api/src/Application/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommand.cs \
  api/src/Application/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommandHandler.cs \
  api/tests/Application.UnitTests/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommandHandlerTests.cs
git commit -m "feat(3.4): add ProcessPdfBundleCommand handler with orchestration logic and tests"
```

---

## Task 11: Infrastructure -- Extend ImportRequest and wire PDF into ImportPipelineHostedService

**Mode:** Characterization

**Files:**
- Modify: `api/src/Application/Common/Models/ImportRequest.cs`
- Modify: `api/src/Infrastructure/Services/ImportPipelineHostedService.cs`
- Modify: `api/src/Application/Features/Import/Commands/StartImport/StartImportCommandHandler.cs`
- Modify: `api/src/Application/Features/Import/Commands/StartImport/StartImportCommandValidator.cs`

**Step 1: Extend ImportRequest with optional PDF content**

Modify `api/src/Application/Common/Models/ImportRequest.cs`:

```csharp
namespace api.Application.Common.Models;

public sealed record ImportRequest(
    Guid ImportSessionId,
    Guid RecruitmentId,
    byte[] FileContent,
    Guid CreatedByUserId,
    byte[]? PdfBundleContent = null);
```

**Step 2: Update StartImportCommandValidator to accept PDF files**

Modify the validator to accept both `.xlsx` and `.pdf`:

```csharp
namespace api.Application.Features.Import.Commands.StartImport;

public class StartImportCommandValidator : AbstractValidator<StartImportCommand>
{
    private const long MaxXlsxFileSize = 10 * 1024 * 1024; // 10 MB
    private const long MaxPdfFileSize = 100 * 1024 * 1024; // 100 MB (AC1)

    public StartImportCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.FileContent).NotEmpty().WithMessage("File content is required.");
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(name =>
                name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .xlsx and .pdf files are supported.");
        RuleFor(x => x.FileSize)
            .Must((cmd, size) =>
            {
                if (cmd.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return size <= MaxPdfFileSize;
                return size <= MaxXlsxFileSize;
            })
            .WithMessage("File size exceeds maximum allowed.");
    }
}
```

**Step 3: Update StartImportCommandHandler to detect PDF and include in ImportRequest**

In `api/src/Application/Features/Import/Commands/StartImport/StartImportCommandHandler.cs`, change the channel write to detect PDF:

Replace the existing `channelWriter.WriteAsync` line:

```csharp
        var isPdf = request.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        await channelWriter.WriteAsync(
            new ImportRequest(
                session.Id,
                request.RecruitmentId,
                isPdf ? [] : request.FileContent,
                userId.Value,
                isPdf ? request.FileContent : null),
            cancellationToken);
```

**Step 4: Update ImportPipelineHostedService to dispatch PDF processing**

In `api/src/Infrastructure/Services/ImportPipelineHostedService.cs`, add PDF processing after XLSX processing.

Add `MediatR.ISender` to constructor params and resolve it:

```csharp
public class ImportPipelineHostedService(
    ChannelReader<ImportRequest> channelReader,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportPipelineHostedService> logger)
    : BackgroundService
```

In the `ProcessImportAsync` method, after the existing XLSX processing `try/catch` block (around line 133), add:

```csharp
        // PDF bundle processing (if present)
        if (request.PdfBundleContent is { Length: > 0 })
        {
            try
            {
                var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
                using var pdfStream = new MemoryStream(request.PdfBundleContent);
                await sender.Send(
                    new Application.Features.Import.Commands.ProcessPdfBundle.ProcessPdfBundleCommand(
                        request.ImportSessionId, request.RecruitmentId, pdfStream), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "PDF bundle processing failed for session {ImportSessionId}", request.ImportSessionId);
                // Session failure is handled by the command handler
            }
        }
```

Also: if only a PDF was uploaded (no XLSX), the existing XLSX processing code will receive an empty `FileContent` array. Guard the XLSX path:

Wrap the existing XLSX processing block with:
```csharp
        if (request.FileContent.Length > 0)
        {
            // existing XLSX processing code...
        }
```

**Step 5: Build to verify**

Run: `dotnet build api`
Expected: Build succeeded.

**Step 6: Run full test suite**

Run: `dotnet test api/tests/Domain.UnitTests -v n`
Expected: All pass

**Step 7: Commit**

```bash
git add api/src/Application/Common/Models/ImportRequest.cs \
  api/src/Infrastructure/Services/ImportPipelineHostedService.cs \
  api/src/Application/Features/Import/Commands/StartImport/StartImportCommandHandler.cs \
  api/src/Application/Features/Import/Commands/StartImport/StartImportCommandValidator.cs
git commit -m "feat(3.4): wire PDF bundle processing into import pipeline and extend validator"
```

---

## Task 12: Application -- Extend GetImportSession query with PDF progress fields

**Mode:** Characterization

**Files:**
- Modify: `api/src/Application/Features/Import/Queries/GetImportSession/ImportSessionDto.cs`

**Step 1: Add PDF progress fields to ImportSessionDto**

Add to the `ImportSessionDto` record properties:

```csharp
    public int? PdfTotalCandidates { get; init; }
    public int? PdfSplitCandidates { get; init; }
    public int PdfSplitErrors { get; init; }
    public string? OriginalBundleBlobUrl { get; init; }
    public List<ImportDocumentDto> ImportDocuments { get; init; } = new();
```

Add to the `From()` mapping:

```csharp
        PdfTotalCandidates = entity.PdfTotalCandidates,
        PdfSplitCandidates = entity.PdfSplitCandidates,
        PdfSplitErrors = entity.PdfSplitErrors,
        OriginalBundleBlobUrl = entity.OriginalBundleBlobUrl,
        ImportDocuments = entity.ImportDocuments.Select(ImportDocumentDto.From).ToList(),
```

**Step 2: Add ImportDocumentDto**

Add at the bottom of the same file:

```csharp
public record ImportDocumentDto
{
    public Guid Id { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public string BlobStorageUrl { get; init; } = string.Empty;
    public string? WorkdayCandidateId { get; init; }
    public string MatchStatus { get; init; } = string.Empty;
    public Guid? MatchedCandidateId { get; init; }

    public static ImportDocumentDto From(Domain.Entities.ImportDocument entity) => new()
    {
        Id = entity.Id,
        CandidateName = entity.CandidateName,
        BlobStorageUrl = entity.BlobStorageUrl,
        WorkdayCandidateId = entity.WorkdayCandidateId,
        MatchStatus = entity.MatchStatus.ToString(),
        MatchedCandidateId = entity.MatchedCandidateId,
    };
}
```

**Step 3: Build to verify**

Run: `dotnet build api`
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add api/src/Application/Features/Import/Queries/GetImportSession/ImportSessionDto.cs
git commit -m "feat(3.4): extend ImportSessionDto with PDF split progress and ImportDocument data"
```

---

## Task 13: Full verification and build

**Mode:** Verification

**Step 1: Build entire solution**

Run: `dotnet build api`
Expected: Build succeeded. 0 Warning(s) 0 Error(s)

**Step 2: Run all domain tests**

Run: `dotnet test api/tests/Domain.UnitTests -v n`
Expected: All pass

**Step 3: Run all application tests** (if environment supports it)

Run: `dotnet test api/tests/Application.UnitTests -v n`
Expected: All pass (or note environment limitation with ASP.NET Core runtime)

**Step 4: Anti-pattern scan**

Verify no direct `dbContext.ImportDocuments.Add()` calls -- all additions go through `ImportSession.AddImportDocument()`.
Verify no PII in log messages (candidate names logged at Warning level only, not in structured fields visible to monitoring).

---

## Task 14: Update Dev Agent Record and commit

**Mode:** N/A

**Files:**
- Modify: `_bmad-output/implementation-artifacts/3-4-pdf-bundle-upload-splitting.md`

**Step 1: Update story file status and Dev Agent Record**

Change `Status: ready-for-dev` to `Status: done`

Fill the Dev Agent Record section:
- Agent Model Used
- Testing Mode Rationale (test-first for domain/handler, spike for PDF/blob infrastructure)
- Key Decisions
- Debug Log
- Completion Notes
- File List

**Step 2: Final commit**

```bash
git add _bmad-output/implementation-artifacts/3-4-pdf-bundle-upload-splitting.md
git commit -m "docs(3.4): fill Dev Agent Record, mark story done"
```

---

## Summary

| Task | Description | Mode | Tests Added |
|------|-------------|------|-------------|
| 1 | Enums (DocumentSource, ImportDocumentMatchStatus) | N/A | 0 |
| 2 | CandidateDocument extension (WorkdayCandidateId, DocumentSource) | Test-first | 2 |
| 3 | ImportDocument child entity | Test-first | 2 |
| 4 | ImportSession PDF progress + ImportDocument collection | Test-first | 6 |
| 5 | IPdfSplitter, IBlobStorageService interfaces + value objects | N/A | 0 |
| 6 | NuGet packages (PdfPig, Azure.Storage.Blobs) | N/A | 0 |
| 7 | PdfSplitterService implementation + tests | Spike/test | 4 |
| 8 | BlobStorageService implementation | Spike | 0 |
| 9 | DI registration | N/A | 0 |
| 10 | ProcessPdfBundleCommand handler + tests | Test-first | 4 |
| 11 | Pipeline wiring (ImportRequest, HostedService, Validator) | Characterization | 0 |
| 12 | GetImportSession DTO extension | Characterization | 0 |
| 13 | Full verification | Verification | 0 |
| 14 | Dev Agent Record | N/A | 0 |

**Total new tests: ~18**
**Total new/modified files: ~20**

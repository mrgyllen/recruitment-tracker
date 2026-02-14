# Story 3.4: PDF Bundle Upload & Splitting

Status: ready-for-dev

## Story

As a **recruiting leader (Erik)**,
I want to **upload a Workday CV bundle PDF and have the system automatically split it into individual per-candidate documents**,
so that **each candidate has their own viewable CV without manual file splitting**.

## Acceptance Criteria

### AC1: PDF bundle upload validation
**Given** an active recruitment exists
**When** the user uploads a PDF bundle file via the import endpoint
**Then** the system validates the file type (.pdf) and size (<=100 MB)
**And** if validation fails, the API returns 400 Bad Request with Problem Details describing the issue

### AC2: TOC parsing and candidate identification
**Given** a valid PDF bundle is uploaded as part of an import session
**When** the import pipeline processes the bundle
**Then** the system parses the Workday TOC table to identify candidate entries
**And** each TOC entry's candidate name and Workday Candidate ID are extracted

### AC3: Page boundary detection and PDF splitting
**Given** the TOC has been parsed
**When** the system determines page boundaries
**Then** individual per-candidate PDF documents are produced from the bundle using TOC link annotations for page boundaries
**And** each split PDF contains the complete CV + letter content for that candidate

### AC4: Blob storage and document linking
**Given** individual PDFs have been split
**When** the system stores the documents
**Then** each PDF is uploaded to Azure Blob Storage (not the database)
**And** an `ImportDocument` tracking record is created on the ImportSession with the blob storage path
**And** the Workday Candidate ID from the TOC is stored as reference metadata on the ImportDocument
**And** actual `CandidateDocument` creation (via Candidate aggregate root) is deferred to Story 3.5's matching step

### AC5: Unparseable bundle graceful failure
**Given** the PDF bundle cannot be parsed (invalid format, no TOC found)
**When** the splitter encounters the error
**Then** the `ImportSession` records the failure with a clear error message identifying which step failed
**And** the original bundle is retained in blob storage as a fallback
**And** any candidates already split successfully before the failure are preserved

### AC6: Partial success handling
**Given** the bundle splits partially (some candidates extracted, some fail)
**When** the import session completes
**Then** successfully split documents are stored and linked
**And** failed extractions are recorded with specific error messages per candidate
**And** the import session status is "Completed" (not "Failed") with the partial results noted

### AC7: Splitting progress reporting
**Given** PDF splitting is in progress
**When** the client polls the import session
**Then** the response includes splitting progress (e.g., "Splitting PDFs: 45 of 127")

### AC8: Re-upload replaces previous bundle documents
**Given** a PDF bundle has already been uploaded for this recruitment
**When** a new bundle is uploaded
**Then** the new bundle's split documents replace any previous bundle documents
**And** individually uploaded documents (Story 3.5) are not affected

### Prerequisites
- **Story 3.2** (XLSX Import Pipeline) -- ImportPipelineHostedService, Channel<T> consumer, ImportSession aggregate, import endpoint

### FRs Fulfilled
- **FR28:** Users can upload a Workday CV bundle PDF for a recruitment
- **FR29:** The system produces individual per-candidate PDF documents from an uploaded Workday CV bundle
- **FR30:** The system extracts and stores the Workday Candidate ID from the bundle TOC as reference metadata

## Tasks / Subtasks

- [ ] Task 1: Domain -- Extend `CandidateDocument` with Workday metadata and source type (AC: #4, #8)
  - [ ] 1.1 Add `WorkdayCandidateId` (string, nullable) property to `CandidateDocument` entity
  - [ ] 1.2 Add `DocumentSource` enum (`BundleSplit`, `IndividualUpload`) and property to `CandidateDocument`
  - [ ] 1.3 Update `CandidateDocument.Create()` factory method to accept optional `workdayCandidateId` and `documentSource`
  - [ ] 1.4 Add `ImportDocument` child entity to `ImportSession` with properties: `CandidateName` (string), `BlobStorageUrl` (string), `WorkdayCandidateId` (string, nullable), `MatchStatus` (enum: Pending, AutoMatched, Unmatched, ManuallyAssigned), `MatchedCandidateId` (Guid, nullable)
  - [ ] 1.5 Unit test: `CandidateDocument.Create` sets WorkdayCandidateId and DocumentSource correctly
  - [ ] 1.6 Unit test: `ImportDocument.Create` sets CandidateName, BlobStorageUrl, WorkdayCandidateId, and default MatchStatus correctly
  - [ ] 1.7 Update `CandidateDocumentConfiguration` EF config for new columns
  - [ ] 1.8 Add `ImportSession.AddImportDocument(candidateName, blobStorageUrl, workdayCandidateId)` method that creates ImportDocument child entity with MatchStatus.Pending
  - [ ] 1.9 Add `ImportDocumentConfiguration` EF config for ImportDocument table
  - [ ] 1.10 Unit test: `ImportSession.AddImportDocument` creates child entity with correct properties

- [ ] Task 2: Domain -- Extend `ImportSession` with PDF splitting progress fields (AC: #6, #7)
  - [ ] 2.1 Add `PdfTotalCandidates` (int, nullable) and `PdfSplitCandidates` (int, nullable) properties to `ImportSession`
  - [ ] 2.2 Add `PdfSplitErrors` (int, default 0) property
  - [ ] 2.3 Add `OriginalBundleBlobUrl` (string, nullable) property to `ImportSession`
  - [ ] 2.4 Add methods: `SetPdfSplitProgress(total, completed, errors)`, `SetOriginalBundleUrl(url)`
  - [ ] 2.5 Unit test: `SetPdfSplitProgress` updates progress fields correctly
  - [ ] 2.6 Unit test: progress cannot be set on a non-Processing session
  - [ ] 2.7 Update `ImportSessionConfiguration` EF config for new columns

- [ ] Task 3: Application -- Define `IPdfSplitter` and `IBlobStorageService` interfaces (AC: #2, #3, #4)
  - [ ] 3.1 Create `IPdfSplitter` interface in `Application/Common/Interfaces/`
    ```csharp
    public interface IPdfSplitter
    {
        Task<PdfSplitResult> SplitBundleAsync(
            Stream pdfStream,
            IProgress<PdfSplitProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
    ```
  - [ ] 3.2 Create `IBlobStorageService` interface in `Application/Common/Interfaces/`
    ```csharp
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
  - [ ] 3.3 Create value objects: `PdfSplitResult`, `PdfSplitEntry`, `PdfSplitProgress` in `Application/Common/Models/`
  - [ ] 3.4 Register interfaces in DI configuration

- [ ] Task 4: Infrastructure -- Implement `BlobStorageService` (AC: #4, #5)
  - [ ] 4.1 Create `BlobStorageService` implementing `IBlobStorageService` in `Infrastructure/Services/`
  - [ ] 4.2 Use `Azure.Storage.Blobs` SDK with `BlobServiceClient` injected via DI
  - [ ] 4.3 Configure blob storage connection string in `appsettings.json` / `appsettings.Development.json`
  - [ ] 4.4 Blob naming convention: `{recruitmentId}/{candidateId}/{documentId}.pdf`
  - [ ] 4.5 Original bundle stored as: `{recruitmentId}/bundles/{importSessionId}_original.pdf`
  - [ ] 4.6 SAS token generation with configurable validity (default: 15 minutes, from config)
  - [ ] 4.7 Integration test (spike): upload, download, delete, SAS generation against Azurite

- [ ] Task 5: Infrastructure -- Implement `PdfSplitterService` (AC: #2, #3, #5, #6)
  - [ ] 5.1 Create `PdfSplitterService` implementing `IPdfSplitter` in `Infrastructure/Services/`
  - [ ] 5.2 Use PdfPig library (`UglyToad.PdfPig`) for PDF reading
  - [ ] 5.3 Implement TOC parsing: read bookmarks via `document.TryGetBookmarks()`, extract candidate name and Workday Candidate ID from bookmark titles
  - [ ] 5.4 Implement page boundary detection: each bookmark's destination page marks the start; the next bookmark's page (or document end) marks the end
  - [ ] 5.5 Implement page extraction: use `PdfDocumentBuilder.AddPage(document, pageNumber)` to copy pages for each candidate range
  - [ ] 5.6 Report progress via `IProgress<PdfSplitProgress>` after each candidate is split
  - [ ] 5.7 Handle partial failures: catch per-candidate exceptions, record error, continue to next candidate
  - [ ] 5.8 Return `PdfSplitResult` with list of `PdfSplitEntry` (success entries with byte[], failed entries with error message)
  - [ ] 5.9 Unit test: TOC with 3 candidates produces 3 split entries
  - [ ] 5.10 Unit test: PDF with no bookmarks/TOC returns error result
  - [ ] 5.11 Unit test: partial failure (1 of 3 fails) returns 2 successes + 1 error
  - [ ] 5.12 Unit test: progress reported for each candidate

- [ ] Task 6: Application -- PDF splitting orchestration in import pipeline (AC: #2, #3, #4, #5, #6, #7, #8)
  - [ ] 6.1 Create `ProcessPdfBundleCommand` in `Application/Features/Import/Commands/ProcessPdfBundle/`
  - [ ] 6.2 Command handler orchestrates: upload original bundle -> split PDF -> upload each split to blob -> create ImportDocument tracking records on ImportSession -> update progress
  - [ ] 6.3 Handler loads ImportSession, calls `IPdfSplitter.SplitBundleAsync()`, processes results
  - [ ] 6.4 For each successful split: upload to blob via `IBlobStorageService`, then add `ImportDocument` to ImportSession via `importSession.AddImportDocument(candidateName, blobUrl, workdayCandidateId)` (matching to Candidates happens in Story 3.5)
  - [ ] 6.5 For re-uploads (AC8): delete previous bundle-sourced documents from blob storage before processing new bundle
  - [ ] 6.6 Store original bundle in blob as fallback before splitting
  - [ ] 6.7 Update `ImportSession.SetPdfSplitProgress()` during processing
  - [ ] 6.8 On complete success: `ImportSession.MarkCompleted()` with split counts
  - [ ] 6.9 On partial success: `ImportSession.MarkCompleted()` with both success and error counts
  - [ ] 6.10 On total failure (no TOC, invalid PDF): `ImportSession.MarkFailed()` with descriptive reason
  - [ ] 6.11 Unit test: successful split orchestration creates ImportDocuments on ImportSession and uploads to blob
  - [ ] 6.12 Unit test: partial failure records errors but still stores successful splits
  - [ ] 6.13 Unit test: total failure marks ImportSession as Failed with reason
  - [ ] 6.14 Unit test: re-upload deletes previous bundle documents before processing
  - [ ] 6.15 Unit test: original bundle is stored before splitting begins

- [ ] Task 7: Infrastructure -- Wire PDF processing into `ImportPipelineHostedService` Channel consumer (AC: #7)
  - [ ] 7.1 Extend the `Channel<T>` message type (from Story 3.2) to include optional PDF bundle stream
  - [ ] 7.2 In the `ImportPipelineHostedService` consumer loop, after XLSX processing completes, check for PDF bundle
  - [ ] 7.3 If PDF bundle present, dispatch `ProcessPdfBundleCommand` via MediatR
  - [ ] 7.4 Ensure ImportSession tracks combined progress (XLSX rows + PDF split progress)

- [ ] Task 8: Application -- Extend `GetImportSession` query to return PDF split progress (AC: #7)
  - [ ] 8.1 Add `pdfTotalCandidates`, `pdfSplitCandidates`, `pdfSplitErrors` to `ImportSessionDto`
  - [ ] 8.2 Add `originalBundleUrl` (SAS URL generated on read) to `ImportSessionDto`
  - [ ] 8.3 Unit test: DTO mapping includes PDF split progress fields

- [ ] Task 9: EF Core migration for new fields (AC: #4, #7)
  - [ ] 9.1 Generate migration for `CandidateDocument` new columns (WorkdayCandidateId, DocumentSource)
  - [ ] 9.2 Generate migration for `ImportSession` new columns (PdfTotalCandidates, PdfSplitCandidates, PdfSplitErrors, OriginalBundleBlobUrl)
  - [ ] 9.3 Verify migration applies cleanly against SQL Server (Testcontainers)

## Dev Notes

### Affected Aggregate(s)

**Candidate** (aggregate root -- SCHEMA ONLY) -- `CandidateDocument` is a child entity of the `Candidate` aggregate. This story extends `CandidateDocument` with `WorkdayCandidateId` and `DocumentSource` fields (schema preparation for Story 3.5's document matching). This story does NOT create `CandidateDocument` records -- that happens in Story 3.5 when documents are matched to candidates.

**ImportSession** (aggregate root) -- Tracks the overall import pipeline status. This story extends it with PDF-specific progress fields (`PdfTotalCandidates`, `PdfSplitCandidates`, `PdfSplitErrors`, `OriginalBundleBlobUrl`) and a new `ImportDocument` child entity collection to track split PDF results (candidate name, blob URL, WorkdayCandidateId, match status). The existing `MarkCompleted()` and `MarkFailed()` methods handle final status transitions. New `SetPdfSplitProgress()` and `AddImportDocument()` methods manage the splitting lifecycle.

Single-aggregate: The handler only modifies the `ImportSession` aggregate (adding ImportDocument records and updating progress). It does NOT touch the Candidate aggregate. Document-to-candidate matching and CandidateDocument creation is handled by Story 3.5.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Domain CandidateDocument extension) | **Test-first** | Domain invariants -- metadata correctness, replace behavior |
| Task 2 (Domain ImportSession extension) | **Test-first** | Domain state machine -- progress tracking, status guards |
| Task 3 (Interface definitions) | **N/A** | Interface-only, no behavior to test |
| Task 4 (BlobStorageService) | **Spike** | New infrastructure integration with Azure SDK -- explore Azurite first, add integration tests before merge |
| Task 5 (PdfSplitterService) | **Spike then test-first** | New library (PdfPig) -- spike TOC parsing with a sample Workday PDF, then write structured tests for parsing logic |
| Task 6 (Orchestration command) | **Test-first** | Core business logic -- orchestration of split + store + link + progress |
| Task 7 (Pipeline wiring) | **Characterization** | Extending existing hosted service -- verify integration behavior |
| Task 8 (GetImportSession extension) | **Characterization** | DTO mapping extension -- verify via existing query tests |
| Task 9 (EF migration) | **N/A** | Generated migration -- verify via Testcontainers integration test |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **PDF Library: PdfPig (UglyToad.PdfPig)**

   PdfPig is the chosen PDF library for this project. It is open-source (Apache 2.0), lightweight, supports reading annotations/bookmarks, and can create new PDFs from existing pages.

   Install:
   ```
   dotnet add api/src/Infrastructure/Infrastructure.csproj package UglyToad.PdfPig
   ```

   Key APIs used:
   ```csharp
   // Reading bookmarks/TOC
   using (var document = PdfDocument.Open(stream))
   {
       if (document.TryGetBookmarks(out Bookmarks bookmarks))
       {
           foreach (var root in bookmarks.Roots)
           {
               // root.Title = "Candidate Name (WD12345)"
               // root.PageNumber = starting page
           }
       }
   }

   // Extracting pages into a new PDF
   var builder = new PdfDocumentBuilder();
   for (int pageNum = startPage; pageNum <= endPage; pageNum++)
   {
       builder.AddPage(sourceDocument, pageNum);
   }
   byte[] splitPdf = builder.Build();
   ```

2. **Azure Blob Storage SDK: Azure.Storage.Blobs**

   Install:
   ```
   dotnet add api/src/Infrastructure/Infrastructure.csproj package Azure.Storage.Blobs
   ```

   Configuration in `appsettings.json`:
   ```json
   {
     "BlobStorage": {
       "ConnectionString": "UseDevelopmentStorage=true",
       "ContainerName": "documents",
       "SasTokenValidityMinutes": 15
     }
   }
   ```

   For development, use **Azurite** (Azure Storage emulator). It can run via Docker:
   ```bash
   docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
     mcr.microsoft.com/azure-storage/azurite
   ```

3. **IPdfSplitter interface and value objects:**

   ```csharp
   // Application/Common/Interfaces/IPdfSplitter.cs
   public interface IPdfSplitter
   {
       Task<PdfSplitResult> SplitBundleAsync(
           Stream pdfStream,
           IProgress<PdfSplitProgress>? progress = null,
           CancellationToken cancellationToken = default);
   }

   // Application/Common/Models/PdfSplitResult.cs
   public record PdfSplitResult(
       bool Success,
       IReadOnlyList<PdfSplitEntry> Entries,
       string? ErrorMessage);

   public record PdfSplitEntry(
       string CandidateName,
       string? WorkdayCandidateId,
       int StartPage,
       int EndPage,
       byte[]? PdfBytes,          // null if extraction failed
       string? ErrorMessage);      // non-null if extraction failed

   public record PdfSplitProgress(
       int TotalCandidates,
       int CompletedCandidates,
       string? CurrentCandidateName);
   ```

4. **IBlobStorageService interface:**

   ```csharp
   // Application/Common/Interfaces/IBlobStorageService.cs
   public interface IBlobStorageService
   {
       Task<string> UploadAsync(
           string containerName, string blobName,
           Stream content, string contentType,
           CancellationToken cancellationToken = default);
       Task DeleteAsync(
           string containerName, string blobName,
           CancellationToken cancellationToken = default);
       Task<Stream> DownloadAsync(
           string containerName, string blobName,
           CancellationToken cancellationToken = default);
       Uri GenerateSasUri(
           string containerName, string blobName,
           TimeSpan validity);
   }
   ```

5. **PdfSplitterService implementation pattern:**

   ```csharp
   // Infrastructure/Services/PdfSplitterService.cs
   public class PdfSplitterService : IPdfSplitter
   {
       public async Task<PdfSplitResult> SplitBundleAsync(
           Stream pdfStream,
           IProgress<PdfSplitProgress>? progress = null,
           CancellationToken cancellationToken = default)
       {
           using var document = PdfDocument.Open(pdfStream);

           if (!document.TryGetBookmarks(out var bookmarks) || bookmarks.Roots.Count == 0)
           {
               return new PdfSplitResult(false, [], "PDF bundle has no table of contents (bookmarks). Cannot determine candidate boundaries.");
           }

           var tocEntries = ParseTocEntries(bookmarks, document.NumberOfPages);
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

           return new PdfSplitResult(true, entries, null);
       }

       private List<TocEntry> ParseTocEntries(Bookmarks bookmarks, int totalPages)
       {
           var entries = new List<TocEntry>();
           var roots = bookmarks.Roots
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

               // Parse "Candidate Name (WD12345)" format
               var (candidateName, workdayId) = ParseCandidateInfo(name);

               entries.Add(new TocEntry(candidateName, workdayId, startPage, endPage));
           }

           return entries;
       }

       private (string Name, string? WorkdayId) ParseCandidateInfo(string title)
       {
           // Expected format: "Lastname, Firstname (WD12345)"
           // or: "Lastname, Firstname"
           var match = Regex.Match(title, @"^(.+?)\s*\((\w+)\)\s*$");
           if (match.Success)
               return (match.Groups[1].Value.Trim(), match.Groups[2].Value);
           return (title.Trim(), null);
       }

       private record TocEntry(
           string CandidateName, string? WorkdayCandidateId,
           int StartPage, int EndPage);
   }
   ```

6. **BlobStorageService implementation pattern:**

   ```csharp
   // Infrastructure/Services/BlobStorageService.cs
   public class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
   {
       public async Task<string> UploadAsync(
           string containerName, string blobName,
           Stream content, string contentType,
           CancellationToken cancellationToken = default)
       {
           var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
           await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
           var blobClient = containerClient.GetBlobClient(blobName);
           await blobClient.UploadAsync(content,
               new BlobHttpHeaders { ContentType = contentType },
               cancellationToken: cancellationToken);
           return blobClient.Uri.ToString();
       }

       public async Task DeleteAsync(
           string containerName, string blobName,
           CancellationToken cancellationToken = default)
       {
           var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
           var blobClient = containerClient.GetBlobClient(blobName);
           await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
       }

       public async Task<Stream> DownloadAsync(
           string containerName, string blobName,
           CancellationToken cancellationToken = default)
       {
           var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
           var blobClient = containerClient.GetBlobClient(blobName);
           var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
           return response.Value.Content;
       }

       public Uri GenerateSasUri(
           string containerName, string blobName,
           TimeSpan validity)
       {
           var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
           var blobClient = containerClient.GetBlobClient(blobName);
           var sasUri = blobClient.GenerateSasUri(
               Azure.Storage.Sas.BlobSasPermissions.Read,
               DateTimeOffset.UtcNow.Add(validity));
           return sasUri;
       }
   }
   ```

7. **ProcessPdfBundleCommand handler pattern:**

   ```csharp
   // Application/Features/Import/Commands/ProcessPdfBundle/ProcessPdfBundleCommandHandler.cs
   public class ProcessPdfBundleCommandHandler(
       IApplicationDbContext dbContext,
       IPdfSplitter pdfSplitter,
       IBlobStorageService blobStorage,
       ITenantContext tenantContext,
       ILogger<ProcessPdfBundleCommandHandler> logger)
       : IRequestHandler<ProcessPdfBundleCommand>
   {
       private const string ContainerName = "documents";

       public async Task Handle(ProcessPdfBundleCommand request, CancellationToken ct)
       {
           var importSession = await dbContext.ImportSessions
               .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, ct)
               ?? throw new NotFoundException(nameof(ImportSession), request.ImportSessionId);

           // 1. Upload original bundle as fallback
           var bundleBlobName = $"{request.RecruitmentId}/bundles/{importSession.Id}_original.pdf";
           request.PdfStream.Position = 0;
           await blobStorage.UploadAsync(ContainerName, bundleBlobName,
               request.PdfStream, "application/pdf", ct);
           importSession.SetOriginalBundleUrl(bundleBlobName);

           // 2. Delete previous bundle documents if re-uploading (AC8)
           await DeletePreviousBundleDocumentsAsync(request.RecruitmentId, ct);

           // 3. Split the PDF
           request.PdfStream.Position = 0;
           var progressReporter = new Progress<PdfSplitProgress>(p =>
           {
               importSession.SetPdfSplitProgress(p.TotalCandidates, p.CompletedCandidates, 0);
               // Note: SaveChanges called periodically, not on every progress tick
           });

           var result = await pdfSplitter.SplitBundleAsync(request.PdfStream, progressReporter, ct);

           if (!result.Success)
           {
               importSession.MarkFailed($"PDF splitting failed: {result.ErrorMessage}");
               await dbContext.SaveChangesAsync(ct);
               return;
           }

           // 4. Upload successful splits and create CandidateDocument records
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
                       stream, "application/pdf", ct);

                   // Track split result as ImportDocument on ImportSession
                   // Matching to Candidates happens in Story 3.5
                   importSession.AddImportDocument(
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

           importSession.SetPdfSplitProgress(result.Entries.Count, successCount, errorCount);
           importSession.MarkCompleted(successCount, errorCount);
           await dbContext.SaveChangesAsync(ct);
       }
   }
   ```

   **IMPORTANT:** The handler only modifies the ImportSession aggregate. Split results are tracked as ImportDocument records. Document-to-Candidate matching and CandidateDocument creation happens in Story 3.5.

8. **Blob naming convention:**
   ```
   documents/
     {recruitmentId}/
       bundles/
         {importSessionId}_original.pdf     # Original bundle (fallback)
       cvs/
         {documentId}.pdf                   # Split individual CVs
   ```

9. **Error handling:** Domain exceptions propagate to global exception middleware for Problem Details conversion. The PDF splitter catches per-candidate errors internally and returns them as part of `PdfSplitResult`. The handler translates split failures into ImportSession status updates.

10. **ITenantContext:** The handler runs in a service context (background `ImportPipelineHostedService`). `ITenantContext.RecruitmentId` is set to the recruitment being processed. `ITenantContext.IsServiceContext` may be needed if the handler must bypass tenant filters for cross-recruitment queries.

### Architecture Compliance

- **Aggregate root access only:** Add `ImportDocument` records through `importSession.AddImportDocument()`. CandidateDocument creation (via Candidate aggregate root) happens in Story 3.5. NEVER directly add to `dbContext.ImportDocuments` or `dbContext.CandidateDocuments`.
- **Ubiquitous language:** Use "Import Session" (not upload/sync/batch), "Candidate Document" (not file/attachment).
- **Manual DTO mapping:** `ImportSessionDto.From()` factory method for the polling response.
- **Problem Details for errors:** Validation failures (wrong file type, too large) return RFC 9457 Problem Details.
- **No PII in audit events/logs:** Log candidate counts and document IDs, never names or emails.
- **NSubstitute for ALL mocking** (never Moq).
- **NFR32 compliance:** Documents stored in Azure Blob Storage, NEVER in the database.
- **NFR7 compliance:** 150 candidates / 100 MB within 60 seconds -- PdfPig is lightweight enough. If performance is insufficient, consider parallelizing page extraction.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. New migration for CandidateDocument + ImportSession fields. |
| MediatR | 13.x | `IRequest` (void) for ProcessPdfBundleCommand. Pipeline behaviors for validation. |
| FluentValidation | Latest | Validate file type (.pdf) and size (<=100 MB) on upload. |
| **UglyToad.PdfPig** | Latest (0.1.10+) | PDF reading, bookmark/TOC extraction, page-level copying via PdfDocumentBuilder. Open source, Apache 2.0. |
| **Azure.Storage.Blobs** | 12.x | Azure Blob Storage SDK. `BlobServiceClient`, `BlobContainerClient`, SAS token generation. |
| NSubstitute | Latest | Mocking for `IPdfSplitter`, `IBlobStorageService` in unit tests. |
| FluentAssertions | Latest | `.Should().Be()`, `.Should().HaveCount()` for test assertions. |
| NUnit | Latest | `[Test]`, `[TestCase]` for all test methods. |

### File Structure Requirements

**New files to create:**
```
api/src/Domain/Enums/
  DocumentSource.cs                              # BundleSplit, IndividualUpload
  ImportDocumentMatchStatus.cs                   # Pending, AutoMatched, Unmatched, ManuallyAssigned

api/src/Domain/Entities/
  ImportDocument.cs                              # Child entity of ImportSession: CandidateName, BlobStorageUrl, WorkdayCandidateId, MatchStatus, MatchedCandidateId

api/src/Application/Common/Interfaces/
  IPdfSplitter.cs
  IBlobStorageService.cs

api/src/Application/Common/Models/
  PdfSplitResult.cs                              # PdfSplitResult, PdfSplitEntry, PdfSplitProgress records

api/src/Application/Features/Import/Commands/
  ProcessPdfBundle/
    ProcessPdfBundleCommand.cs
    ProcessPdfBundleCommandHandler.cs

api/src/Infrastructure/Services/
  BlobStorageService.cs
  PdfSplitterService.cs

api/src/Infrastructure/Data/Configurations/
  CandidateDocumentConfiguration.cs              # MODIFY or CREATE if not exists

api/tests/Domain.UnitTests/Entities/
  CandidateDocumentTests.cs                      # NEW or extend existing
  ImportSessionPdfTests.cs                       # NEW

api/tests/Application.UnitTests/Features/Import/Commands/
  ProcessPdfBundleCommandTests.cs

api/tests/Infrastructure.IntegrationTests/Services/
  BlobStorageServiceTests.cs
  PdfSplitterServiceTests.cs
```

**Existing files to modify:**
```
api/src/Domain/Entities/CandidateDocument.cs     # Add WorkdayCandidateId, DocumentSource
api/src/Domain/Entities/ImportSession.cs          # Add PDF progress fields, ImportDocument collection, AddImportDocument() method
api/src/Application/Common/Interfaces/IApplicationDbContext.cs  # Add DbSet<CandidateDocument> if not present
api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs  # New columns
api/src/Web/Configuration/DependencyInjection.cs  # Register IPdfSplitter, IBlobStorageService
api/Directory.Packages.props                      # Add UglyToad.PdfPig, Azure.Storage.Blobs
api/src/Infrastructure/Infrastructure.csproj       # Add package references
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**

**Domain unit tests:**
- `CandidateDocument_Create_WithWorkdayId_SetsProperties` -- verify WorkdayCandidateId and DocumentSource are set
- `CandidateDocument_Create_WithoutWorkdayId_DefaultsToNull` -- backward compatibility
- `ImportSession_AddImportDocument_CreatesChildEntity` -- candidate name, blob URL, WorkdayCandidateId stored
- `ImportDocument_Create_SetsProperties` -- all properties set correctly, MatchStatus defaults to Pending
- `ImportSession_SetPdfSplitProgress_UpdatesFields` -- progress values correct
- `ImportSession_SetPdfSplitProgress_NotProcessing_Throws` -- status guard
- `ImportSession_SetOriginalBundleUrl_StoresUrl` -- URL stored

**Application unit tests (mocked infra):**
- `ProcessPdfBundle_ValidBundle_SplitsAndUploadsAll` -- mock IPdfSplitter returns 3 entries, mock IBlobStorageService uploads 3 + 1 original, ImportSession has 3 ImportDocuments
- `ProcessPdfBundle_PartialFailure_StoresSuccessfulSplits` -- 2 of 3 succeed, ImportSession completed with counts
- `ProcessPdfBundle_NoToc_MarksSessionFailed` -- IPdfSplitter returns failure, ImportSession.MarkFailed called
- `ProcessPdfBundle_ReUpload_DeletesPreviousBundleDocs` -- verifies previous documents deleted
- `ProcessPdfBundle_OriginalBundleStored_BeforeSplitting` -- verify upload order
- `ProcessPdfBundle_ProgressReported_DuringSplitting` -- verify SetPdfSplitProgress called

**Infrastructure integration tests:**
- `BlobStorageService_Upload_StoresBlob` -- Azurite, verify blob exists
- `BlobStorageService_Download_ReturnsContent` -- upload then download, compare
- `BlobStorageService_Delete_RemovesBlob` -- upload, delete, verify gone
- `BlobStorageService_GenerateSasUri_ReturnsValidUri` -- verify SAS token format
- `PdfSplitterService_ValidBundle_ReturnsSplitEntries` -- test with real PDF fixture
- `PdfSplitterService_NoBookmarks_ReturnsFailure` -- test with plain PDF (no TOC)
- `PdfSplitterService_PartialExtraction_ReturnsPartialResults` -- test with malformed page range
- Test naming: `MethodName_Scenario_ExpectedBehavior`

### Previous Story Intelligence

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- `CandidateDocument` already exists with `CandidateId`, `DocumentType`, `BlobStorageUrl`, `UploadedAt`
- `CandidateDocument.Create()` is an `internal static` factory method
- Child entity constructors are `private` -- only creatable through factory methods or aggregate root
- `Candidate.AttachDocument()` enforces one document per type (throws `InvalidOperationException` on duplicate)
- `Candidate.ReplaceDocument()` is NOT created in this story -- it is created in Story 3.5 when documents are matched to candidates
- `ImportSession` has `Processing -> Completed/Failed` state machine with guards
- `ApplicationDbContext` constructor requires `ITenantContext`

**From Story 2.1 (Create Recruitment):**
- CQRS folder structure: one command per folder with Command + Handler (+ optional Validator)
- `IApplicationDbContext` is used directly (no repository abstraction)
- Response DTOs use `static From()` factory methods (manual mapping)
- `NotFoundException` exists in `Application/Common/Exceptions/`

**From Story 3.2 (XLSX Import Pipeline -- in parallel):**
- `ImportPipelineHostedService` implements `IHostedService` + `Channel<T>` consumer
- Import endpoint returns 202 Accepted with import session ID and polling URL
- `GetImportSession` query provides polling endpoint
- `ImportSession.Create()` starts in Processing status
- Channel message carries the uploaded file stream + recruitment context
- PDF splitting is the NEXT step after XLSX processing in the pipeline

**From Story 3.3 (Import Wizard -- in parallel):**
- Frontend polls `GET /api/import-sessions/{id}` for progress
- ImportProgress.tsx shows "Splitting PDF bundle..." when PDF processing active
- ImportSummary.tsx will display split results (success/error counts)
- FileUploadStep.tsx handles both XLSX and PDF file selection

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(3.4): add DocumentSource enum and extend CandidateDocument with Workday metadata`
2. `feat(3.4): extend ImportSession with PDF splitting progress fields`
3. `feat(3.4): add IPdfSplitter and IBlobStorageService interfaces + value objects`
4. `feat(3.4): implement BlobStorageService with Azure.Storage.Blobs SDK`
5. `feat(3.4): implement PdfSplitterService with PdfPig TOC parsing and page extraction`
6. `feat(3.4): add ProcessPdfBundleCommand handler with orchestration logic + tests`
7. `feat(3.4): wire PDF processing into ImportPipelineHostedService consumer`
8. `feat(3.4): extend GetImportSession query with PDF split progress fields`
9. `chore(3.4): add EF Core migration for CandidateDocument + ImportSession changes`

### Latest Tech Information

- **.NET 10.0:** LTS until Nov 2028. `Stream` handling and `IProgress<T>` patterns stable.
- **PdfPig (UglyToad.PdfPig):** Latest version 0.1.10+. Open-source PDF library for .NET. Key capabilities: read bookmarks via `TryGetBookmarks()`, copy pages via `PdfDocumentBuilder.AddPage(document, pageNumber)`, lightweight memory footprint. No external native dependencies (pure .NET). Apache 2.0 license.
- **Azure.Storage.Blobs 12.x:** Stable SDK. `BlobServiceClient` injected via DI. `CreateIfNotExistsAsync` for container auto-creation. `GenerateSasUri()` for SAS token generation. Azurite for local development/testing.
- **EF Core 10:** Migration generation via `dotnet ef migrations add`. No breaking changes for new columns.
- **MediatR 13.x:** `IRequest` (void) pattern for commands with no return value. Pipeline behaviors for validation.

### Project Structure Notes

- This is a **backend/infrastructure-only** story. No frontend components are created. Frontend integration happens in Story 3.3 (Import Wizard) and Story 3.5 (CV Auto-Match).
- `PdfSplitterService` and `BlobStorageService` live in `Infrastructure/Services/` per the project structure.
- Interfaces (`IPdfSplitter`, `IBlobStorageService`) live in `Application/Common/Interfaces/` following Clean Architecture dependency inversion.
- Value objects (`PdfSplitResult`, etc.) live in `Application/Common/Models/` alongside `PaginatedList.cs`.
- The `ProcessPdfBundle` command lives in `Application/Features/Import/Commands/` since PDF splitting is part of the import pipeline.
- EF configurations in `Infrastructure/Data/Configurations/` per established pattern.
- Test fixtures (sample PDFs) can be placed in `tests/Infrastructure.IntegrationTests/TestData/` or embedded as resources.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-3-candidate-import-document-management.md` -- Story 3.4 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries (Candidate owns CandidateDocument, ImportSession tracks results), ITenantContext service context]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, error handling, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- 202 Accepted async pattern, polling endpoint, Problem Details]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Infrastructure/Services location, Application/Common/Interfaces location, test structure]
- [Source: `_bmad-output/planning-artifacts/architecture/infrastructure.md` -- IHostedService + Channel\<T\> consumer, blob storage boundary]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- NUnit + NSubstitute + FluentAssertions, Testcontainers, test naming]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR28, FR29, FR30, NFR7, NFR31, NFR32]
- [Source: `api/src/Domain/Entities/CandidateDocument.cs` -- Existing entity with CandidateId, DocumentType, BlobStorageUrl]
- [Source: `api/src/Domain/Entities/Candidate.cs` -- Existing AttachDocument() method with duplicate type guard]
- [Source: `api/src/Domain/Entities/ImportSession.cs` -- Existing Processing->Completed/Failed state machine]
- [Source: `api/src/Application/Common/Interfaces/ITenantContext.cs` -- RecruitmentId for import scoping]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

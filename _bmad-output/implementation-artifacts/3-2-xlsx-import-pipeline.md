# Story 3.2: XLSX Import Pipeline

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **recruiting leader (Erik)**,
I want to **upload a Workday XLSX export file and have the system automatically parse, validate, and import candidates**,
so that **I can quickly populate a recruitment with candidates from Workday without manual data entry**.

## Acceptance Criteria

### AC1: File upload validation
**Given** an active recruitment exists
**When** the user uploads an XLSX file via the import endpoint
**Then** the system validates the file type (.xlsx) and size (<=10 MB)
**And** if validation fails, the API returns 400 Bad Request with Problem Details describing the issue

### AC2: Import session creation (202 Accepted)
**Given** a valid XLSX file is uploaded
**When** the API receives the file
**Then** an `ImportSession` is created with status "Processing", recording who uploaded, when, and the source filename
**And** the API returns 202 Accepted with the import session ID and a status polling URL
**And** the file is queued for async processing via `Channel<T>`

### AC3: XLSX parsing
**Given** the import pipeline processes the XLSX
**When** the file is parsed
**Then** the system extracts five fields per row: full name, email, phone, location, and date applied
**And** column names are resolved using configurable column-name mapping (NFR30)

### AC4: Invalid XLSX handling
**Given** the XLSX file has an invalid format or is missing required columns
**When** the parser attempts to read it
**Then** the `ImportSession` status is set to "Failed" with a clear error message identifying what's wrong
**And** no candidates are created or modified

### AC5: Email-based matching (high confidence)
**Given** candidates are extracted from the XLSX
**When** the matching engine runs
**Then** each candidate is matched to existing records by email (case-insensitive, high confidence)
**And** the candidate's profile fields (name, phone, location, date applied) are updated from the XLSX
**And** app-side data is never overwritten (workflow states, outcomes, reason codes)

### AC6: Name+phone matching (low confidence)
**Given** a candidate has no email match
**When** the matching engine uses name + phone as a fallback
**Then** the match is flagged as low confidence for manual review
**And** the candidate's profile fields are NOT updated until the match is confirmed

### AC7: New candidate creation
**Given** a candidate in the XLSX has no match in the recruitment
**When** the import processes the row
**Then** a new candidate is created with the extracted fields
**And** the candidate is placed at the first workflow step with outcome status "Not Started"

### AC8: Existing candidates preserved
**Given** a candidate exists in the recruitment but is missing from the XLSX
**When** the import completes
**Then** the existing candidate is NOT deleted or modified

### AC9: Idempotent re-import
**Given** the same XLSX file is uploaded twice
**When** the second import processes
**Then** the result is identical to the first import (idempotent)
**And** no duplicate candidates are created
**And** no data is corrupted

### AC10: Import session polling
**Given** the import pipeline completes (success or failure)
**When** the client polls the import session endpoint
**Then** the response includes: status (Completed/Failed), summary counts (created, updated, errored, flagged), and row-level detail for any errors or flags

### AC11: Closed recruitment rejection
**Given** a recruitment is closed
**When** the user attempts to import candidates via API
**Then** the API returns 400 Bad Request with Problem Details: "Recruitment is closed"

### FRs Fulfilled
- **FR14:** Users can import candidates by uploading a Workday XLSX file
- **FR15:** The system extracts five fields from the XLSX: full name, email, phone, location, and date applied
- **FR16:** The system matches imported candidates to existing records by email (primary), with name+phone as a low-confidence fallback
- **FR17:** The system flags low-confidence matches for manual review
- **FR19:** The system tracks import sessions (who uploaded, when, source filename, summary counts)
- **FR20:** Re-importing the same file produces the same result without creating duplicates or corrupting data (idempotent)
- **FR21:** The import never overwrites app-side data (workflow states, outcomes, reason codes)
- **FR22:** The import never auto-deletes candidates missing from a re-import
- **FR25:** The system validates uploaded XLSX files before processing and reports clear errors

### Prerequisites
- **Story 3.1** (Manual Candidate Management) -- Candidate aggregate CRUD, CandidateEndpoints, candidate API client

## Tasks / Subtasks

- [ ] Task 1: Domain -- Extend ImportSession aggregate with row-level tracking (AC: #2, #10)
  - [ ] 1.1 Add `SourceFileName` property to `ImportSession` entity
  - [ ] 1.2 Add `ImportRowResult` value object with fields: row number, candidate name, email, action (Created/Updated/Errored/Flagged), match confidence, error message
  - [ ] 1.3 Add `_rowResults` collection to `ImportSession` and methods `AddRowResult()`, `RowResults` read-only collection
  - [ ] 1.4 Add `FlaggedRows` and `UpdatedRows` computed properties to `ImportSession`
  - [ ] 1.5 Update `MarkCompleted()` to accept created/updated/errored/flagged counts
  - [ ] 1.6 Domain unit tests: ImportSession creation, status transitions, row result tracking, cannot transition backwards

- [ ] Task 2: Domain -- Candidate profile update method (AC: #5)
  - [ ] 2.1 Add `UpdateProfile(fullName, email, phoneNumber, location, dateApplied)` method to `Candidate` entity
  - [ ] 2.2 Domain unit test: UpdateProfile sets all fields correctly, does NOT touch outcomes or documents

- [ ] Task 3: Infrastructure -- IXlsxParser interface and ClosedXML implementation (AC: #3, #4)
  - [ ] 3.1 Create `IXlsxParser` interface in `Application/Common/Interfaces/` (if not already present)
  - [ ] 3.2 Create `XlsxParserService` in `Infrastructure/Services/` using ClosedXML
  - [ ] 3.3 Implement configurable column-name mapping via `appsettings.json` (NFR30)
  - [ ] 3.4 Return parsed rows as `List<ParsedCandidateRow>` value object (fullName, email, phone, location, dateApplied, rowNumber)
  - [ ] 3.5 Validate required columns exist, return clear error if missing
  - [ ] 3.6 Integration tests with real XLSX test fixtures: valid file, missing columns, empty file, corrupt file

- [ ] Task 4: Infrastructure -- ICandidateMatchingEngine implementation (AC: #5, #6, #7, #8, #9)
  - [ ] 4.1 Create `ICandidateMatchingEngine` interface in `Application/Common/Interfaces/` (if not already present)
  - [ ] 4.2 Create `CandidateMatchingEngine` in `Infrastructure/Services/`
  - [ ] 4.3 Implement email matching: case-insensitive comparison, returns `CandidateMatch` with `High` confidence
  - [ ] 4.4 Implement name+phone fallback: normalized name comparison + exact phone, returns `CandidateMatch` with `Low` confidence
  - [ ] 4.5 No match: returns `CandidateMatch` with `None` confidence (create new candidate)
  - [ ] 4.6 Unit tests: email match, name+phone match, no match, case-insensitive email, idempotent re-match

- [ ] Task 5: Infrastructure -- Channel<T> and ImportPipelineHostedService (AC: #2, #3, #4, #5, #6, #7, #10)
  - [ ] 5.1 Create `ImportRequest` record: `(Guid ImportSessionId, Guid RecruitmentId, Stream FileStream)`
  - [ ] 5.2 Register `Channel<ImportRequest>` as singleton in DI (`Channel.CreateUnbounded<ImportRequest>()`)
  - [ ] 5.3 Create `ImportPipelineHostedService` as `BackgroundService` consuming from channel
  - [ ] 5.4 Implement pipeline: read from channel -> parse XLSX -> match candidates -> upsert -> update ImportSession
  - [ ] 5.5 Set `ITenantContext.RecruitmentId` before processing (scoped data access)
  - [ ] 5.6 Wrap processing in try/catch: on exception, call `importSession.MarkFailed(reason)` and save
  - [ ] 5.7 Use a scoped `IServiceProvider` per import job (create scope from `IServiceScopeFactory`)
  - [ ] 5.8 Unit tests: service starts, consumes from channel, calls parser and matching engine, updates session

- [ ] Task 6: Application -- StartImportCommand (AC: #1, #2, #11)
  - [ ] 6.1 Create `StartImportCommand` record with `Guid RecruitmentId` and `IFormFile File` (or `Stream` + `string FileName`)
  - [ ] 6.2 Create `StartImportCommandValidator`: file not null, file extension .xlsx, file size <= 10 MB, recruitmentId not empty
  - [ ] 6.3 Create `StartImportCommandHandler`: verify recruitment exists + user is member, verify recruitment is active, create ImportSession, write to Channel<T>, return session ID
  - [ ] 6.4 Create `StartImportResponse` DTO: `ImportSessionId`, `StatusUrl`
  - [ ] 6.5 Unit tests: valid upload creates session + writes to channel, closed recruitment throws, non-member throws ForbiddenAccessException

- [ ] Task 7: Application -- GetImportSessionQuery (AC: #10)
  - [ ] 7.1 Create `GetImportSessionQuery` with `Guid ImportSessionId`
  - [ ] 7.2 Create `GetImportSessionQueryHandler`: load ImportSession by ID, verify user has access (member of the recruitment)
  - [ ] 7.3 Create `ImportSessionDto` with status, summary counts, row-level results, source filename, timestamps
  - [ ] 7.4 Unit tests: returns session data, not found throws, non-member throws

- [ ] Task 8: Web -- Import endpoints (AC: #1, #2, #10, #11)
  - [ ] 8.1 Create `ImportEndpoints.cs` inheriting `EndpointGroupBase`, group name `"recruitments/{recruitmentId:guid}/import"`
  - [ ] 8.2 Map `POST /` -- accepts multipart/form-data file upload, sends `StartImportCommand`, returns 202 Accepted with session ID and polling URL
  - [ ] 8.3 Create `ImportSessionEndpoints.cs` inheriting `EndpointGroupBase`, group name `"import-sessions"`
  - [ ] 8.4 Map `GET /{id:guid}` -- sends `GetImportSessionQuery`, returns 200 OK with session data
  - [ ] 8.5 Integration tests: upload valid XLSX -> 202, invalid file type -> 400, oversized file -> 400, closed recruitment -> 400, poll session -> 200

- [ ] Task 9: Infrastructure -- EF Core configuration for ImportSession extensions (AC: #10)
  - [ ] 9.1 Update `ImportSessionConfiguration.cs` for new properties (SourceFileName, row results as owned entity collection or JSON column)
  - [ ] 9.2 Create EF Core migration for ImportSession schema changes
  - [ ] 9.3 Add index: `IX_ImportSessions_RecruitmentId`

- [ ] Task 10: DI registration and configuration (AC: all)
  - [ ] 10.1 Register `IXlsxParser` -> `XlsxParserService` in DI
  - [ ] 10.2 Register `ICandidateMatchingEngine` -> `CandidateMatchingEngine` in DI
  - [ ] 10.3 Register `ImportPipelineHostedService` as hosted service
  - [ ] 10.4 Register `Channel<ImportRequest>` as singleton
  - [ ] 10.5 Add XLSX column mapping configuration to `appsettings.json`
  - [ ] 10.6 Add ClosedXML NuGet package to Infrastructure project

## Dev Notes

### Affected Aggregate(s)

**ImportSession** (aggregate root) -- this is the primary aggregate for this story. The domain model already exists in `api/src/Domain/Entities/ImportSession.cs` with basic status transitions (Processing -> Completed/Failed). This story extends it with:
- `SourceFileName` property
- Row-level result tracking via `ImportRowResult` value objects
- Updated `MarkCompleted()` with detailed counts

**Candidate** (aggregate root, cross-aggregate) -- candidates are created/updated by the import pipeline. The `Candidate` entity already exists in `api/src/Domain/Entities/Candidate.cs` with `Create()` factory method. This story adds:
- `UpdateProfile()` method for updating profile fields during email-match import

**Recruitment** (read-only reference) -- the handler verifies the recruitment exists, is active, and the user is a member. No mutations to the Recruitment aggregate.

Key aggregate rules:
- `ImportSession` is its own aggregate root, separate from Recruitment and Candidate
- Cross-aggregate references use IDs only: `ImportSession.RecruitmentId`, `Candidate.RecruitmentId`
- The import pipeline creates Candidates via `Candidate.Create()` and updates via `candidate.UpdateProfile()` -- always through aggregate root methods
- One aggregate per transaction: each candidate upsert is saved individually within the import loop (not a single bulk transaction)

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (ImportSession extensions) | **Test-first** | Domain logic -- status transitions, row result tracking, invariant enforcement |
| Task 2 (Candidate UpdateProfile) | **Test-first** | Domain logic -- must verify only profile fields updated, not app-side data |
| Task 3 (XlsxParserService) | **Spike then test** | New library (ClosedXML) -- explore API first, then add integration tests with real XLSX fixtures before merge |
| Task 4 (CandidateMatchingEngine) | **Test-first** | Core business logic -- matching rules have clear expected behavior |
| Task 5 (ImportPipelineHostedService) | **Spike then test** | Channel<T> + BackgroundService pattern -- explore first, then unit test orchestration |
| Task 6 (StartImportCommand) | **Test-first** | Application layer orchestration -- verify session creation, channel write, auth checks |
| Task 7 (GetImportSessionQuery) | **Test-first** | Query handler -- verify DTO mapping, auth, not-found |
| Task 8 (Import endpoints) | **Test-first** | Integration boundary -- must verify 202, 400, multipart upload |
| Task 9 (EF configuration) | **Characterization** | Configuration/migration -- verify schema applies correctly |
| Task 10 (DI registration) | **Characterization** | Wiring -- verified by integration tests |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **ImportSession domain extensions:**
   ```csharp
   // api/src/Domain/ValueObjects/ImportRowResult.cs
   public sealed record ImportRowResult(
       int RowNumber,
       string? CandidateName,
       string? Email,
       ImportRowAction Action,        // Created, Updated, Errored, Flagged
       ImportMatchConfidence? MatchConfidence,
       string? ErrorMessage);

   // api/src/Domain/Enums/ImportRowAction.cs
   public enum ImportRowAction
   {
       Created,
       Updated,
       Errored,
       Flagged     // Low-confidence match pending manual review
   }
   ```

2. **ImportSession aggregate extensions:**
   ```csharp
   // Add to ImportSession.cs
   public string? SourceFileName { get; private set; }
   public int UpdatedRows { get; private set; }
   public int FlaggedRows { get; private set; }

   private readonly List<ImportRowResult> _rowResults = new();
   public IReadOnlyCollection<ImportRowResult> RowResults => _rowResults.AsReadOnly();

   public static ImportSession Create(
       Guid recruitmentId, Guid createdByUserId, string sourceFileName)
   {
       return new ImportSession
       {
           RecruitmentId = recruitmentId,
           Status = ImportSessionStatus.Processing,
           CreatedAt = DateTimeOffset.UtcNow,
           CreatedByUserId = createdByUserId,
           SourceFileName = sourceFileName,
       };
   }

   public void AddRowResult(ImportRowResult result)
   {
       EnsureProcessing();
       _rowResults.Add(result);
   }

   public void MarkCompleted(int createdCount, int updatedCount, int erroredCount, int flaggedCount)
   {
       EnsureProcessing();
       Status = ImportSessionStatus.Completed;
       SuccessfulRows = createdCount + updatedCount;
       FailedRows = erroredCount;
       UpdatedRows = updatedCount;
       FlaggedRows = flaggedCount;
       TotalRows = createdCount + updatedCount + erroredCount + flaggedCount;
       CompletedAt = DateTimeOffset.UtcNow;
   }
   ```

3. **Candidate UpdateProfile method:**
   ```csharp
   // Add to Candidate.cs
   public void UpdateProfile(
       string fullName,
       string? phoneNumber,
       string? location,
       DateTimeOffset dateApplied)
   {
       FullName = fullName;
       PhoneNumber = phoneNumber;
       Location = location;
       DateApplied = dateApplied;
       // NOTE: Email is NOT updated (it's the matching key)
       // NOTE: Outcomes and Documents are NOT touched (FR21)
   }
   ```

4. **Channel<T> producer/consumer pattern:**
   ```csharp
   // api/src/Application/Common/Models/ImportRequest.cs
   public sealed record ImportRequest(
       Guid ImportSessionId,
       Guid RecruitmentId,
       byte[] FileContent,       // File bytes (stream consumed at upload)
       string FileName);

   // DI Registration in DependencyInjection.cs or Program.cs
   builder.Services.AddSingleton(Channel.CreateUnbounded<ImportRequest>(
       new UnboundedChannelOptions { SingleReader = true }));

   // Writing to channel (in StartImportCommandHandler):
   var channel = serviceProvider.GetRequiredService<Channel<ImportRequest>>();
   await channel.Writer.WriteAsync(new ImportRequest(
       session.Id, request.RecruitmentId, fileBytes, request.FileName));
   ```

5. **ImportPipelineHostedService (BackgroundService):**
   ```csharp
   // api/src/Infrastructure/Services/ImportPipelineHostedService.cs
   public class ImportPipelineHostedService(
       Channel<ImportRequest> channel,
       IServiceScopeFactory scopeFactory,
       ILogger<ImportPipelineHostedService> logger) : BackgroundService
   {
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           await foreach (var request in channel.Reader.ReadAllAsync(stoppingToken))
           {
               try
               {
                   await ProcessImportAsync(request, stoppingToken);
               }
               catch (Exception ex)
               {
                   logger.LogError(ex,
                       "Import {ImportSessionId} failed unexpectedly",
                       request.ImportSessionId);
                   // Session marked as failed inside ProcessImportAsync
               }
           }
       }

       private async Task ProcessImportAsync(
           ImportRequest request, CancellationToken ct)
       {
           using var scope = scopeFactory.CreateScope();
           var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
           var parser = scope.ServiceProvider.GetRequiredService<IXlsxParser>();
           var matcher = scope.ServiceProvider.GetRequiredService<ICandidateMatchingEngine>();
           var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

           // Scope data access to this recruitment
           tenantContext.RecruitmentId = request.RecruitmentId;

           var session = await db.ImportSessions
               .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, ct);

           try
           {
               // 1. Parse XLSX
               using var stream = new MemoryStream(request.FileContent);
               var rows = parser.Parse(stream);

               // 2. Load existing candidates for this recruitment
               var existingCandidates = await db.Candidates
                   .Where(c => c.RecruitmentId == request.RecruitmentId)
                   .ToListAsync(ct);

               // 3. Get first workflow step for new candidate placement
               var firstStep = await db.Recruitments
                   .Where(r => r.Id == request.RecruitmentId)
                   .SelectMany(r => r.Steps)
                   .OrderBy(s => s.Order)
                   .FirstOrDefaultAsync(ct);

               int created = 0, updated = 0, errored = 0, flagged = 0;

               foreach (var row in rows)
               {
                   // 4. Match and upsert
                   var match = matcher.Match(row, existingCandidates);
                   // ... process based on match confidence
               }

               session!.MarkCompleted(created, updated, errored, flagged);
           }
           catch (Exception ex)
           {
               session!.MarkFailed(ex.Message);
           }

           await db.SaveChangesAsync(ct);
       }
   }
   ```

6. **IXlsxParser interface and implementation:**
   ```csharp
   // api/src/Application/Common/Interfaces/IXlsxParser.cs
   public interface IXlsxParser
   {
       List<ParsedCandidateRow> Parse(Stream xlsxStream);
   }

   // api/src/Domain/ValueObjects/ParsedCandidateRow.cs
   public sealed record ParsedCandidateRow(
       int RowNumber,
       string FullName,
       string Email,
       string? PhoneNumber,
       string? Location,
       DateTimeOffset? DateApplied);

   // api/src/Infrastructure/Services/XlsxParserService.cs
   // Uses ClosedXML to read the XLSX
   // Column mapping from IOptions<XlsxColumnMappingOptions>
   ```

7. **Configurable column-name mapping (NFR30):**
   ```json
   // appsettings.json
   {
     "XlsxColumnMapping": {
       "FullName": ["Full Name", "Name", "Candidate Name"],
       "Email": ["Email", "Email Address", "E-mail"],
       "PhoneNumber": ["Phone", "Phone Number", "Tel"],
       "Location": ["Location", "City", "Office"],
       "DateApplied": ["Date Applied", "Application Date", "Applied"]
     }
   }
   ```

8. **StartImportCommand with multipart file upload:**
   ```csharp
   // api/src/Application/Features/Import/Commands/StartImport/StartImportCommand.cs
   public record StartImportCommand(
       Guid RecruitmentId,
       byte[] FileContent,
       string FileName,
       long FileSize) : IRequest<StartImportResponse>;

   public record StartImportResponse(
       Guid ImportSessionId,
       string StatusUrl);
   ```
   Note: The endpoint extracts file bytes from `IFormFile` and passes them as `byte[]` to the command. This keeps MediatR commands free of ASP.NET Core dependencies.

9. **Import endpoint pattern -- multipart/form-data:**
   ```csharp
   // api/src/Web/Endpoints/ImportEndpoints.cs
   public class ImportEndpoints : EndpointGroupBase
   {
       public override string? GroupName => "recruitments/{recruitmentId:guid}/import";

       public override void Map(RouteGroupBuilder group)
       {
           group.MapPost("/", StartImport)
               .DisableAntiforgery();   // Required for file upload
       }

       private static async Task<IResult> StartImport(
           ISender sender,
           Guid recruitmentId,
           IFormFile file)
       {
           using var ms = new MemoryStream();
           await file.CopyToAsync(ms);

           var result = await sender.Send(new StartImportCommand(
               recruitmentId,
               ms.ToArray(),
               file.FileName,
               file.Length));

           return Results.Accepted(
               result.StatusUrl,
               result);
       }
   }

   // api/src/Web/Endpoints/ImportSessionEndpoints.cs
   public class ImportSessionEndpoints : EndpointGroupBase
   {
       public override string? GroupName => "import-sessions";

       public override void Map(RouteGroupBuilder group)
       {
           group.MapGet("/{id:guid}", GetImportSession);
       }

       private static async Task<IResult> GetImportSession(
           ISender sender,
           Guid id)
       {
           var result = await sender.Send(new GetImportSessionQuery(id));
           return Results.Ok(result);
       }
   }
   ```

10. **Handler authorization pattern (MANDATORY):**
    ```csharp
    // StartImportCommandHandler MUST verify membership
    var recruitment = await _context.Recruitments
        .Include(r => r.Members)
        .Include(r => r.Steps)
        .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
        ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

    if (!recruitment.Members.Any(m => m.UserId == _tenantContext.UserGuid))
    {
        throw new ForbiddenAccessException();
    }

    if (recruitment.Status == RecruitmentStatus.Closed)
    {
        throw new RecruitmentClosedException(recruitment.Id);
    }
    ```

11. **Error handling:** Domain exceptions (`RecruitmentClosedException`, `InvalidWorkflowTransitionException`) are converted to Problem Details by global exception middleware. DO NOT catch domain exceptions in handlers.

12. **ITenantContext usage:** The `ImportPipelineHostedService` sets `ITenantContext.RecruitmentId` on the scoped context before accessing data, enabling EF Core global query filters for proper data isolation.

### Architecture Compliance

- **Aggregate root access only:** Create candidates via `Candidate.Create()`, update via `candidate.UpdateProfile()`. NEVER directly set properties. Create ImportSession via `ImportSession.Create()`.
- **Cross-aggregate references use IDs only:** `ImportSession.RecruitmentId` (Guid), `Candidate.RecruitmentId` (Guid). No navigation properties between aggregates.
- **One aggregate per transaction:** Each candidate create/update is a separate save within the import loop. ImportSession status updates are separate saves.
- **Ubiquitous language:** Use "Import Session" (not upload/sync/batch), "Candidate" (not applicant), "Recruitment" (not job/position), "Workflow Step" (not stage/phase).
- **Manual DTO mapping:** `ImportSessionDto.From()` static factory method. NO AutoMapper.
- **Problem Details for errors:** All error responses use RFC 9457 Problem Details. File validation errors include detail about what's wrong.
- **No PII in audit events/logs:** Log ImportSession IDs and counts, never candidate names or emails.
- **NSubstitute for ALL mocking** (never Moq).
- **MediatR v13+:** `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. Owned entities for ImportRowResult collection. |
| MediatR | 13.x | `IRequest<T>`, `IRequestHandler<,>`, pipeline behaviors for validation. |
| FluentValidation | Latest | `AbstractValidator<T>`, registered via DI. |
| ClosedXML | Latest (0.104+) | XLSX parsing library. MIT licensed. Add to Infrastructure.csproj. |
| System.Threading.Channels | Built-in (.NET 10) | `Channel<T>` for async producer/consumer. No NuGet needed. |
| NUnit | Latest | `[Test]`, `[TestCase]`, `[SetUp]`, `[TearDown]`. |
| NSubstitute | Latest | `Substitute.For<T>()` for all mocking. |
| FluentAssertions | Latest | `.Should().Be()`, `.Should().Throw<T>()`. |

### File Structure Requirements

**New files to create:**
```
api/src/Domain/
  ValueObjects/
    ImportRowResult.cs
    ParsedCandidateRow.cs
  Enums/
    ImportRowAction.cs

api/src/Application/
  Common/
    Interfaces/
      IXlsxParser.cs                     (if not already present)
      ICandidateMatchingEngine.cs         (if not already present)
    Models/
      ImportRequest.cs                    (Channel<T> message)
  Features/
    Import/
      Commands/
        StartImport/
          StartImportCommand.cs
          StartImportCommandValidator.cs
          StartImportCommandHandler.cs
          StartImportResponse.cs
      Queries/
        GetImportSession/
          GetImportSessionQuery.cs
          GetImportSessionQueryHandler.cs
          ImportSessionDto.cs

api/src/Infrastructure/
  Services/
    XlsxParserService.cs
    CandidateMatchingEngine.cs
    ImportPipelineHostedService.cs

api/src/Web/
  Endpoints/
    ImportEndpoints.cs
    ImportSessionEndpoints.cs

api/tests/Domain.UnitTests/
  Entities/
    ImportSessionTests.cs               (or extend existing)
  ValueObjects/
    ImportRowResultTests.cs

api/tests/Application.UnitTests/
  Features/
    Import/
      Commands/
        StartImportCommandTests.cs
        StartImportCommandValidatorTests.cs
      Queries/
        GetImportSessionQueryTests.cs

api/tests/Infrastructure.IntegrationTests/
  Services/
    XlsxParserServiceTests.cs
    CandidateMatchingEngineTests.cs

api/tests/Application.FunctionalTests/
  Endpoints/
    ImportEndpointTests.cs
```

**Existing files to modify:**
```
api/src/Domain/Entities/ImportSession.cs       -- Add SourceFileName, row results, updated MarkCompleted
api/src/Domain/Entities/Candidate.cs           -- Add UpdateProfile() method
api/src/Infrastructure/Data/Configurations/ImportSessionConfiguration.cs  -- Add new column/owned entity config
api/src/Infrastructure/DependencyInjection.cs  -- Register new services + Channel + HostedService
api/Directory.Packages.props                   -- Add ClosedXML package
api/src/Infrastructure/Infrastructure.csproj   -- Add ClosedXML package reference
api/src/Web/appsettings.json                   -- Add XlsxColumnMapping config section
api/src/Web/appsettings.Development.json       -- Add XlsxColumnMapping config (dev overrides if needed)
```

**Test fixture files:**
```
api/tests/Infrastructure.IntegrationTests/
  TestFixtures/
    valid-import.xlsx                  -- Valid Workday export with 5 columns
    missing-columns.xlsx               -- XLSX missing required columns
    empty.xlsx                         -- XLSX with headers but no data rows
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**

Domain unit tests:
- `ImportSession_Create_SetsStatusProcessing` -- factory method sets correct initial state
- `ImportSession_Create_RecordsSourceFileName` -- filename is preserved
- `ImportSession_MarkCompleted_UpdatesAllCounts` -- created/updated/errored/flagged counts tracked
- `ImportSession_MarkCompleted_WhenNotProcessing_ThrowsInvalidWorkflowTransitionException` -- cannot complete a failed session
- `ImportSession_MarkFailed_SetsReasonAndStatus` -- failure reason recorded
- `ImportSession_MarkFailed_TruncatesLongReason` -- reason capped at 2000 chars
- `ImportSession_AddRowResult_WhenProcessing_AddsResult` -- row results accumulate
- `ImportSession_AddRowResult_WhenCompleted_ThrowsInvalidWorkflowTransitionException`
- `Candidate_UpdateProfile_UpdatesAllProfileFields` -- name, phone, location, dateApplied updated
- `Candidate_UpdateProfile_DoesNotAffectOutcomes` -- outcomes collection unchanged
- `Candidate_UpdateProfile_DoesNotAffectDocuments` -- documents collection unchanged

Application unit tests (handlers):
- `StartImportCommand_ValidFile_CreatesSessionAndWritesToChannel`
- `StartImportCommand_RecruitmentNotFound_ThrowsNotFoundException`
- `StartImportCommand_UserNotMember_ThrowsForbiddenAccessException`
- `StartImportCommand_ClosedRecruitment_ThrowsRecruitmentClosedException`
- `StartImportCommandValidator_EmptyRecruitmentId_Fails`
- `StartImportCommandValidator_NullFile_Fails`
- `StartImportCommandValidator_InvalidExtension_Fails`
- `StartImportCommandValidator_OversizedFile_Fails` (>10 MB)
- `StartImportCommandValidator_ValidInput_Passes`
- `GetImportSessionQuery_ExistingSession_ReturnsDto`
- `GetImportSessionQuery_NotFound_ThrowsNotFoundException`

Infrastructure unit/integration tests:
- `XlsxParserService_ValidFile_ExtractsAllRows` -- parse real XLSX fixture
- `XlsxParserService_MissingRequiredColumns_ThrowsWithClearMessage`
- `XlsxParserService_EmptyFile_ReturnsEmptyList`
- `XlsxParserService_AlternateColumnNames_ResolvesViaMapping`
- `CandidateMatchingEngine_EmailMatch_ReturnsHighConfidence`
- `CandidateMatchingEngine_EmailMatch_CaseInsensitive` -- "John@example.com" matches "john@example.com"
- `CandidateMatchingEngine_NameAndPhoneMatch_ReturnsLowConfidence`
- `CandidateMatchingEngine_NoMatch_ReturnsNoneConfidence`
- `CandidateMatchingEngine_IdempotentReMatch_SameResult` -- same input, same output

Functional/integration tests (endpoints):
- `POST /api/recruitments/{id}/import` with valid XLSX -> 202 Accepted with session ID
- `POST /api/recruitments/{id}/import` with .csv file -> 400 Problem Details
- `POST /api/recruitments/{id}/import` with oversized file -> 400 Problem Details
- `POST /api/recruitments/{id}/import` on closed recruitment -> 400 Problem Details "Recruitment is closed"
- `GET /api/import-sessions/{id}` -> 200 with session data
- `GET /api/import-sessions/{nonexistent}` -> 404 Problem Details

Test naming convention: `MethodName_Scenario_ExpectedBehavior`

### Previous Story Intelligence (Epic 1+2 Learnings + Story 3.1 Dependency)

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- always use `apiGet`/`apiPost` from it
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers
- For file uploads, the httpClient currently sets `Content-Type: application/json` -- the import endpoint will need a separate `apiPostFormData()` helper or the endpoint handler must accept `IFormFile` directly

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- `ImportSession` entity already exists with basic structure -- this story extends it
- `Candidate` entity already exists with `Create()` factory -- this story adds `UpdateProfile()`
- `ApplicationDbContext` constructor requires `ITenantContext` -- mock it in tests
- MediatR 13: `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`

**From Story 1.4 (Shared UI Components):**
- 18 shadcn/ui components already installed
- `useAppToast()` hook for toast notifications

**From Story 2.1 (Create Recruitment):**
- CQRS folder structure established: one command per folder with Command, Validator, Handler
- Endpoint pattern established with `EndpointGroupBase` inheritance
- Response DTOs use `static From()` factory methods

**From Story 2.5 (Close Recruitment):**
- `RecruitmentClosedException` -> 400 Problem Details mapping already exists in global exception middleware
- `EnsureNotClosed()` guard on Recruitment aggregate -- import handler must check recruitment status

**Story 3.1 Dependency (Candidate aggregate):**
- Story 3.1 creates the `Candidate` aggregate with manual CRUD operations (CreateCandidate, RemoveCandidate)
- The Candidate entity (`api/src/Domain/Entities/Candidate.cs`) already exists from Story 1.3 with `Create()` factory
- Story 3.1 may add `CreateCandidateCommand`/`RemoveCandidateCommand` handlers and `CandidateEndpoints.cs`
- Story 3.2 MUST NOT re-create the Candidate entity -- only extend it with `UpdateProfile()`
- Story 3.2 creates candidates directly via `Candidate.Create()` in the pipeline (not through MediatR commands) because the import pipeline runs in a background service without HTTP context

**CRITICAL:** The import pipeline (BackgroundService) does NOT use MediatR commands to create candidates. It directly calls `Candidate.Create()` and `db.Candidates.Add()` because:
1. It runs outside the HTTP request pipeline (no `HttpContext`)
2. It processes many candidates in a loop (MediatR overhead per candidate is unnecessary)
3. It manages its own scoped `IServiceProvider` and `ITenantContext`

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(3.2): add ImportRowResult value object and ImportRowAction enum`
2. `feat(3.2): extend ImportSession aggregate with row results and source filename`
3. `feat(3.2): add Candidate.UpdateProfile() method + tests`
4. `feat(3.2): add IXlsxParser interface and ClosedXML implementation`
5. `feat(3.2): add ICandidateMatchingEngine implementation + tests`
6. `feat(3.2): add Channel<T> ImportRequest and ImportPipelineHostedService`
7. `feat(3.2): add StartImportCommand with validator and handler + tests`
8. `feat(3.2): add GetImportSessionQuery with handler and DTO + tests`
9. `feat(3.2): add ImportEndpoints and ImportSessionEndpoints + integration tests`
10. `feat(3.2): register services, add ClosedXML package, column mapping config`

### Latest Tech Information

- **.NET 10.0:** LTS until Nov 2028. `Channel<T>` and `BackgroundService` patterns stable. `IFormFile` multipart upload well-supported in Minimal APIs.
- **ClosedXML:** Latest stable (0.104+). MIT license. Pure .NET library for reading/writing XLSX files. No COM dependencies. Thread-safe for reading. Recommended over EPPlus (commercial license for > $5M revenue) and NPOI (less ergonomic API). Note: ClosedXML does NOT support .xls (legacy format) -- only .xlsx.
- **System.Threading.Channels:** Built into .NET runtime. `Channel.CreateUnbounded<T>()` with `SingleReader = true` option is the recommended pattern for single-consumer background processing. No NuGet package needed.
- **BackgroundService vs IHostedService:** `BackgroundService` is the preferred base class (inherits `IHostedService` with simpler API). Override `ExecuteAsync()` instead of implementing `StartAsync`/`StopAsync`.
- **EF Core 10:** Owned entities can be stored as JSON columns (`ToJson()`). Consider JSON column for `ImportRowResult` collection to avoid a separate table for row-level detail. Alternative: owned entity collection with separate table.
- **MediatR 13.x:** Pattern unchanged. The import pipeline does NOT use MediatR for candidate creation -- direct domain model usage in background service is correct.

### Project Structure Notes

- Alignment with unified project structure: all paths follow Clean Architecture (`api/`) + Vite React (`web/`) split
- This story is **backend-heavy** -- minimal frontend work (just the upload endpoint + polling). Frontend UI for import wizard and summary is in Story 3.3.
- `ImportEndpoints.cs` and `ImportSessionEndpoints.cs` are separate endpoint classes following the established `EndpointGroupBase` pattern
- Infrastructure services (`XlsxParserService`, `CandidateMatchingEngine`, `ImportPipelineHostedService`) all live in `api/src/Infrastructure/Services/`
- Test fixtures (XLSX files) should be embedded resources or placed in a `TestFixtures/` folder within the integration test project

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-3-candidate-import-document-management.md` -- Story 3.2 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries (ImportSession as separate root), ITenantContext, Channel<T> decision, async import processing]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, handler authorization, error handling, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- 202 Accepted pattern, polling endpoint, Problem Details, multipart upload]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, ImportPipelineHostedService path, XlsxParserService path]
- [Source: `_bmad-output/planning-artifacts/architecture/infrastructure.md` -- Background processing patterns, Channel<T> + IHostedService]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Test frameworks, naming, Testcontainers, mandatory security scenarios]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR14-FR22, FR25, NFR6, NFR30]
- [Source: `api/src/Domain/Entities/ImportSession.cs` -- Existing aggregate with Processing/Completed/Failed transitions]
- [Source: `api/src/Domain/Entities/Candidate.cs` -- Existing aggregate with Create(), Outcomes, Documents]
- [Source: `api/src/Domain/ValueObjects/CandidateMatch.cs` -- Existing value object (Confidence + MatchMethod)]
- [Source: `api/src/Domain/Enums/ImportMatchConfidence.cs` -- High, Low, None]
- [Source: `api/src/Domain/Enums/ImportSessionStatus.cs` -- Processing, Completed, Failed]
- [Source: `api/src/Application/Common/Interfaces/ITenantContext.cs` -- RecruitmentId setter for import scoping]
- [Source: `api/src/Application/Common/Interfaces/IApplicationDbContext.cs` -- DbSet<ImportSession>, DbSet<Candidate>]
- [Source: `api/src/Web/Endpoints/RecruitmentEndpoints.cs` -- EndpointGroupBase pattern reference]
- [Source: `web/src/lib/api/httpClient.ts` -- HTTP client with auth headers]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

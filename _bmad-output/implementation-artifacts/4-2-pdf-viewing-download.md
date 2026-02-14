# Story 4.2: PDF Viewing & Download

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **user (Lina)**,
I want to **view a candidate's CV inline within the application and download it when needed**,
so that **I can review CVs without downloading files and switching between applications**.

## Acceptance Criteria

### AC1: Inline PDF rendering via SAS URL
**Given** a candidate has a linked PDF document
**When** the user selects that candidate
**Then** the PDF renders inline in the viewer panel using react-pdf with the SAS-authenticated URL
**And** the PDF loads within 2 seconds (NFR4)

### AC2: Multi-page scrolling
**Given** the PDF is displayed in the viewer
**When** the user scrolls
**Then** all pages of the document are accessible via react-pdf's per-page lazy loading on scroll

### AC3: PDF download
**Given** a candidate has a linked PDF document
**When** the user clicks the "Download" action
**Then** the PDF is downloaded to the user's device via the SAS URL

### AC4: No document empty state
**Given** a candidate does not have a linked document
**When** the user selects that candidate
**Then** the viewer panel shows an empty state: "No CV available" with an "Upload CV" action (if recruitment is active)

### AC5: Candidate switching replaces PDF
**Given** the user is viewing candidate A's PDF and selects candidate B
**When** candidate B loads
**Then** candidate B's PDF replaces candidate A's in the viewer
**And** if candidate B's SAS URL was pre-fetched, the PDF loads from the pre-fetched URL

### AC6: SAS URL pre-fetching
**Given** the user is reviewing candidate N in the candidate list
**When** the viewer is active
**Then** the system pre-fetches SAS URLs for the next 2-3 candidates' PDFs in the background
**And** pre-fetched PDFs load faster when those candidates are selected

### AC7: SAS token expiry refresh
**Given** a SAS token has expired (>15 minutes)
**When** the user attempts to view or download a PDF
**Then** a fresh SAS URL is requested from the API transparently
**And** the PDF loads without user intervention

### Prerequisites
- **Story 3.1** (Manual Candidate Management) -- Candidate aggregate CRUD, CandidateEndpoints, CandidateList.tsx, CandidateDetail.tsx
- **Story 3.4** (PDF Bundle Upload & Splitting) -- BlobStorageService with SAS token generation, CandidateDocument entity
- **Story 3.5** (CV Auto-Match, Manual Assignment & Individual Upload) -- DocumentUpload component, `candidateApi.uploadDocument()`, document attachment domain methods
- **Story 4.1** (Candidate List & Search/Filter) -- `GetCandidateByIdQuery` with `CandidateDetailDto` (includes SAS URL via `IBlobStorageService.GenerateSasUri()`), `candidateApi.getById()`, paginated candidate list with search/filter

### FRs Fulfilled
- **FR34:** Users can view a candidate's individual PDF within the application
- **FR35:** Users can download a candidate's PDF document

## Tasks / Subtasks

- [ ] Task 1: _Resolved -- GetCandidateByIdQuery is created by Story 4.1 (Task 3)_
  - Story 4.1 creates `GetCandidateByIdQuery` with `CandidateDetailDto` including documents with SAS URLs, outcome history, and all candidate fields. This story consumes that query and its frontend `candidateApi.getById()` method.
  - If Story 4.2 is implemented before Story 4.1, create a minimal `GetCandidateByIdQuery` with SAS URL support, and Story 4.1 will extend it with outcome history fields.

- [ ] Task 2: Backend -- Extend GetCandidatesQuery with batch SAS URLs (AC: #6)
  - [ ] 2.1 Modify `GetCandidatesQueryHandler` to call `IBlobStorageService.GenerateSasUri()` for each candidate that has a document
  - [ ] 2.2 Extend `CandidateDto` with `SasUrl` property (nullable string). Update `From()` factory to accept optional `Uri?` parameter
  - [ ] 2.3 Update `PaginatedCandidateListDto` -- no structural changes needed, flows through `CandidateDto`
  - [ ] 2.4 Unit test: candidates with documents have SAS URLs populated
  - [ ] 2.5 Unit test: candidates without documents have null SAS URLs

- [ ] Task 3: Backend -- Refresh SAS URL endpoint (AC: #7)
  - [ ] 3.1 Add `GET /api/recruitments/{recruitmentId}/candidates/{candidateId}/document/sas` endpoint to `CandidateEndpoints.cs`
  - [ ] 3.2 Create `GetDocumentSasQuery` in `api/src/Application/Features/Candidates/Queries/GetDocumentSas/`
  - [ ] 3.3 Create `GetDocumentSasQueryHandler`:
    - Load recruitment with members, verify user is member
    - Load candidate with documents, verify belongs to recruitment
    - If no document, throw `NotFoundException`
    - Generate fresh SAS URI via `IBlobStorageService.GenerateSasUri()` with 15-minute validity
    - Return `DocumentSasDto` with `sasUrl` and `expiresAt`
  - [ ] 3.4 Create `DocumentSasDto` record with `SasUrl` (string) and `ExpiresAt` (DateTimeOffset)
  - [ ] 3.5 Unit test: returns fresh SAS URL for candidate with document
  - [ ] 3.6 Unit test: throws NotFoundException when candidate has no document
  - [ ] 3.7 Unit test: non-member user throws `ForbiddenAccessException`
  - [ ] 3.8 Integration test: GET endpoint returns 200 with SAS URL
  - [ ] 3.9 Integration test: GET endpoint returns 404 when no document

- [ ] Task 4: Frontend -- API client extensions (AC: #1, #7)
  - [ ] 4.1 _Resolved -- `CandidateDetailResponse` and `candidateApi.getById()` are created by Story 4.1 (Task 5)._ Verify SAS URL fields are present; extend if needed.
  - [ ] 4.2 Add `DocumentSasResponse` type with `sasUrl: string` and `expiresAt: string` fields to `web/src/lib/api/candidates.types.ts`
  - [ ] 4.3 Add `candidateApi.refreshDocumentSas()` method for SAS refresh endpoint to `web/src/lib/api/candidates.ts`
  - [ ] 4.4 Update existing `CandidateResponse` type to include `sasUrl: string | null`

- [ ] Task 5: Frontend -- PdfViewer component (AC: #1, #2, #3, #4, #5)
  - [ ] 5.1 Install `react-pdf` package: `npm install react-pdf`
  - [ ] 5.2 Create `web/src/features/screening/PdfViewer.tsx` component with props: `sasUrl: string | null`, `candidateName: string`, `isRecruitmentActive: boolean`, `onUploadClick?: () => void`
  - [ ] 5.3 Implement PDF rendering using `react-pdf`'s `Document` and `Page` components with the SAS URL
  - [ ] 5.4 Enable text layer for screen reader accessibility (WCAG 2.1 AA)
  - [ ] 5.5 Implement per-page lazy loading via `react-pdf`'s `Page` component rendered on scroll (only render visible pages + 1 page buffer)
  - [ ] 5.6 Add "Download" button that triggers browser download via the SAS URL (`<a href={sasUrl} download>`)
  - [ ] 5.7 Show loading state using `SkeletonLoader` while PDF loads (not a full-page spinner)
  - [ ] 5.8 Show error state if PDF fails to load (e.g., corrupted file, network error) with retry action
  - [ ] 5.9 When `sasUrl` is null, render `EmptyState` with "No CV available" message and conditional "Upload CV" action (shown only when `isRecruitmentActive` is true)
  - [ ] 5.10 When `sasUrl` changes (candidate switch), unmount previous PDF and render new one
  - [ ] 5.11 Unit test: renders PDF document when sasUrl is provided
  - [ ] 5.12 Unit test: shows empty state when sasUrl is null
  - [ ] 5.13 Unit test: shows "Upload CV" action in empty state when recruitment is active
  - [ ] 5.14 Unit test: hides "Upload CV" action in empty state when recruitment is closed
  - [ ] 5.15 Unit test: shows download button when sasUrl is provided
  - [ ] 5.16 Unit test: shows loading skeleton while PDF is loading

- [ ] Task 6: Frontend -- usePdfPrefetch hook (AC: #5, #6, #7)
  - [ ] 6.1 Create `web/src/features/screening/hooks/usePdfPrefetch.ts`
  - [ ] 6.2 Hook accepts: `candidates: CandidateResponse[]` (current page), `currentIndex: number` (selected candidate index)
  - [ ] 6.3 Pre-fetch logic: when `currentIndex` changes, identify next 2-3 candidates with documents. If their SAS URLs are already available from the batch response, no additional fetch needed. If URLs have expired (>12 minutes old -- refresh before the 15-minute expiry), call `candidateApi.refreshDocumentSas()` to get fresh URLs
  - [ ] 6.4 Store pre-fetched SAS URLs in a `Map<string, { sasUrl: string; fetchedAt: number }>` keyed by candidate ID
  - [ ] 6.5 Expose `getSasUrl(candidateId: string): string | null` that returns the freshest available SAS URL (pre-fetched or from batch response)
  - [ ] 6.6 Expose `isExpired(candidateId: string): boolean` utility for consumers
  - [ ] 6.7 Use TanStack Query's `queryClient.prefetchQuery()` for coordinating pre-fetch requests to avoid duplicate fetches
  - [ ] 6.8 Unit test: pre-fetches SAS URLs for next 2 candidates when current index changes
  - [ ] 6.9 Unit test: returns cached SAS URL when available and not expired
  - [ ] 6.10 Unit test: refreshes expired SAS URL transparently
  - [ ] 6.11 Unit test: does not pre-fetch for candidates without documents

- [ ] Task 7: Frontend -- react-pdf worker configuration (AC: #1)
  - [ ] 7.1 Configure PDF.js worker in Vite: set `pdfjs.GlobalWorkerOptions.workerSrc` to use the `pdfjs-dist` worker bundle
  - [ ] 7.2 Add worker configuration in app initialization (e.g., in `main.tsx` or a dedicated `pdfConfig.ts`)
  - [ ] 7.3 Verify worker loads correctly in Vite dev server and production build

- [ ] Task 8: Frontend -- MSW handlers and fixtures (AC: #1, #7)
  - [ ] 8.1 Add MSW handler for `GET /api/recruitments/:recruitmentId/candidates/:candidateId` returning candidate with SAS URL
  - [ ] 8.2 Add MSW handler for `GET /api/recruitments/:recruitmentId/candidates/:candidateId/document/sas` returning fresh SAS URL
  - [ ] 8.3 Add candidate detail fixture with document SAS URL in `web/src/mocks/fixtures/candidates.ts`
  - [ ] 8.4 Update existing candidate list MSW handler to include `sasUrl` field in responses

## Dev Notes

### Affected Aggregate(s)

**Candidate** (read-only in this story) -- The `Candidate` entity at `api/src/Domain/Entities/Candidate.cs` owns `CandidateDocument` as a child entity. This story reads documents to generate SAS URLs. No domain state is modified.

**Recruitment** (read-only) -- Loaded to verify membership and active/closed status. Not modified.

No domain entities are modified in this story. All changes are query/read-path additions and frontend components.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (GetCandidateByIdQuery) | **Test-first** | Query handler with security checks and SAS URL generation |
| Task 2 (Extend GetCandidatesQuery) | **Test-first** | Modifying existing query -- must prove SAS URLs are populated |
| Task 3 (Refresh SAS endpoint) | **Test-first** | New API endpoint with security checks |
| Task 4 (API client extensions) | **Characterization** | Thin wrapper -- test via component integration tests |
| Task 5 (PdfViewer component) | **Test-first** | User-facing component with loading, empty, error states |
| Task 6 (usePdfPrefetch hook) | **Test-first** | Non-trivial pre-fetch logic with expiry tracking |
| Task 7 (react-pdf worker config) | **Spike** | Library-specific configuration -- verify manually, test via component tests |
| Task 8 (MSW handlers) | **Characterization** | Test infrastructure supporting other tests |

### Technical Requirements

**Backend -- GetCandidateByIdQuery:** _Created by Story 4.1 (Task 3)._ See `4-1-candidate-list-search-filter.md` for the full `GetCandidateByIdQuery`, `CandidateDetailDto`, and handler implementation. Story 4.1's version includes SAS URLs for documents, outcome history, and all candidate fields. This story's `PdfViewer` component consumes the `sasUrl` field from that DTO.

**Backend -- Extend CandidateDto with SAS URL:**

```csharp
// Modify api/src/Application/Features/Candidates/CandidateDto.cs
public record CandidateDto
{
    // ... existing properties ...
    public string? SasUrl { get; init; }

    public static CandidateDto From(Candidate candidate, Uri? sasUri = null) => new()
    {
        // ... existing mappings ...
        SasUrl = sasUri?.ToString(),
    };
}
```

**Backend -- GetCandidatesQueryHandler SAS URL generation:**

```csharp
// Modify the handler to generate batch SAS URLs
var items = await query
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);

var candidateDtos = items.Select(c =>
{
    Uri? sasUri = null;
    var doc = c.Documents.FirstOrDefault();
    if (doc is not null)
    {
        var blobUri = new Uri(doc.BlobStorageUrl);
        var segments = blobUri.AbsolutePath.TrimStart('/').Split('/', 2);
        sasUri = blobStorageService.GenerateSasUri(segments[0], segments[1], SasValidity);
    }
    return CandidateDto.From(c, sasUri);
}).ToList();
```

**Backend -- DocumentSasDto:**

```csharp
// api/src/Application/Features/Candidates/Queries/GetDocumentSas/DocumentSasDto.cs
public record DocumentSasDto
{
    public string SasUrl { get; init; } = null!;
    public DateTimeOffset ExpiresAt { get; init; }

    public static DocumentSasDto From(Uri sasUri, TimeSpan validity) => new()
    {
        SasUrl = sasUri.ToString(),
        ExpiresAt = DateTimeOffset.UtcNow.Add(validity),
    };
}
```

**Backend -- Handler authorization pattern (mandatory):**

```csharp
// All query handlers in this story MUST:
var recruitment = await dbContext.Recruitments
    .Include(r => r.Members)
    .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct)
    ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

// MANDATORY: Verify current user is a member
var userId = tenantContext.UserGuid;
if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
    throw new ForbiddenAccessException();
```

**Frontend -- PdfViewer component:**

```typescript
// web/src/features/screening/PdfViewer.tsx
import { useState } from 'react';
import { Document, Page, pdfjs } from 'react-pdf';
import 'react-pdf/dist/Page/TextLayer.css';
import 'react-pdf/dist/Page/AnnotationLayer.css';
import { Button } from '@/components/ui/button';
import { EmptyState } from '@/components/EmptyState';
import { SkeletonLoader } from '@/components/SkeletonLoader';

interface PdfViewerProps {
  sasUrl: string | null;
  candidateName: string;
  isRecruitmentActive: boolean;
  onUploadClick?: () => void;
}

export function PdfViewer({
  sasUrl,
  candidateName,
  isRecruitmentActive,
  onUploadClick,
}: PdfViewerProps) {
  const [numPages, setNumPages] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  if (!sasUrl) {
    return (
      <EmptyState
        title="No CV available"
        description={`${candidateName} does not have a CV document linked.`}
        action={
          isRecruitmentActive && onUploadClick
            ? { label: 'Upload CV', onClick: onUploadClick }
            : undefined
        }
      />
    );
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-end gap-2 p-2 border-b">
        <a
          href={sasUrl}
          download
          className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline"
        >
          Download
        </a>
      </div>
      <div className="flex-1 overflow-auto">
        {isLoading && <SkeletonLoader variant="card" />}
        <Document
          file={sasUrl}
          onLoadSuccess={({ numPages }) => {
            setNumPages(numPages);
            setIsLoading(false);
            setError(null);
          }}
          onLoadError={(err) => {
            setError(err);
            setIsLoading(false);
          }}
          loading={null}
        >
          {numPages &&
            Array.from({ length: numPages }, (_, i) => (
              <Page key={i + 1} pageNumber={i + 1} renderTextLayer renderAnnotationLayer />
            ))}
        </Document>
        {error && (
          <div className="p-4 text-center">
            <p className="text-red-600 mb-2">Failed to load PDF</p>
            <Button variant="outline" onClick={() => setError(null)}>
              Retry
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
```

**Frontend -- usePdfPrefetch hook:**

```typescript
// web/src/features/screening/hooks/usePdfPrefetch.ts
import { useCallback, useRef, useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { candidateApi } from '@/lib/api/candidates';
import type { CandidateResponse } from '@/lib/api/candidates.types';

const SAS_TTL_MS = 12 * 60 * 1000; // Refresh 3 minutes before 15-minute expiry
const PREFETCH_AHEAD = 3;

interface CachedSas {
  sasUrl: string;
  fetchedAt: number;
}

export function usePdfPrefetch(
  recruitmentId: string,
  candidates: CandidateResponse[],
  currentIndex: number,
) {
  const cache = useRef(new Map<string, CachedSas>());
  const queryClient = useQueryClient();

  // Seed cache from batch response SAS URLs
  useEffect(() => {
    for (const candidate of candidates) {
      if (candidate.sasUrl && !cache.current.has(candidate.id)) {
        cache.current.set(candidate.id, {
          sasUrl: candidate.sasUrl,
          fetchedAt: Date.now(),
        });
      }
    }
  }, [candidates]);

  // Pre-fetch next N candidates when index changes
  useEffect(() => {
    const upcoming = candidates
      .slice(currentIndex + 1, currentIndex + 1 + PREFETCH_AHEAD)
      .filter((c) => c.document != null);

    for (const candidate of upcoming) {
      const cached = cache.current.get(candidate.id);
      const isExpired = cached && Date.now() - cached.fetchedAt > SAS_TTL_MS;

      if (!cached || isExpired) {
        queryClient.prefetchQuery({
          queryKey: ['candidate-sas', recruitmentId, candidate.id],
          queryFn: async () => {
            const result = await candidateApi.refreshDocumentSas(
              recruitmentId,
              candidate.id,
            );
            cache.current.set(candidate.id, {
              sasUrl: result.sasUrl,
              fetchedAt: Date.now(),
            });
            return result;
          },
          staleTime: SAS_TTL_MS,
        });
      }
    }
  }, [currentIndex, candidates, recruitmentId, queryClient]);

  const getSasUrl = useCallback(
    (candidateId: string): string | null => {
      const cached = cache.current.get(candidateId);
      if (!cached) return null;
      if (Date.now() - cached.fetchedAt > SAS_TTL_MS) return null;
      return cached.sasUrl;
    },
    [],
  );

  const isExpired = useCallback(
    (candidateId: string): boolean => {
      const cached = cache.current.get(candidateId);
      if (!cached) return true;
      return Date.now() - cached.fetchedAt > SAS_TTL_MS;
    },
    [],
  );

  return { getSasUrl, isExpired };
}
```

**Frontend -- API types additions:**

```typescript
// CandidateDetailResponse is created by Story 4.1 (Task 5.2) with full fields.
// This story adds DocumentSasResponse and ensures CandidateResponse has sasUrl:

// Add to web/src/lib/api/candidates.types.ts
export interface DocumentSasResponse {
  sasUrl: string;
  expiresAt: string;
}

// Extend CandidateResponse (already created by Story 4.1) with:
//   sasUrl: string | null;
```

**Frontend -- API client additions:**

```typescript
// candidateApi.getById() is created by Story 4.1 (Task 5.5).
// This story adds the SAS refresh method:

// Add to web/src/lib/api/candidates.ts
  refreshDocumentSas: (recruitmentId: string, candidateId: string) =>
    apiGet<DocumentSasResponse>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/document/sas`,
    ),
```

**Frontend -- react-pdf worker configuration:**

```typescript
// web/src/lib/pdfConfig.ts
import { pdfjs } from 'react-pdf';

// Configure PDF.js worker for Vite
pdfjs.GlobalWorkerOptions.workerSrc = new URL(
  'pdfjs-dist/build/pdf.worker.min.mjs',
  import.meta.url,
).toString();
```

```typescript
// Import in main.tsx (before App renders)
import './lib/pdfConfig';
```

### Architecture Compliance

- **Read-only aggregate access:** This story only reads Candidate and Recruitment aggregates. No domain state is modified.
- **SAS tokens for document access (NFR15):** All PDF URLs are short-lived SAS tokens (15-minute validity). Raw blob URLs are never exposed to the frontend.
- **Decision #6 compliance:** Batch SAS URLs embedded in GetCandidates response. GetCandidateById includes a single SAS URL. Separate refresh endpoint for expired tokens.
- **Ubiquitous language:** "Candidate" (not applicant), "Recruitment" (not job/position), "Screening" (not review).
- **No PII in logs:** SAS URL generation logs blob identifiers (GUIDs), not candidate names.
- **Manual DTO mapping:** `CandidateDetailDto.From()` and `DocumentSasDto.From()` static factory methods. No AutoMapper.
- **NSubstitute for ALL mocking** (never Moq).
- **ITenantContext:** All query handlers verify membership via `ITenantContext.UserGuid`.
- **httpClient.ts as single HTTP entry point:** All new API methods use `apiGet` from httpClient.ts. Frontend never calls `fetch` directly.
- **Shared components:** Uses `EmptyState` and `SkeletonLoader` from `components/`. No feature-local equivalents.
- **WCAG 2.1 AA:** react-pdf text layer enabled for screen reader accessibility. Focus management follows existing patterns.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | Primary constructors for DI. |
| EF Core | 10.x | `Include()` for loading documents with candidate. `AsNoTracking()` for queries. |
| MediatR | 13.x | `IRequest<T>` for queries returning DTOs. |
| Azure.Storage.Blobs | Latest | `BlobClient.GenerateSasUri()` for SAS token generation. 15-minute validity. |
| react-pdf | 9.x | PDF.js wrapper for inline rendering. Text layer for accessibility. Per-page lazy loading. |
| pdfjs-dist | 4.x | PDF.js core (peer dependency of react-pdf). Worker must be configured for Vite. |
| React | 19.x | State management for PDF loading, error, page count. |
| TypeScript | 5.7.x | Strict mode. |
| TanStack Query | 5.x | `prefetchQuery()` for SAS URL pre-fetching. `useQuery` for candidate detail. |
| Tailwind CSS | 4.x | CSS-first config. |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Candidates/
  Queries/
    GetDocumentSas/
      GetDocumentSasQuery.cs
      GetDocumentSasQueryHandler.cs
      DocumentSasDto.cs

api/tests/Application.UnitTests/Features/Candidates/
  Queries/
    GetDocumentSas/
      GetDocumentSasQueryHandlerTests.cs

web/src/features/screening/
  PdfViewer.tsx
  PdfViewer.test.tsx
  hooks/
    usePdfPrefetch.ts
    usePdfPrefetch.test.ts

web/src/lib/
  pdfConfig.ts
```

**Existing files to modify:**
```
api/src/Application/Features/Candidates/CandidateDto.cs              -- Add SasUrl property, update From() factory
api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandler.cs  -- Add SAS URL generation for batch responses
api/src/Web/Endpoints/CandidateEndpoints.cs                          -- Add GET /{candidateId} and GET /{candidateId}/document/sas endpoints

web/src/lib/api/candidates.ts                                        -- Add getById, refreshDocumentSas methods
web/src/lib/api/candidates.types.ts                                  -- Add CandidateDetailResponse, DocumentSasResponse types, add sasUrl to CandidateResponse
web/src/main.tsx                                                      -- Import pdfConfig.ts for worker setup
web/src/mocks/candidateHandlers.ts                                   -- Add candidate detail and SAS refresh handlers
web/src/mocks/fixtures/candidates.ts                                 -- Add candidate detail fixtures with SAS URLs
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**

GetCandidateByIdQuery handler: _Tests owned by Story 4.1 (Task 3)._ Story 4.2 verifies that SAS URLs are present in integration tests.

GetCandidatesQuery handler (extended with batch SAS URLs):
- `Handle_CandidatesWithDocuments_IncludesSasUrls`
- `Handle_CandidatesWithoutDocuments_NullSasUrls`

GetDocumentSas query handler:
- `Handle_CandidateWithDocument_ReturnsFreshSasUrl`
- `Handle_CandidateWithoutDocument_ThrowsNotFoundException`
- `Handle_NonMemberUser_ThrowsForbiddenAccessException`
- `Handle_RecruitmentNotFound_ThrowsNotFoundException`

Integration tests (API endpoints):
- `GetCandidateById_ValidRequest_Returns200WithSasUrl`
- `GetCandidateById_NonMember_Returns403`
- `GetDocumentSas_ValidRequest_Returns200WithSasUrl`
- `GetDocumentSas_NoDocument_Returns404`

**Frontend tests (Vitest + Testing Library + MSW):**

PdfViewer:
- "should render PDF document when sasUrl is provided"
- "should show empty state when sasUrl is null"
- "should show 'Upload CV' action in empty state when recruitment is active"
- "should hide 'Upload CV' action when recruitment is closed"
- "should show download button when sasUrl is provided"
- "should show loading skeleton while PDF is loading"
- "should show error state when PDF fails to load"

usePdfPrefetch:
- "should pre-fetch SAS URLs for next candidates when index changes"
- "should return cached SAS URL when available and not expired"
- "should return null for expired SAS URLs"
- "should not pre-fetch for candidates without documents"
- "should seed cache from batch response SAS URLs"

MSW handlers:
- `GET /api/recruitments/:id/candidates/:candidateId` -- returns candidate with SAS URL
- `GET /api/recruitments/:id/candidates/:candidateId/document/sas` -- returns fresh SAS URL

### Previous Story Intelligence

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- use `apiGet` for JSON queries. Frontend never calls `fetch` directly.
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers.

**From Story 1.3 (Core Data Model):**
- `CandidateDocument` has `BlobStorageUrl` property -- this is the raw URL. SAS URLs are generated at query time via `IBlobStorageService.GenerateSasUri()`.
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection.

**From Story 1.4 (Shared UI Components):**
- `EmptyState` component exists at `web/src/components/EmptyState.tsx` -- use for "No CV available" state.
- `SkeletonLoader` component exists at `web/src/components/SkeletonLoader.tsx` -- use for PDF loading state.
- `useAppToast()` hook available for toast notifications.

**From Story 2.1 (Create Recruitment):**
- CQRS folder structure established: one query per folder with Query, Handler, DTO.
- Frontend API client pattern established in `web/src/lib/api/`.
- TanStack Query hook pattern established.

**From Story 2.5 (Close Recruitment):**
- `isClosed` derived from `recruitment.status === 'Closed'` -- same pattern used to hide "Upload CV" in empty state.
- `isRecruitmentActive` prop pattern used in Story 3.5 DocumentUpload component.

**From Story 3.1 (Manual Candidate Management):**
- `CandidateEndpoints.cs` created with candidate CRUD routes -- add detail and SAS refresh endpoints here.
- `candidates.ts` and `candidates.types.ts` API client created -- extend with new methods.
- `GetCandidatesQuery` exists -- extend to include SAS URLs.

**From Story 3.4 (PDF Bundle Upload & Splitting):**
- `BlobStorageService` has `GenerateSasUri()` method with `TimeSpan validity` parameter.
- SAS tokens use `BlobSasPermissions.Read` for read-only access.

**From Story 3.5 (CV Auto-Match & Upload):**
- `DocumentDto` exists at `api/src/Application/Features/Candidates/Commands/DocumentDto.cs`.
- `CandidateDto` exists with `Document` property.
- `CandidateDetail.tsx` exists at `web/src/features/candidates/CandidateDetail.tsx`.
- `DocumentUpload.tsx` exists at `web/src/features/candidates/DocumentUpload.tsx` -- can be reused from PdfViewer empty state.

**From Story 4.1 (Candidate List & Search/Filter) -- parallel dependency:**
- GetCandidatesQuery extended with search/filter params and batch SAS URLs.
- `CandidateList.tsx` enhanced with search, filter, and pagination.
- react-virtuoso installed for list virtualization.

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(4.2): add GetCandidateByIdQuery with SAS URL generation + tests`
2. `feat(4.2): extend GetCandidatesQuery with batch SAS URLs + tests`
3. `feat(4.2): add GetDocumentSasQuery for SAS refresh + tests`
4. `feat(4.2): add candidate detail and SAS refresh API endpoints`
5. `feat(4.2): add candidates API client extensions (getById, refreshDocumentSas)`
6. `feat(4.2): configure react-pdf with PDF.js worker for Vite`
7. `feat(4.2): add PdfViewer component with inline rendering + tests`
8. `feat(4.2): add usePdfPrefetch hook for SAS URL pre-fetching + tests`
9. `feat(4.2): add MSW handlers for candidate detail and SAS refresh`

### Latest Tech Information

- **react-pdf 9.x:** Uses PDF.js 4.x under the hood. Worker must be configured separately for Vite bundlers. Text layer CSS must be imported explicitly (`react-pdf/dist/Page/TextLayer.css`). Per-page rendering with `<Page>` component.
- **PDF.js worker in Vite:** Use `new URL('pdfjs-dist/build/pdf.worker.min.mjs', import.meta.url)` for Vite worker loading. This avoids CDN dependency and bundles the worker locally.
- **TanStack Query 5.x prefetchQuery:** Returns a Promise, does not throw on error. Uses `queryClient.prefetchQuery({ queryKey, queryFn, staleTime })`. Ideal for background pre-fetching without UI state updates.
- **SAS token security:** Azure Blob SAS tokens include permissions, expiry, and are signed server-side. The token is part of the URL query string. No additional auth headers needed for SAS-authenticated requests.

### Project Structure Notes

- `PdfViewer.tsx` lives in `features/screening/` because it is the primary screening tool -- not in `features/candidates/`. It is consumed by the screening layout (Story 4.4) and candidate detail view.
- `usePdfPrefetch.ts` lives in `features/screening/hooks/` alongside `useScreeningSession.ts` and `useKeyboardNavigation.ts` (Stories 4.4 and 4.5).
- `pdfConfig.ts` lives in `lib/` as it is application-wide configuration, not feature-specific.
- Document-related API methods live in `candidates.ts` because documents are candidate-scoped (architecture: "No standalone document endpoints").
- The SAS refresh endpoint is a GET (not POST) because it is idempotent and read-only -- generating a new SAS token does not mutate state.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-screening-outcome-recording.md` -- Story 4.2 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Decision #6 (batch SAS URLs), aggregate boundaries, ITenantContext]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- CQRS structure, handler authorization, DTO mapping, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- Component structure, loading/empty states, shared components]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Endpoint registration, response formats]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- File locations, screening feature folder, document access boundary]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- PDF viewing with react-pdf, batch screening architecture]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Test frameworks, naming conventions]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR34, FR35, NFR4, NFR15]
- [Source: `api/src/Application/Common/Interfaces/IBlobStorageService.cs` -- GenerateSasUri() method signature]
- [Source: `api/src/Infrastructure/Services/BlobStorageService.cs` -- SAS token generation implementation]
- [Source: `api/src/Application/Features/Candidates/CandidateDto.cs` -- Existing DTO to extend with SasUrl]
- [Source: `api/src/Application/Features/Candidates/Queries/GetCandidates/GetCandidatesQueryHandler.cs` -- Existing handler to extend]
- [Source: `web/src/lib/api/candidates.ts` -- Existing API client to extend]
- [Source: `web/src/lib/api/candidates.types.ts` -- Existing types to extend]

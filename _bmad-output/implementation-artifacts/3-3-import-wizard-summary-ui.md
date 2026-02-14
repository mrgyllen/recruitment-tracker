# Story 3.3: Import Wizard & Summary UI

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **recruiting leader (Erik)**,
I want a **guided import wizard that shows upload progress, a clear summary of results, and lets me review low-confidence matches**,
so that **I can confidently import candidates and resolve any matching issues without guessing what happened**.

## Acceptance Criteria

### AC1: Import wizard entry point
**Given** the user is viewing an active recruitment
**When** they click "Import Candidates"
**Then** a Sheet component slides in from the right, full height, containing the import wizard

### AC2: File upload step (Step 1)
**Given** the import wizard is open on Step 1 (Upload)
**When** the user views the upload step
**Then** a file upload area accepts XLSX files (MVP scope -- PDF bundle handled in Story 3.4)
**And** contextual Workday export instructions are visible (what to select, which exports to run, the "always export all candidates" rule)
**And** each file type has its size limit displayed (XLSX: 10 MB)

### AC3: Processing state with polling
**Given** the user has selected a valid file
**When** they click "Start Import"
**Then** the wizard transitions to a processing state with a progress indicator
**And** the UI displays "Importing candidates..."
**And** the frontend polls `GET /api/import-sessions/{id}` every 2 seconds for progress updates

### AC4: Import summary on success
**Given** the import processing completes successfully
**When** the wizard receives a "Completed" status
**Then** the wizard transitions to the import summary view
**And** the summary shows: total candidates processed, created count, updated count, errored count
**And** row-level detail is accessible for each category (expandable)

### AC5: Error detail display
**Given** the import summary shows errors
**When** the user views the error details
**Then** each errored row shows the row number, candidate name (if available), and the specific error message
**And** the errors are presented clearly but do not block the rest of the import results

### AC6: Low-confidence match review
**Given** low-confidence matches were flagged during import
**When** the summary is displayed
**Then** flagged matches are shown with an amber indicator: "N matches by name+phone only -- review recommended"
**And** each flagged match shows: the imported name/phone, the matched existing candidate, and the match method
**And** the user can confirm or reject each match

### AC7: Match confirmation
**Given** the user confirms a low-confidence match
**When** they click "Confirm Match"
**Then** the candidate's profile fields are updated from the import data
**And** the match status updates to confirmed

### AC8: Match rejection
**Given** the user rejects a low-confidence match
**When** they click "Reject"
**Then** a new candidate is created from the import data instead
**And** the rejected match is recorded in the import session

### AC9: Count discrepancy notice
**Given** there is a count discrepancy between imported candidates and split CVs
**When** the summary is displayed
**Then** the discrepancy is reported as an informational notice (not an error)
**And** the notice uses amber/info styling, not red error styling

### AC10: Import failure handling
**Given** the import fails (invalid file format, processing error)
**When** the wizard receives a "Failed" status
**Then** the wizard shows a prominent error with a clear message explaining what happened
**And** a retry path is available (return to upload step)

### AC11: Wizard close and refresh
**Given** the user is done reviewing the import summary
**When** they close the import wizard
**Then** the candidate list refreshes to show all imported candidates
**And** a success toast confirms: "N candidates imported"

### AC12: Closed recruitment guard
**Given** a closed recruitment exists
**When** the user views the recruitment
**Then** the "Import Candidates" button is not available

### Prerequisites
- **Story 3.2** (XLSX Import Pipeline) -- ImportSession aggregate, ImportPipelineHostedService, Channel<T>, GetImportSession polling query, import API endpoints

### FRs Fulfilled
- **FR14:** Users can import candidates by uploading a Workday XLSX file
- **FR17:** The system flags low-confidence matches for manual review
- **FR18:** Users can view an import summary showing created, updated, and errored records with row-level detail
- **FR25:** The system validates uploaded XLSX files before processing and reports clear errors
- **FR26:** The system reports count discrepancies as informational notices, not errors
- **FR55:** The system provides contextual Workday export instructions within the import flow

## Tasks / Subtasks

- [ ] Task 1: Backend -- ResolveMatchConflict command, validator, handler (AC: #7, #8)
  - [ ] 1.1 Create `ResolveMatchConflictCommand` record in `api/src/Application/Features/Import/Commands/ResolveMatchConflict/`
  - [ ] 1.2 Create `ResolveMatchConflictCommandValidator` with FluentValidation (importSessionId required, matchIndex required, action required: "Confirm" | "Reject")
  - [ ] 1.3 Create `ResolveMatchConflictCommandHandler` -- loads ImportSession aggregate, confirms or rejects match, saves
  - [ ] 1.4 Unit test handler: confirm match updates candidate profile, marks match as confirmed
  - [ ] 1.5 Unit test handler: reject match creates new candidate, records rejection
  - [ ] 1.6 Unit test handler: import session not found throws NotFoundException
  - [ ] 1.7 Unit test validator: empty importSessionId fails, invalid action fails
- [ ] Task 2: Backend -- Minimal API endpoint `POST /api/import-sessions/{id}/resolve-match` (AC: #7, #8)
  - [ ] 2.1 Add `POST /api/import-sessions/{id}/resolve-match` to `ImportEndpoints.cs`
  - [ ] 2.2 Return `200 OK` with updated match result on success
  - [ ] 2.3 Integration test: successful confirm returns 200
  - [ ] 2.4 Integration test: successful reject returns 200
  - [ ] 2.5 Integration test: non-existent import session returns 404 Problem Details
- [ ] Task 3: Frontend -- API client for import operations (AC: #3, #4, #7, #8)
  - [ ] 3.1 Create `web/src/lib/api/import.ts` -- `importApi` with methods: `startImport`, `getSession`, `resolveMatch`
  - [ ] 3.2 Create `web/src/lib/api/import.types.ts` -- request/response types for import session, match resolution
- [ ] Task 4: Frontend -- Import session polling hook (AC: #3, #4, #10)
  - [ ] 4.1 Create `web/src/features/candidates/ImportFlow/hooks/useImportSession.ts` -- TanStack Query hook with `refetchInterval` for polling
  - [ ] 4.2 Polling enabled when status is "Processing", disabled on "Completed" or "Failed"
  - [ ] 4.3 Timeout after 120 seconds with clear error message
  - [ ] 4.4 Unit test: polling enables when processing, disables on completion
- [ ] Task 5: Frontend -- ImportWizard container (AC: #1, #11, #12)
  - [ ] 5.1 Create `web/src/features/candidates/ImportFlow/ImportWizard.tsx` -- Sheet container with multi-step state machine
  - [ ] 5.2 Wizard steps: Upload -> Processing -> Summary (with optional MatchReview)
  - [ ] 5.3 Sheet slides from right, full height, wider than default (`max-w-lg` or `w-[600px]`)
  - [ ] 5.4 On close: invalidate candidate queries, show success toast
  - [ ] 5.5 Unit tests: wizard opens as Sheet, step transitions work, close triggers refresh
  - [ ] 5.6 MSW handler for `POST /api/recruitments/:id/import` returning 202
- [ ] Task 6: Frontend -- FileUploadStep component (AC: #2)
  - [ ] 6.1 Create `web/src/features/candidates/ImportFlow/FileUploadStep.tsx` -- drag-and-drop upload zone
  - [ ] 6.2 Client-side file validation: .xlsx type, max 10 MB
  - [ ] 6.3 Display file size limit prominently
  - [ ] 6.4 "Start Import" button disabled until valid file selected
  - [ ] 6.5 Unit tests: file type validation, size validation, button state
- [ ] Task 7: Frontend -- WorkdayGuide component (AC: #2)
  - [ ] 7.1 Create `web/src/features/candidates/ImportFlow/WorkdayGuide.tsx` -- contextual export instructions
  - [ ] 7.2 Collapsible section with Workday export steps (what to select, which exports to run, "always export all candidates" rule)
  - [ ] 7.3 Uses shadcn/ui Collapsible component
  - [ ] 7.4 Unit test: renders instructions, toggles collapse
- [ ] Task 8: Frontend -- ImportProgress component (AC: #3)
  - [ ] 8.1 Create `web/src/features/candidates/ImportFlow/ImportProgress.tsx` -- polling-based progress display
  - [ ] 8.2 Shows Progress bar (indeterminate or determinate based on backend data)
  - [ ] 8.3 Descriptive text: "Importing candidates..."
  - [ ] 8.4 No user actions available during processing (back button hidden)
  - [ ] 8.5 Unit tests: renders progress indicator, shows descriptive text, transitions on completion
- [ ] Task 9: Frontend -- ImportSummary component (AC: #4, #5, #9, #11)
  - [ ] 9.1 Create `web/src/features/candidates/ImportFlow/ImportSummary.tsx` -- summary counts + drill-down
  - [ ] 9.2 Summary cards: created, updated, errored, flagged counts
  - [ ] 9.3 Expandable sections for each category showing row-level detail
  - [ ] 9.4 Error rows show: row number, candidate name, error message
  - [ ] 9.5 Count discrepancy shown as info Alert (amber/blue styling)
  - [ ] 9.6 "Done" button closes wizard
  - [ ] 9.7 Unit tests: renders counts, expandable detail, error display, discrepancy notice
- [ ] Task 10: Frontend -- MatchReviewStep component (AC: #6, #7, #8)
  - [ ] 10.1 Create `web/src/features/candidates/ImportFlow/MatchReviewStep.tsx` -- low-confidence match review
  - [ ] 10.2 Each flagged match shows: imported name/phone, matched existing candidate, match method
  - [ ] 10.3 Confirm/Reject buttons per match
  - [ ] 10.4 Create `useResolveMatch` mutation hook
  - [ ] 10.5 Amber indicator with count: "N matches by name+phone only -- review recommended"
  - [ ] 10.6 Unit tests: renders match details, confirm/reject calls API, updates UI optimistically
- [ ] Task 11: Frontend -- Wire ImportWizard into RecruitmentPage (AC: #1, #12)
  - [ ] 11.1 Add "Import Candidates" button to RecruitmentPage (or candidate list area)
  - [ ] 11.2 Button hidden when recruitment status is "Closed"
  - [ ] 11.3 Button opens ImportWizard Sheet
  - [ ] 11.4 Unit test: button renders for active recruitment, hidden for closed

## Dev Notes

### Affected Aggregate(s)

**ImportSession** (aggregate root) -- the backend addition is `ResolveMatchConflictCommand` which operates on the ImportSession aggregate to confirm or reject low-confidence matches. The ImportSession aggregate is created by Story 3.2 and tracks import status, row-level results, and match results.

**Candidate** (aggregate root, indirectly) -- when a match is confirmed, the candidate's profile fields are updated. When a match is rejected, a new candidate is created. Both operations go through the Candidate aggregate methods.

Cross-aggregate: ResolveMatchConflict affects both ImportSession (updates match status) and Candidate (updates profile or creates new). Use domain events for cross-aggregate coordination per architecture rules.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (ResolveMatchConflict handler) | **Test-first** | Business logic -- match confirmation/rejection with cross-aggregate coordination |
| Task 2 (API endpoint) | **Test-first** | Integration boundary -- verify 200, 404 Problem Details |
| Task 3 (API client) | **Characterization** | Thin wrapper over httpClient -- test via component integration tests |
| Task 4 (Polling hook) | **Test-first** | Async behavior -- must verify polling lifecycle, timeout handling |
| Task 5 (ImportWizard container) | **Test-first** | Multi-step state machine -- step transitions, close behavior |
| Task 6 (FileUploadStep) | **Test-first** | Validation logic -- file type/size, button state |
| Task 7 (WorkdayGuide) | **Characterization** | Static content display -- verify renders |
| Task 8 (ImportProgress) | **Test-first** | Async UI state -- progress display, transitions |
| Task 9 (ImportSummary) | **Test-first** | Complex data display -- counts, expandable detail, error presentation |
| Task 10 (MatchReviewStep) | **Test-first** | User-facing decisions -- confirm/reject actions, API integration |
| Task 11 (Wiring) | **Characterization** | Glue code -- verify button renders and opens wizard |

### Technical Requirements

**Backend -- MUST follow these patterns:**

1. **CQRS folder structure:**
   ```
   api/src/Application/Features/Import/
     Commands/
       ResolveMatchConflict/
         ResolveMatchConflictCommand.cs
         ResolveMatchConflictCommandValidator.cs
         ResolveMatchConflictCommandHandler.cs
   ```
   Rule: One command per folder. Handler in same folder.

2. **Command as record:**
   ```csharp
   public record ResolveMatchConflictCommand(
       Guid ImportSessionId,
       int MatchIndex,
       string Action  // "Confirm" or "Reject"
   ) : IRequest<ResolveMatchResultDto>;
   ```

3. **Handler pattern -- loads ImportSession, verifies membership, resolves match:**
   ```csharp
   public class ResolveMatchConflictCommandHandler(
       IApplicationDbContext dbContext,
       ITenantContext tenantContext)
       : IRequestHandler<ResolveMatchConflictCommand, ResolveMatchResultDto>
   {
       public async Task<ResolveMatchResultDto> Handle(
           ResolveMatchConflictCommand request,
           CancellationToken cancellationToken)
       {
           var importSession = await dbContext.ImportSessions
               .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, cancellationToken)
               ?? throw new NotFoundException(nameof(ImportSession), request.ImportSessionId);

           if (request.Action == "Confirm")
           {
               importSession.ConfirmMatch(request.MatchIndex);
               // Update candidate profile via domain event or direct call
           }
           else
           {
               importSession.RejectMatch(request.MatchIndex);
               // Create new candidate via domain event
           }

           await dbContext.SaveChangesAsync(cancellationToken);
           return ResolveMatchResultDto.From(importSession, request.MatchIndex);
       }
   }
   ```
   CRITICAL: The `ConfirmMatch()` and `RejectMatch()` methods on ImportSession handle all invariants. Do NOT duplicate domain logic in the handler.

4. **FluentValidation:**
   ```csharp
   public class ResolveMatchConflictCommandValidator
       : AbstractValidator<ResolveMatchConflictCommand>
   {
       public ResolveMatchConflictCommandValidator()
       {
           RuleFor(x => x.ImportSessionId).NotEmpty();
           RuleFor(x => x.MatchIndex).GreaterThanOrEqualTo(0);
           RuleFor(x => x.Action).NotEmpty()
               .Must(a => a == "Confirm" || a == "Reject")
               .WithMessage("Action must be 'Confirm' or 'Reject'");
       }
   }
   ```

5. **Minimal API endpoint -- add to `ImportEndpoints.cs`:**
   ```csharp
   group.MapPost("/{id:guid}/resolve-match", async (
       Guid id,
       ResolveMatchConflictRequest request,
       ISender sender) =>
   {
       var result = await sender.Send(new ResolveMatchConflictCommand(
           id, request.MatchIndex, request.Action));
       return Results.Ok(result);
   })
   .WithName("ResolveMatchConflict")
   .Produces<ResolveMatchResultDto>(StatusCodes.Status200OK)
   .ProducesProblem(StatusCodes.Status400BadRequest)
   .ProducesProblem(StatusCodes.Status404NotFound);
   ```
   Note: This endpoint lives on `ImportEndpoints` which maps to `/api/import-sessions`.

6. **Error handling:** Domain exceptions propagate to global exception middleware for Problem Details conversion. DO NOT catch domain exceptions in the handler.

7. **ITenantContext:** The handler should verify the user has access to the recruitment that owns the import session. Load via `IApplicationDbContext` which applies tenant filtering.

**Frontend -- MUST follow these patterns:**

1. **Feature folder structure:**
   ```
   web/src/features/candidates/ImportFlow/
     ImportWizard.tsx             (NEW -- Sheet container)
     ImportWizard.test.tsx        (NEW)
     FileUploadStep.tsx           (NEW)
     FileUploadStep.test.tsx      (NEW)
     ImportProgress.tsx           (NEW)
     ImportProgress.test.tsx      (NEW)
     ImportSummary.tsx            (NEW)
     ImportSummary.test.tsx       (NEW)
     MatchReviewStep.tsx          (NEW)
     MatchReviewStep.test.tsx     (NEW)
     WorkdayGuide.tsx             (NEW)
     WorkdayGuide.test.tsx        (NEW)
     hooks/
       useImportSession.ts        (NEW)
       useImportSession.test.ts   (NEW)
       useResolveMatch.ts         (NEW)
   web/src/lib/api/
     import.ts                    (NEW)
     import.types.ts              (NEW)
   web/src/mocks/
     importHandlers.ts            (NEW)
   ```

2. **API client using existing httpClient:**
   ```typescript
   // web/src/lib/api/import.ts
   import { apiGet, apiPost } from './httpClient'
   import type {
     ImportSessionResponse,
     ResolveMatchRequest,
     ResolveMatchResponse,
     StartImportResponse,
   } from './import.types'

   export const importApi = {
     startImport: (recruitmentId: string, file: File) => {
       // File upload requires FormData, NOT JSON
       const formData = new FormData()
       formData.append('file', file)
       return apiPostFormData<StartImportResponse>(
         `/recruitments/${recruitmentId}/import`,
         formData,
       )
     },

     getSession: (importSessionId: string) =>
       apiGet<ImportSessionResponse>(`/import-sessions/${importSessionId}`),

     resolveMatch: (importSessionId: string, data: ResolveMatchRequest) =>
       apiPost<ResolveMatchResponse>(
         `/import-sessions/${importSessionId}/resolve-match`,
         data,
       ),
   }
   ```
   CRITICAL: The `startImport` method uses `FormData` for file upload, NOT `JSON.stringify`. You may need to add an `apiPostFormData` helper to `httpClient.ts` that omits the `Content-Type` header (browser sets `multipart/form-data` boundary automatically).

3. **API types:**
   ```typescript
   // web/src/lib/api/import.types.ts

   export type ImportSessionStatus = 'Processing' | 'Completed' | 'Failed'

   export interface ImportRowResult {
     rowNumber: number
     candidateName: string | null
     email: string | null
     action: 'Created' | 'Updated' | 'Errored' | 'Flagged'
     errorMessage: string | null
     matchConfidence: 'High' | 'Low' | 'None' | null
     matchMethod: string | null
     matchedCandidateId: string | null
     matchedCandidateName: string | null
   }

   export interface ImportSessionResponse {
     id: string
     recruitmentId: string
     status: ImportSessionStatus
     sourceFileName: string
     uploadedAt: string
     completedAt: string | null
     totalRows: number
     createdCount: number
     updatedCount: number
     erroredCount: number
     flaggedCount: number
     rows: ImportRowResult[]
   }

   export interface StartImportResponse {
     importSessionId: string
     statusUrl: string
   }

   export interface ResolveMatchRequest {
     matchIndex: number
     action: 'Confirm' | 'Reject'
   }

   export interface ResolveMatchResponse {
     matchIndex: number
     action: 'Confirmed' | 'Rejected'
     candidateId: string
   }
   ```

4. **Import session polling hook using TanStack Query `refetchInterval`:**
   ```typescript
   // web/src/features/candidates/ImportFlow/hooks/useImportSession.ts
   import { useQuery } from '@tanstack/react-query'
   import { importApi } from '@/lib/api/import'
   import type { ImportSessionResponse } from '@/lib/api/import.types'

   const POLL_INTERVAL_MS = 2000
   const POLL_TIMEOUT_MS = 120_000

   export function useImportSession(importSessionId: string | null) {
     return useQuery<ImportSessionResponse>({
       queryKey: ['import-session', importSessionId],
       queryFn: () => importApi.getSession(importSessionId!),
       enabled: !!importSessionId,
       refetchInterval: (query) => {
         const data = query.state.data
         if (!data) return POLL_INTERVAL_MS
         // Stop polling when completed or failed
         if (data.status === 'Completed' || data.status === 'Failed') {
           return false
         }
         return POLL_INTERVAL_MS
       },
     })
   }
   ```
   IMPORTANT: `refetchInterval` in TanStack Query v5 receives the full query object, not just the data. Access data via `query.state.data`.

5. **ImportWizard state machine:**
   ```typescript
   type WizardStep = 'upload' | 'processing' | 'summary' | 'matchReview'

   // State transitions:
   // 'upload' -> user clicks "Start Import" -> 'processing'
   // 'processing' -> poll returns "Completed" -> 'summary'
   // 'processing' -> poll returns "Failed" -> 'upload' (with error)
   // 'summary' -> user clicks "Review Matches" (if flagged > 0) -> 'matchReview'
   // 'matchReview' -> user clicks "Done" -> close wizard
   // 'summary' -> user clicks "Done" -> close wizard
   ```

6. **Sheet component usage (NOT Dialog):**
   ```typescript
   import {
     Sheet,
     SheetContent,
     SheetHeader,
     SheetTitle,
     SheetDescription,
   } from '@/components/ui/sheet'

   // ImportWizard.tsx
   <Sheet open={isOpen} onOpenChange={handleOpenChange}>
     <SheetContent
       side="right"
       className="w-[600px] max-w-full sm:max-w-[600px] overflow-y-auto"
     >
       <SheetHeader>
         <SheetTitle>Import Candidates</SheetTitle>
         <SheetDescription>
           Upload a Workday export file to import candidates
         </SheetDescription>
       </SheetHeader>

       {/* Step content rendered based on current wizard step */}
       {step === 'upload' && <FileUploadStep ... />}
       {step === 'processing' && <ImportProgress ... />}
       {step === 'summary' && <ImportSummary ... />}
       {step === 'matchReview' && <MatchReviewStep ... />}
     </SheetContent>
   </Sheet>
   ```
   CRITICAL: Override the default `sm:max-w-sm` on SheetContent to give the wizard adequate width. Use `className` to set `w-[600px]` or `max-w-lg`.

7. **File upload with FormData:**
   The `startImport` API sends a file using `FormData`. You must add a `apiPostFormData` helper to `httpClient.ts` that:
   - Does NOT set `Content-Type: application/json` (browser auto-sets `multipart/form-data` with boundary)
   - Still includes auth headers
   - Uses the same `handleResponse` for error handling

   ```typescript
   // Add to web/src/lib/api/httpClient.ts
   export async function apiPostFormData<T>(
     path: string,
     formData: FormData,
   ): Promise<T> {
     const headers = await getAuthHeaders()
     // Remove Content-Type -- browser sets multipart/form-data with boundary
     delete (headers as Record<string, string>)['Content-Type']
     const res = await fetch(`${API_BASE}${path}`, {
       method: 'POST',
       headers,
       body: formData,
     })
     return handleResponse<T>(res)
   }
   ```

8. **Toast on wizard close:**
   ```typescript
   import { useAppToast } from '@/hooks/useAppToast'

   const toast = useAppToast()

   function handleWizardClose() {
     if (importSession?.status === 'Completed') {
       const count = importSession.createdCount + importSession.updatedCount
       toast.success(`${count} candidates imported`)
     }
     queryClient.invalidateQueries({ queryKey: ['candidates'] })
     onClose()
   }
   ```

9. **TanStack Query mutations for match resolution:**
   ```typescript
   // web/src/features/candidates/ImportFlow/hooks/useResolveMatch.ts
   import { useMutation, useQueryClient } from '@tanstack/react-query'
   import { importApi } from '@/lib/api/import'
   import type { ResolveMatchRequest } from '@/lib/api/import.types'

   export function useResolveMatch(importSessionId: string) {
     const queryClient = useQueryClient()
     return useMutation({
       mutationFn: (data: ResolveMatchRequest) =>
         importApi.resolveMatch(importSessionId, data),
       onSuccess: () => {
         void queryClient.invalidateQueries({
           queryKey: ['import-session', importSessionId],
         })
       },
     })
   }
   ```
   Note: `isPending` (not `isLoading`) for TanStack Query v5.

10. **MSW handlers for testing:**
    ```typescript
    // web/src/mocks/importHandlers.ts
    import { http, HttpResponse } from 'msw'
    import type { ImportSessionResponse } from '@/lib/api/import.types'

    export const mockImportSessionId = 'import-session-001'

    const mockCompletedSession: ImportSessionResponse = {
      id: mockImportSessionId,
      recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
      status: 'Completed',
      sourceFileName: 'workday-export.xlsx',
      uploadedAt: new Date().toISOString(),
      completedAt: new Date().toISOString(),
      totalRows: 10,
      createdCount: 7,
      updatedCount: 2,
      erroredCount: 0,
      flaggedCount: 1,
      rows: [
        {
          rowNumber: 1,
          candidateName: 'Anna Svensson',
          email: 'anna@example.com',
          action: 'Created',
          errorMessage: null,
          matchConfidence: null,
          matchMethod: null,
          matchedCandidateId: null,
          matchedCandidateName: null,
        },
        // ... more rows
      ],
    }

    export const importHandlers = [
      http.post('/api/recruitments/:id/import', () => {
        return HttpResponse.json(
          {
            importSessionId: mockImportSessionId,
            statusUrl: `/api/import-sessions/${mockImportSessionId}`,
          },
          { status: 202 },
        )
      }),

      http.get('/api/import-sessions/:id', () => {
        return HttpResponse.json(mockCompletedSession)
      }),

      http.post('/api/import-sessions/:id/resolve-match', async ({ request }) => {
        const body = (await request.json()) as {
          matchIndex: number
          action: string
        }
        return HttpResponse.json({
          matchIndex: body.matchIndex,
          action: body.action === 'Confirm' ? 'Confirmed' : 'Rejected',
          candidateId: 'new-candidate-id',
        })
      }),
    ]
    ```
    Register these handlers in `web/src/mocks/handlers.ts` alongside existing handlers.

### Architecture Compliance

- **Aggregate root access only:** Call `importSession.ConfirmMatch()` and `importSession.RejectMatch()`. NEVER directly modify match results.
- **Ubiquitous language:** Use "Import Session" (not upload/sync/batch), "Candidate" (not applicant), "Recruitment" (not job/position).
- **Manual DTO mapping:** `static From()` factory on response DTOs. NO AutoMapper.
- **Problem Details for errors:** Test that validation failures return RFC 9457 Problem Details with `errors` object.
- **No PII in audit events/logs:** Match resolution events contain only entity IDs (ImportSessionId, CandidateId, MatchIndex).
- **NSubstitute for ALL mocking** (never Moq).
- **MediatR v13+:** `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`.
- **httpClient.ts is the SINGLE HTTP entry point** -- never call `fetch` directly from API modules.
- **Shared components:** Use `ActionButton`, `EmptyState`, `SkeletonLoader`, `StatusBadge` from `web/src/components/`. Do NOT create feature-local equivalents.
- **Sheet, NOT Dialog:** The import wizard uses shadcn/ui Sheet (full-height slide from right), not Dialog. This provides more space and keeps recruitment context partially visible.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| .NET | 10.0 | LTS. Use primary constructors for DI. |
| EF Core | 10.x | Fluent API only. `FirstOrDefaultAsync` for loading by ID. |
| MediatR | 13.x | `IRequest<T>`, pipeline behaviors for validation. |
| FluentValidation | Latest | `AbstractValidator<T>`, registered via DI. |
| React | 19.x | Controlled components, NOT React 19 form Actions. |
| TypeScript | 5.7.x | Strict mode, `erasableSyntaxOnly` in tsconfig. |
| TanStack Query | 5.x | `useQuery` with `refetchInterval` for polling, `useMutation` for match resolution. `isPending` not `isLoading`. |
| shadcn/ui | Installed | Sheet (wizard container), Progress, Alert, Collapsible, Button components. |
| Tailwind CSS | 4.x | CSS-first config via `@theme` in `index.css`. |
| sonner | Installed | Toast notifications via `useAppToast()` hook. |
| Lucide React | Installed | Icons: Upload, FileSpreadsheet, CheckCircle, XCircle, AlertTriangle, Loader2, ChevronDown. |

### File Structure Requirements

**New files to create:**
```
api/src/Application/Features/Import/
  Commands/ResolveMatchConflict/
    ResolveMatchConflictCommand.cs
    ResolveMatchConflictCommandValidator.cs
    ResolveMatchConflictCommandHandler.cs
    ResolveMatchResultDto.cs

api/tests/Application.UnitTests/Features/Import/
  Commands/ResolveMatchConflict/
    ResolveMatchConflictCommandHandlerTests.cs
    ResolveMatchConflictCommandValidatorTests.cs

web/src/features/candidates/ImportFlow/
  ImportWizard.tsx
  ImportWizard.test.tsx
  FileUploadStep.tsx
  FileUploadStep.test.tsx
  ImportProgress.tsx
  ImportProgress.test.tsx
  ImportSummary.tsx
  ImportSummary.test.tsx
  MatchReviewStep.tsx
  MatchReviewStep.test.tsx
  WorkdayGuide.tsx
  WorkdayGuide.test.tsx
  hooks/
    useImportSession.ts
    useImportSession.test.ts
    useResolveMatch.ts

web/src/lib/api/
  import.ts
  import.types.ts

web/src/mocks/
  importHandlers.ts
```

**Existing files to modify:**
```
web/src/lib/api/httpClient.ts                -- Add apiPostFormData helper for multipart file upload
web/src/mocks/handlers.ts                    -- Register importHandlers
web/src/features/recruitments/pages/RecruitmentPage.tsx  -- Add "Import Candidates" button that opens ImportWizard
api/src/Web/Endpoints/ImportEndpoints.cs     -- Add POST resolve-match endpoint (created by Story 3.2)
```

### Testing Requirements

**Backend tests (NUnit + NSubstitute + FluentAssertions):**
- Handler tests: mock `IApplicationDbContext` and `ITenantContext`, verify `ConfirmMatch()` called on import session for confirm action
- Handler tests: verify `RejectMatch()` called on import session for reject action, new candidate created
- Handler tests: import session not found -> throws `NotFoundException`
- Validator tests: empty importSessionId -> fails, invalid action -> fails, negative matchIndex -> fails
- Integration tests: `POST /api/import-sessions/{id}/resolve-match` with confirm -> 200 OK
- Integration tests: `POST /api/import-sessions/{id}/resolve-match` with reject -> 200 OK
- Integration tests: non-existent import session -> 404 Problem Details
- Test naming: `MethodName_Scenario_ExpectedBehavior`

**Frontend tests (Vitest + Testing Library + MSW):**
- ImportWizard: renders as Sheet when opened, shows upload step initially
- ImportWizard: transitions from upload to processing when file submitted
- ImportWizard: transitions from processing to summary on "Completed" status
- ImportWizard: shows error and retry on "Failed" status
- ImportWizard: closes Sheet and fires toast on "Done" click
- ImportWizard: invalidates candidate queries on close
- FileUploadStep: accepts .xlsx files, rejects non-xlsx
- FileUploadStep: rejects files over 10 MB with error message
- FileUploadStep: "Start Import" button disabled until file selected
- FileUploadStep: "Start Import" button shows loading state when uploading
- WorkdayGuide: renders export instructions, toggles collapsible
- ImportProgress: renders progress indicator with descriptive text
- ImportProgress: transitions when polling returns "Completed"
- ImportSummary: renders created/updated/errored/flagged counts
- ImportSummary: expands to show row-level detail
- ImportSummary: shows error rows with row number, name, and error message
- ImportSummary: shows amber info notice for count discrepancy
- MatchReviewStep: renders flagged matches with imported data and existing candidate
- MatchReviewStep: "Confirm Match" calls API with correct data
- MatchReviewStep: "Reject" calls API with correct data
- MatchReviewStep: updates UI after match resolution
- useImportSession: polls at 2-second intervals when status is "Processing"
- useImportSession: stops polling when status is "Completed"
- useImportSession: stops polling when status is "Failed"
- Wiring: "Import Candidates" button renders for active recruitment
- Wiring: "Import Candidates" button hidden for closed recruitment
- MSW handlers: mock all import API endpoints
- Use custom `test-utils.tsx` that wraps with QueryClientProvider + MemoryRouter

### Previous Story Intelligence

**From Story 1.2 (SSO Auth):**
- `httpClient.ts` is the SINGLE HTTP entry point -- always use `apiGet`/`apiPost` from it
- Dev auth uses `VITE_AUTH_MODE=development` with `X-Dev-User-Id`/`X-Dev-User-Name` headers
- Need to add `apiPostFormData` to `httpClient.ts` for file upload (does not exist yet)

**From Story 1.3 (Core Data Model):**
- `GuidEntity` base class provides `Guid Id` + `DomainEvents` collection
- Child entity constructors are `internal` -- only creatable through aggregate root methods
- `ApplicationDbContext` constructor requires `ITenantContext` -- mock it in tests
- MediatR 13: `RequestHandlerDelegate` takes `CancellationToken` -- lambda uses `(_)` not `()`

**From Story 1.4 (Shared UI Components):**
- 18 shadcn/ui components already installed in `web/src/components/ui/` including **Sheet**, **Progress**, **Alert**, **Collapsible**, **Badge**
- Custom components: `StatusBadge`, `ActionButton`, `EmptyState`, `SkeletonLoader`, `ErrorBoundary`
- `useAppToast()` hook for toast notifications (3-second auto-dismiss for success)
- `cn()` utility in `web/src/lib/utils.ts` for className merging
- Design tokens in `@theme` block in `web/src/index.css`

**From Story 1.5 (App Shell):**
- React Router v7 declarative mode (NOT framework mode) -- single `react-router` package
- TanStack Query v5: `isPending` replaces `isLoading`, `retry: false` for mutations
- CSS Grid layout: `grid-template-rows: 48px 1fr`
- `ProtectedRoute` calls `login()` instead of navigating to `/login`

**From Story 2.1 (Create Recruitment):**
- CQRS folder structure established: one command per folder with Command, Validator, Handler
- Response DTOs use `static From()` factory methods
- Frontend API client pattern established in `web/src/lib/api/recruitments.ts`
- TanStack Query mutation pattern established in `useCreateRecruitment.ts`

**From Story 2.5 (Close Recruitment):**
- Read-only mode enforcement: check `recruitment.status === 'Closed'` to hide mutation controls
- AlertDialog used for destructive confirmations
- `useCloseRecruitment` mutation hook pattern

**From Story 3.1 (Manual Candidate Management -- prerequisite):**
- `Candidate` aggregate root created with all fields
- `CreateCandidateCommand` exists
- Candidate API client (`candidates.ts`) may exist

**From Story 3.2 (XLSX Import Pipeline -- prerequisite):**
- `ImportSession` aggregate root with status transitions (Processing -> Completed/Failed)
- `StartImportCommand` accepts file upload, creates ImportSession, returns 202 + session ID
- `GetImportSessionQuery` returns session status with row-level detail
- `ImportPipelineHostedService` processes XLSX via `Channel<T>`
- `ImportEndpoints.cs` with `POST /api/recruitments/{id}/import` and `GET /api/import-sessions/{id}`
- Matching engine: email (high confidence), name+phone (low confidence)
- Row results as value objects on ImportSession

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(3.3): add apiPostFormData helper to httpClient`
2. `feat(3.3): add import API client, types, and MSW handlers`
3. `feat(3.3): add ResolveMatchConflict command, validator, handler + tests`
4. `feat(3.3): add resolve-match endpoint to ImportEndpoints`
5. `feat(3.3): add useImportSession polling hook`
6. `feat(3.3): add FileUploadStep component + tests`
7. `feat(3.3): add WorkdayGuide component + tests`
8. `feat(3.3): add ImportProgress component + tests`
9. `feat(3.3): add ImportSummary component + tests`
10. `feat(3.3): add MatchReviewStep component + tests`
11. `feat(3.3): add ImportWizard container with step state machine + tests`
12. `feat(3.3): wire ImportWizard into RecruitmentPage + tests`

### Latest Tech Information

- **.NET 10.0:** LTS until Nov 2028. No breaking changes for typical CRUD. File upload via `IFormFile` in Minimal API is well-supported.
- **EF Core 10:** `FirstOrDefaultAsync` for loading by ID. No special considerations for this story.
- **MediatR 13.x:** `IRequest<T>` pattern unchanged. Pipeline behaviors for validation.
- **React 19.2:** No issues with shadcn/ui Sheet component or file input handling.
- **TanStack Query 5.90.x:** `refetchInterval` callback receives full `Query` object (not just data). Access data via `query.state.data`. `isPending` for mutation loading state.
- **shadcn/ui Sheet:** Based on Radix Dialog primitive. Default `sm:max-w-sm` is too narrow for the wizard -- override via `className`. The `side="right"` prop slides from right. `showCloseButton` prop available (default true).
- **File uploads with fetch:** When using `FormData`, do NOT set `Content-Type` header manually. The browser auto-sets `multipart/form-data` with the correct boundary string. Setting it manually breaks the upload.

### Project Structure Notes

- All import flow components live in `web/src/features/candidates/ImportFlow/` per the architecture project structure specification
- API types co-located with API modules in `web/src/lib/api/import.types.ts`
- MSW handlers in `web/src/mocks/importHandlers.ts` -- registered in `handlers.ts` alongside existing handlers
- Backend ResolveMatchConflict command follows CQRS structure at `api/src/Application/Features/Import/Commands/ResolveMatchConflict/`
- ImportEndpoints.cs (created by Story 3.2) is modified to add the resolve-match endpoint
- httpClient.ts is modified to add `apiPostFormData` -- this is a shared utility, not feature-specific

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-3-candidate-import-document-management.md` -- Story 3.3 acceptance criteria, FR mapping]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Aggregate boundaries, ImportSession invariants, ITenantContext]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-backend.md` -- C# naming, CQRS structure, DTO mapping, error handling, test conventions]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- React/TS naming, component structure, loading states, error handling]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- TanStack Query patterns, API client contract, state management]
- [Source: `_bmad-output/planning-artifacts/architecture/api-patterns.md` -- Async operations (202 Accepted), response formats, Problem Details]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- Full directory tree, ImportFlow folder path, file boundaries]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Vitest, Testing Library, MSW, NUnit, NSubstitute patterns]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md` -- J0/J2 import journey flows, async processing state]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md` -- ImportWizard component spec, Sheet usage, polling interval]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md` -- Button hierarchy, feedback patterns, inline alerts, processing states]
- [Source: `_bmad-output/planning-artifacts/prd.md` -- FR14, FR17, FR18, FR25, FR26, FR55 requirements]
- [Source: `web/src/lib/api/httpClient.ts` -- HTTP client with dual auth path, needs apiPostFormData addition]
- [Source: `web/src/components/ui/sheet.tsx` -- Existing Sheet component with side/className props]
- [Source: `web/src/hooks/useAppToast.ts` -- Toast hook with success/error/info variants]
- [Source: `web/src/mocks/recruitmentHandlers.ts` -- MSW handler pattern reference]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

### File List

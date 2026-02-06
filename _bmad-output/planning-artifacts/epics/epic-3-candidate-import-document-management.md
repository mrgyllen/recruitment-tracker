# Epic 3: Candidate Import & Document Management

Erik can import candidates from Workday, upload CV bundles that auto-split and match, manually create candidates, and upload individual CVs.

## Story 3.1: Manual Candidate Management

As a **recruiting leader (Erik)**,
I want to manually create and remove candidate records within a recruitment,
So that I can add candidates who aren't in the Workday export (referrals, direct applications) and remove candidates who shouldn't be tracked.

**Acceptance Criteria:**

**Given** an active recruitment with workflow steps exists
**When** the user clicks "Add Candidate"
**Then** a form is displayed with fields for full name (required), email (required), phone (optional), location (optional), and date applied (optional, defaults to today)

**Given** the add candidate form is open
**When** the user submits with valid data
**Then** a new candidate is created within the recruitment
**And** the candidate is placed at the first workflow step with outcome status "Not Started"
**And** the API returns 201 Created
**And** a success toast confirms the creation
**And** the candidate appears in the candidate list

**Given** the add candidate form is open
**When** the user submits with a missing full name or email
**Then** field-level validation errors are shown
**And** the form is not submitted

**Given** a candidate with the same email already exists in this recruitment
**When** the user attempts to create a duplicate
**Then** the API returns 400 Bad Request with Problem Details: "A candidate with this email already exists in this recruitment"
**And** the user sees an inline error message

**Given** an active recruitment has candidates
**When** the user clicks the remove action for a candidate
**Then** a confirmation dialog is shown: "Remove [Name] from this recruitment? This cannot be undone."

**Given** the user confirms the removal
**When** the system processes the removal
**Then** the candidate is removed from the recruitment
**And** the candidate list updates to reflect the removal
**And** a success toast confirms the removal

**Given** a closed recruitment exists
**When** the user views the candidate list
**Then** the "Add Candidate" and remove actions are not available

**Technical notes:**
- Backend: `CreateCandidateCommand` + handler + FluentValidation, `RemoveCandidateCommand` + handler
- Frontend: `CreateCandidateForm.tsx` with react-hook-form + zod
- Domain: `Candidate` aggregate root created with all fields needed for import stories (enables FR14-FR22 later)
- Cross-aggregate reference: `Candidate` holds `RecruitmentId` (ID only, no navigation property)
- FR23, FR24, FR62 fulfilled

## Story 3.2: XLSX Import Pipeline

As a **recruiting leader (Erik)**,
I want to upload a Workday XLSX export file and have the system automatically parse, validate, and import candidates,
So that I can quickly populate a recruitment with candidates from Workday without manual data entry.

**Acceptance Criteria:**

**Given** an active recruitment exists
**When** the user uploads an XLSX file via the import endpoint
**Then** the system validates the file type (.xlsx) and size (≤10 MB)
**And** if validation fails, the API returns 400 Bad Request with Problem Details describing the issue

**Given** a valid XLSX file is uploaded
**When** the API receives the file
**Then** an `ImportSession` is created with status "Processing", recording who uploaded, when, and the source filename
**And** the API returns 202 Accepted with the import session ID and a status polling URL
**And** the file is queued for async processing via `Channel<T>`

**Given** the import pipeline processes the XLSX
**When** the file is parsed
**Then** the system extracts five fields per row: full name, email, phone, location, and date applied
**And** column names are resolved using configurable column-name mapping (NFR30)

**Given** the XLSX file has an invalid format or is missing required columns
**When** the parser attempts to read it
**Then** the `ImportSession` status is set to "Failed" with a clear error message identifying what's wrong
**And** no candidates are created or modified

**Given** candidates are extracted from the XLSX
**When** the matching engine runs
**Then** each candidate is matched to existing records by email (case-insensitive, high confidence)
**And** if no email match is found, name + phone is used as a low-confidence fallback
**And** if no match is found, a new candidate is created at the first workflow step with status "Not Started"

**Given** a candidate matches an existing record by email
**When** the import processes the match
**Then** the candidate's profile fields (name, phone, location, date applied) are updated from the XLSX
**And** app-side data is never overwritten (workflow states, outcomes, reason codes)

**Given** a candidate matches by name + phone (low confidence)
**When** the import processes the match
**Then** the match is flagged as low confidence for manual review
**And** the candidate's profile fields are NOT updated until the match is confirmed

**Given** a candidate in the XLSX has no match in the recruitment
**When** the import processes the row
**Then** a new candidate is created with the extracted fields
**And** the candidate is placed at the first workflow step with outcome status "Not Started"

**Given** a candidate exists in the recruitment but is missing from the XLSX
**When** the import completes
**Then** the existing candidate is NOT deleted or modified

**Given** the same XLSX file is uploaded twice
**When** the second import processes
**Then** the result is identical to the first import (idempotent)
**And** no duplicate candidates are created
**And** no data is corrupted

**Given** the import pipeline completes (success or failure)
**When** the client polls the import session endpoint
**Then** the response includes: status (Completed/Failed), summary counts (created, updated, errored, flagged), and row-level detail for any errors or flags

**Technical notes:**
- Backend: `StartImportCommand` (accepts file upload, creates ImportSession, writes to Channel<T>), `ImportPipelineHostedService` (IHostedService consumer), `GetImportSessionQuery` (polling endpoint)
- Infrastructure: `XlsxParserService` (IXlsxParser), `CandidateMatchingEngine` (ICandidateMatchingEngine)
- Domain: `ImportSession` aggregate root tracks status transitions (Processing → Completed/Failed), row-level results as value objects
- ImportSession sets `ITenantContext.RecruitmentId` for scoped data access
- FR14, FR15, FR16, FR17, FR19, FR20, FR21, FR22, FR25 fulfilled

## Story 3.3: Import Wizard & Summary UI

As a **recruiting leader (Erik)**,
I want a guided import wizard that shows upload progress, a clear summary of results, and lets me review low-confidence matches,
So that I can confidently import candidates and resolve any matching issues without guessing what happened.

**Acceptance Criteria:**

**Given** the user is viewing an active recruitment
**When** they click "Import Candidates"
**Then** a Sheet component slides in from the right, full height, containing the import wizard

**Given** the import wizard is open on Step 1 (Upload)
**When** the user views the upload step
**Then** a file upload area accepts XLSX and/or PDF bundle files
**And** contextual Workday export instructions are visible (what to select, which exports to run, the "always export all candidates" rule)
**And** each file type has its size limit displayed (XLSX: 10 MB, PDF: 100 MB)

**Given** the user has selected valid file(s)
**When** they click "Start Import"
**Then** the wizard transitions to a processing state with a progress indicator
**And** the UI displays "Importing candidates..." (XLSX) or "Splitting PDF bundle..." (PDF) or both
**And** the frontend polls `GET /api/import-sessions/{id}` for progress updates

**Given** the import processing completes successfully
**When** the wizard receives a "Completed" status
**Then** the wizard transitions to the import summary view
**And** the summary shows: total candidates processed, created count, updated count, errored count
**And** row-level detail is accessible for each category (expandable or drillable)

**Given** the import summary shows errors
**When** the user views the error details
**Then** each errored row shows the row number, candidate name (if available), and the specific error message
**And** the errors are presented clearly but do not block the rest of the import results

**Given** low-confidence matches were flagged during import
**When** the summary is displayed
**Then** flagged matches are shown with an amber indicator: "N matches by name+phone only — review recommended"
**And** each flagged match shows: the imported name/phone, the matched existing candidate, and the match method
**And** the user can confirm or reject each match

**Given** the user confirms a low-confidence match
**When** they click "Confirm Match"
**Then** the candidate's profile fields are updated from the import data
**And** the match status updates to confirmed

**Given** the user rejects a low-confidence match
**When** they click "Reject"
**Then** a new candidate is created from the import data instead
**And** the rejected match is recorded in the import session

**Given** there is a count discrepancy between imported candidates and split CVs
**When** the summary is displayed
**Then** the discrepancy is reported as an informational notice (not an error)
**And** the notice uses amber/info styling, not red error styling

**Given** the import fails (invalid file format, processing error)
**When** the wizard receives a "Failed" status
**Then** the wizard shows a prominent error with a clear message explaining what happened
**And** a retry path is available (return to upload step)

**Given** the user is done reviewing the import summary
**When** they close the import wizard
**Then** the candidate list refreshes to show all imported candidates
**And** a success toast confirms: "N candidates imported"

**Technical notes:**
- Frontend: `ImportWizard.tsx` (Sheet container), `FileUploadStep.tsx`, `ImportProgress.tsx` (polling), `ImportSummary.tsx`, `MatchReviewStep.tsx`, `WorkdayGuide.tsx`
- Backend: `ResolveMatchConflictCommand` for confirming/rejecting low-confidence matches
- Uses shadcn/ui Sheet, Form, Progress, Alert components
- NFR10: import summary with row-level detail renders within 2 seconds
- FR14, FR17, FR18, FR25, FR26, FR55 fulfilled

## Story 3.4: PDF Bundle Upload & Splitting

As a **recruiting leader (Erik)**,
I want to upload a Workday CV bundle PDF and have the system automatically split it into individual per-candidate documents,
So that each candidate has their own viewable CV without manual file splitting.

**Acceptance Criteria:**

**Given** an active recruitment exists
**When** the user uploads a PDF bundle file via the import endpoint
**Then** the system validates the file type (.pdf) and size (≤100 MB)
**And** if validation fails, the API returns 400 Bad Request with Problem Details describing the issue

**Given** a valid PDF bundle is uploaded as part of an import session
**When** the import pipeline processes the bundle
**Then** the system parses the Workday TOC table to identify candidate entries
**And** each TOC entry's candidate name and Workday Candidate ID are extracted

**Given** the TOC has been parsed
**When** the system determines page boundaries
**Then** individual per-candidate PDF documents are produced from the bundle using TOC link annotations for page boundaries
**And** each split PDF contains the complete CV + letter content for that candidate

**Given** individual PDFs have been split
**When** the system stores the documents
**Then** each PDF is uploaded to Azure Blob Storage (not the database)
**And** a `CandidateDocument` record is created referencing the blob storage path
**And** the Workday Candidate ID from the TOC is stored as reference metadata on the document

**Given** the PDF bundle cannot be parsed (invalid format, no TOC found)
**When** the splitter encounters the error
**Then** the `ImportSession` records the failure with a clear error message identifying which step failed
**And** the original bundle is retained in blob storage as a fallback
**And** any candidates already split successfully before the failure are preserved

**Given** the bundle splits partially (some candidates extracted, some fail)
**When** the import session completes
**Then** successfully split documents are stored and linked
**And** failed extractions are recorded with specific error messages per candidate
**And** the import session status is "Completed" (not "Failed") with the partial results noted

**Given** PDF splitting is in progress
**When** the client polls the import session
**Then** the response includes splitting progress (e.g., "Splitting PDFs: 45 of 127")

**Given** a PDF bundle has already been uploaded for this recruitment
**When** a new bundle is uploaded
**Then** the new bundle's split documents replace any previous bundle documents
**And** individually uploaded documents (Story 3.5) are not affected

**Technical notes:**
- Infrastructure: `PdfSplitterService` (IPdfSplitter) — TOC parsing, page boundary detection via link annotations, PDF extraction
- Infrastructure: `BlobStorageService` (IBlobStorageService) — upload split PDFs to Azure Blob Storage
- NFR7: splitting up to 150 candidates / 100 MB completes within 60 seconds, runs asynchronously with progress
- NFR31: PDF bundle parsing validated against known Workday format, fails gracefully with clear error messages
- NFR32: documents stored in blob storage, not database
- Original bundle retained in blob storage as fallback
- FR28, FR29, FR30 fulfilled

## Story 3.5: CV Auto-Match, Manual Assignment & Individual Upload

As a **recruiting leader (Erik)**,
I want split CVs to be automatically matched to candidates by name, and to manually assign any unmatched CVs or upload individual PDFs per candidate,
So that every candidate has the correct CV document linked to their record.

**Acceptance Criteria:**

**Given** the PDF bundle has been split into individual documents (Story 3.4)
**When** the import pipeline runs the CV matching step
**Then** each split PDF is matched to an imported candidate by normalized name comparison (case-insensitive, whitespace-normalized)
**And** matched documents are linked to the corresponding `Candidate` via `CandidateDocument`

**Given** a split PDF's name matches exactly one candidate
**When** the auto-match runs
**Then** the document is automatically linked to that candidate
**And** the match is recorded in the import session as "auto-matched"

**Given** a split PDF's name does not match any candidate
**When** the auto-match completes
**Then** the document is stored in blob storage but remains unmatched
**And** the import session records it as "unmatched — manual assignment needed"

**Given** the import summary shows unmatched CVs
**When** the user views the unmatched documents section
**Then** each unmatched PDF shows the name extracted from the TOC
**And** a dropdown or search allows the user to select a candidate to assign it to
**And** only candidates without a document are suggested first, with all candidates available

**Given** the user assigns an unmatched PDF to a candidate
**When** they confirm the assignment
**Then** the document is linked to the selected candidate
**And** the unmatched count decreases
**And** the assignment is recorded in the import session

**Given** an active recruitment has a candidate without a document
**When** the user navigates to that candidate's detail or the candidate list
**Then** an "Upload CV" action is available for that candidate

**Given** the user uploads a PDF for an individual candidate
**When** the file is submitted
**Then** the system validates the file type (.pdf) and size
**And** the PDF is stored in Azure Blob Storage
**And** a `CandidateDocument` record is created linking the file to the candidate
**And** a success toast confirms the upload

**Given** a candidate already has a document
**When** the user uploads a new PDF for that candidate
**Then** the new document replaces the previous one
**And** the previous document is removed from blob storage

**Given** a closed recruitment exists
**When** the user views candidate documents
**Then** the "Upload CV" and manual assignment actions are not available

**Technical notes:**
- Import pipeline: name-based matching runs after PDF splitting, within the same import session
- Backend: `AssignDocumentCommand` (manual assignment of unmatched PDFs), `UploadDocumentCommand` (individual per-candidate upload)
- Frontend: manual assignment UI within `ImportSummary.tsx`, individual upload via `CandidateDetail.tsx`
- Name normalization: lowercase, trim whitespace, collapse multiple spaces, strip diacritics
- FR31, FR32, FR33 fulfilled

---

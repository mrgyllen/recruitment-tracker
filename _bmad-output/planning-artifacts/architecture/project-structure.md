# Project Structure & Boundaries

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains core decisions and enforcement guidelines._

## Complete Project Directory Structure

```
recruitment-tracker/
├── .github/
│   └── workflows/
│       └── ci.yml                          # Single pipeline: build + test api/ and web/
├── .gitignore
├── docs/                                    # Existing project documentation
│   └── chatgpt-research-intro.md
├── api/                                     # .NET Clean Architecture solution
│   ├── api.sln
│   ├── Directory.Build.props                # Shared MSBuild properties
│   ├── Directory.Packages.props             # Central package management
│   ├── .editorconfig                        # C# code style enforcement
│   ├── azure.yaml                           # azd deployment manifest
│   ├── src/
│   │   ├── Domain/
│   │   │   ├── Domain.csproj
│   │   │   ├── Common/
│   │   │   │   └── BaseEntity.cs
│   │   │   ├── Entities/
│   │   │   │   ├── Recruitment.cs
│   │   │   │   ├── WorkflowStep.cs
│   │   │   │   ├── Candidate.cs
│   │   │   │   ├── CandidateOutcome.cs
│   │   │   │   ├── CandidateDocument.cs
│   │   │   │   ├── RecruitmentMember.cs
│   │   │   │   ├── ImportSession.cs
│   │   │   │   └── AuditEntry.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── CandidateMatch.cs        # Import matching result (confidence, method)
│   │   │   │   └── AnonymizationResult.cs   # GDPR anonymization outcome
│   │   │   ├── Enums/
│   │   │   │   ├── OutcomeStatus.cs          # NotStarted, Pass, Fail, Hold
│   │   │   │   ├── ImportMatchConfidence.cs  # High, Low, None
│   │   │   │   ├── RecruitmentStatus.cs      # Active, Closed
│   │   │   │   └── ImportSessionStatus.cs    # Processing, Completed, Failed
│   │   │   ├── Events/
│   │   │   │   ├── CandidateImportedEvent.cs
│   │   │   │   ├── OutcomeRecordedEvent.cs
│   │   │   │   ├── DocumentUploadedEvent.cs
│   │   │   │   ├── RecruitmentCreatedEvent.cs
│   │   │   │   ├── RecruitmentClosedEvent.cs
│   │   │   │   └── MembershipChangedEvent.cs
│   │   │   └── Exceptions/
│   │   │       ├── RecruitmentClosedException.cs
│   │   │       ├── DuplicateCandidateException.cs
│   │   │       ├── InvalidWorkflowTransitionException.cs
│   │   │       └── StepHasOutcomesException.cs
│   │   ├── Application/
│   │   │   ├── Application.csproj
│   │   │   ├── Common/
│   │   │   │   ├── Interfaces/
│   │   │   │   │   ├── IApplicationDbContext.cs
│   │   │   │   │   ├── ITenantContext.cs
│   │   │   │   │   ├── ICurrentUserService.cs   # User identity contract (Clean Architecture)
│   │   │   │   │   ├── IBlobStorageService.cs
│   │   │   │   │   ├── IXlsxParser.cs
│   │   │   │   │   ├── IPdfSplitter.cs
│   │   │   │   │   └── ICandidateMatchingEngine.cs
│   │   │   │   ├── Behaviours/
│   │   │   │   │   ├── ValidationBehaviour.cs
│   │   │   │   │   ├── LoggingBehaviour.cs
│   │   │   │   │   └── AuditBehaviour.cs
│   │   │   │   ├── Models/
│   │   │   │   │   └── PaginatedList.cs
│   │   │   │   └── Mappings/
│   │   │   │       └── MappingExtensions.cs  # Shared ToDto() extension methods
│   │   │   └── Features/
│   │   │       ├── Recruitments/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── CreateRecruitment/
│   │   │       │   │   │   ├── CreateRecruitmentCommand.cs
│   │   │       │   │   │   ├── CreateRecruitmentCommandValidator.cs
│   │   │       │   │   │   └── CreateRecruitmentCommandHandler.cs
│   │   │       │   │   ├── UpdateRecruitment/
│   │   │       │   │   ├── CloseRecruitment/
│   │   │       │   │   ├── AddWorkflowStep/
│   │   │       │   │   └── RemoveWorkflowStep/
│   │   │       │   └── Queries/
│   │   │       │       ├── GetRecruitments/
│   │   │       │       ├── GetRecruitmentById/
│   │   │       │       └── GetRecruitmentOverview/
│   │   │       │           ├── GetRecruitmentOverviewQuery.cs
│   │   │       │           ├── GetRecruitmentOverviewQueryHandler.cs
│   │   │       │           └── RecruitmentOverviewDto.cs
│   │   │       ├── Candidates/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── CreateCandidate/
│   │   │       │   │   ├── RemoveCandidate/
│   │   │       │   │   ├── AssignDocument/
│   │   │       │   │   └── UploadDocument/         # Individual PDF upload (per candidate)
│   │   │       │   └── Queries/
│   │   │       │       ├── GetCandidates/          # Paginated list with batch SAS URLs
│   │   │       │       ├── GetCandidateById/       # Single candidate detail + SAS URL
│   │   │       │       └── SearchCandidates/
│   │   │       ├── Import/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── StartImport/            # Accepts XLSX + optional PDF bundle
│   │   │       │   │   └── ResolveMatchConflict/   # Manual review of low-confidence matches
│   │   │       │   └── Queries/
│   │   │       │       └── GetImportSession/        # Poll for progress + results
│   │   │       ├── Screening/
│   │   │       │   ├── Commands/
│   │   │       │   │   └── RecordOutcome/
│   │   │       │   └── Queries/
│   │   │       │       └── GetCandidateOutcomeHistory/
│   │   │       ├── Team/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── AddMember/
│   │   │       │   │   └── RemoveMember/
│   │   │       │   └── Queries/
│   │   │       │       ├── GetMembers/
│   │   │       │       └── SearchDirectory/        # Entra ID directory search
│   │   │       └── Audit/
│   │   │           └── Queries/
│   │   │               └── GetAuditTrail/
│   │   ├── Infrastructure/
│   │   │   ├── Infrastructure.csproj
│   │   │   ├── Data/
│   │   │   │   ├── ApplicationDbContext.cs
│   │   │   │   ├── Configurations/
│   │   │   │   │   ├── RecruitmentConfiguration.cs
│   │   │   │   │   ├── CandidateConfiguration.cs
│   │   │   │   │   ├── WorkflowStepConfiguration.cs
│   │   │   │   │   ├── CandidateOutcomeConfiguration.cs
│   │   │   │   │   ├── CandidateDocumentConfiguration.cs
│   │   │   │   │   ├── RecruitmentMemberConfiguration.cs
│   │   │   │   │   ├── ImportSessionConfiguration.cs
│   │   │   │   │   └── AuditEntryConfiguration.cs
│   │   │   │   ├── Migrations/
│   │   │   │   ├── Interceptors/
│   │   │   │   │   └── AuditableEntityInterceptor.cs
│   │   │   │   └── Seeds/
│   │   │   │       └── SeedData.cs              # Development seed data
│   │   │   ├── Services/
│   │   │   │   ├── BlobStorageService.cs         # Azure Blob + SAS token generation
│   │   │   │   ├── XlsxParserService.cs          # Workday XLSX parsing + column mapping
│   │   │   │   ├── PdfSplitterService.cs         # Bundle splitting + TOC extraction
│   │   │   │   ├── CandidateMatchingEngine.cs    # Email primary, name+phone fallback
│   │   │   │   ├── ImportPipelineHostedService.cs # IHostedService + Channel<T> consumer
│   │   │   │   └── GdprRetentionService.cs       # IHostedService with daily timer
│   │   │   └── Identity/
│   │   │       ├── TenantContext.cs               # ITenantContext implementation
│   │   │       ├── CurrentUserService.cs          # ICurrentUserService impl, extracts from JWT
│   │   │       └── EntraIdDirectoryService.cs     # Graph API for directory search
│   │   └── Web/
│   │       ├── Web.csproj
│   │       ├── Program.cs
│   │       ├── appsettings.json
│   │       ├── appsettings.Development.json
│   │       ├── Endpoints/
│   │       │   ├── RecruitmentEndpoints.cs
│   │       │   ├── CandidateEndpoints.cs         # Includes document upload endpoint
│   │       │   ├── ImportEndpoints.cs
│   │       │   ├── ScreeningEndpoints.cs
│   │       │   ├── TeamEndpoints.cs
│   │       │   └── AuditEndpoints.cs
│   │       ├── Middleware/
│   │       │   ├── ExceptionHandlingMiddleware.cs  # Global → Problem Details
│   │       │   └── TenantContextMiddleware.cs      # Populates ITenantContext from JWT
│   │       └── Configuration/
│   │           ├── DependencyInjection.cs
│   │           └── AuthenticationConfiguration.cs
│   ├── tests/
│   │   ├── Domain.UnitTests/
│   │   │   ├── Domain.UnitTests.csproj
│   │   │   └── Entities/
│   │   │       ├── RecruitmentTests.cs
│   │   │       ├── CandidateTests.cs
│   │   │       ├── WorkflowStepTests.cs
│   │   │       └── CandidateOutcomeTests.cs
│   │   ├── Application.UnitTests/
│   │   │   ├── Application.UnitTests.csproj
│   │   │   └── Features/
│   │   │       ├── Recruitments/
│   │   │       │   └── Commands/
│   │   │       │       └── CreateRecruitmentCommandTests.cs
│   │   │       ├── Candidates/
│   │   │       ├── Import/
│   │   │       ├── Screening/
│   │   │       └── Team/
│   │   ├── Application.FunctionalTests/
│   │   │   ├── Application.FunctionalTests.csproj
│   │   │   ├── CustomWebApplicationFactory.cs
│   │   │   └── Endpoints/
│   │   │       ├── RecruitmentEndpointTests.cs
│   │   │       ├── CandidateEndpointTests.cs
│   │   │       ├── ImportEndpointTests.cs
│   │   │       ├── ScreeningEndpointTests.cs
│   │   │       ├── TeamEndpointTests.cs
│   │   │       └── SecurityIsolationTests.cs   # Cross-recruitment isolation (mandatory)
│   │   └── Infrastructure.IntegrationTests/
│   │       ├── Infrastructure.IntegrationTests.csproj
│   │       ├── Data/
│   │       │   ├── TenantContextFilterTests.cs  # Global query filter verification
│   │       │   └── MigrationTests.cs
│   │       └── Services/
│   │           ├── BlobStorageServiceTests.cs
│   │           ├── XlsxParserServiceTests.cs
│   │           ├── PdfSplitterServiceTests.cs
│   │           ├── CandidateMatchingEngineTests.cs
│   │           └── GdprRetentionServiceTests.cs # High-risk operation coverage
│   └── aspire/
│       ├── AppHost/                             # Aspire orchestration project
│       │   ├── AppHost.csproj
│       │   └── Program.cs
│       └── ServiceDefaults/
│           ├── ServiceDefaults.csproj
│           └── Extensions.cs
├── web/                                         # Vite React app
│   ├── package.json
│   ├── package-lock.json
│   ├── tsconfig.json
│   ├── tsconfig.app.json
│   ├── tsconfig.node.json
│   ├── vite.config.ts                           # API proxy to localhost:5001
│   ├── index.html
│   ├── .env.example
│   ├── .eslintrc.cjs                            # Import ordering + code style
│   ├── .prettierrc
│   ├── public/
│   │   └── favicon.svg
│   ├── src/
│   │   ├── main.tsx                             # App entry point
│   │   ├── App.tsx                              # Root component + providers
│   │   ├── index.css                            # Tailwind CSS v4 imports
│   │   ├── vite-env.d.ts
│   │   ├── test-utils.tsx                       # Custom render with providers (QueryClient, Router, Auth)
│   │   ├── components/
│   │   │   ├── StatusBadge.tsx                  # Shared status indicator
│   │   │   ├── StatusBadge.types.ts
│   │   │   ├── StatusBadge.test.tsx
│   │   │   ├── ActionButton.tsx                 # Primary/Secondary/Destructive variants
│   │   │   ├── ActionButton.test.tsx
│   │   │   ├── EmptyState.tsx                   # Icon + text + action pattern
│   │   │   ├── EmptyState.test.tsx
│   │   │   ├── Toast/
│   │   │   │   ├── ToastProvider.tsx
│   │   │   │   ├── useToast.ts
│   │   │   │   └── Toast.test.tsx
│   │   │   ├── SkeletonLoader.tsx               # Loading placeholder
│   │   │   ├── ErrorBoundary.tsx                # Feature-level error boundary
│   │   │   └── PaginationControls.tsx           # Shared pagination
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   │   ├── AuthContext.tsx               # Auth state + token management
│   │   │   │   ├── AuthContext.test.tsx
│   │   │   │   ├── LoginRedirect.tsx
│   │   │   │   └── ProtectedRoute.tsx
│   │   │   ├── recruitments/
│   │   │   │   ├── RecruitmentList.tsx
│   │   │   │   ├── RecruitmentList.test.tsx
│   │   │   │   ├── CreateRecruitmentForm.tsx
│   │   │   │   ├── EditRecruitmentForm.tsx
│   │   │   │   ├── WorkflowStepEditor.tsx       # Add/remove/reorder steps
│   │   │   │   ├── CloseRecruitmentDialog.tsx
│   │   │   │   └── hooks/
│   │   │   │       ├── useRecruitments.ts
│   │   │   │       └── useRecruitmentMutations.ts
│   │   │   ├── candidates/
│   │   │   │   ├── CandidateList.tsx
│   │   │   │   ├── CandidateList.test.tsx
│   │   │   │   ├── CandidateDetail.tsx
│   │   │   │   ├── CreateCandidateForm.tsx
│   │   │   │   ├── ImportFlow/
│   │   │   │   │   ├── ImportWizard.tsx         # Multi-step import UX
│   │   │   │   │   ├── FileUploadStep.tsx
│   │   │   │   │   ├── ImportProgress.tsx       # Polling-based progress
│   │   │   │   │   ├── ImportSummary.tsx        # Created/updated/errored detail
│   │   │   │   │   ├── MatchReviewStep.tsx      # Low-confidence match review
│   │   │   │   │   └── WorkdayGuide.tsx         # FR55: Contextual export instructions
│   │   │   │   └── hooks/
│   │   │   │       ├── useCandidates.ts
│   │   │   │       └── useImportSession.ts
│   │   │   ├── screening/
│   │   │   │   ├── ScreeningLayout.tsx          # Split-panel container
│   │   │   │   ├── ScreeningLayout.test.tsx
│   │   │   │   ├── CandidatePanel.tsx           # Left panel: candidate list + nav
│   │   │   │   ├── PdfViewer.tsx                # Right panel: react-pdf + SAS URL
│   │   │   │   ├── OutcomeForm.tsx              # Outcome recording + reason
│   │   │   │   ├── OutcomeForm.test.tsx
│   │   │   │   └── hooks/
│   │   │   │       ├── useScreeningSession.ts   # Session state coordination
│   │   │   │       ├── usePdfPrefetch.ts        # Pre-fetch N+1, N+2
│   │   │   │       └── useKeyboardNavigation.ts # Keyboard-first screening
│   │   │   ├── overview/
│   │   │   │   ├── OverviewDashboard.tsx
│   │   │   │   ├── OverviewDashboard.test.tsx
│   │   │   │   ├── StepSummaryCard.tsx          # Per-step candidate count + stale indicator
│   │   │   │   ├── PendingActionsPanel.tsx
│   │   │   │   └── hooks/
│   │   │   │       └── useRecruitmentOverview.ts
│   │   │   ├── team/
│   │   │   │   ├── MemberList.tsx
│   │   │   │   ├── MemberList.test.tsx
│   │   │   │   ├── InviteMemberDialog.tsx       # Directory search + invite
│   │   │   │   └── hooks/
│   │   │   │       └── useTeamMembers.ts
│   │   │   └── audit/
│   │   │       ├── AuditTrail.tsx
│   │   │       └── hooks/
│   │   │           └── useAuditTrail.ts
│   │   ├── hooks/
│   │   │   ├── useDebounce.ts                   # Shared debounce for search
│   │   │   └── useFocusReturn.ts                # Focus management utility
│   │   ├── lib/
│   │   │   ├── api/
│   │   │   │   ├── httpClient.ts                # Base fetch wrapper + auth token + Problem Details + 401 redirect
│   │   │   │   ├── recruitments.ts
│   │   │   │   ├── recruitments.types.ts
│   │   │   │   ├── candidates.ts
│   │   │   │   ├── candidates.types.ts
│   │   │   │   ├── import.ts
│   │   │   │   ├── import.types.ts
│   │   │   │   ├── screening.ts
│   │   │   │   ├── screening.types.ts
│   │   │   │   ├── team.ts
│   │   │   │   ├── team.types.ts
│   │   │   │   ├── audit.ts
│   │   │   │   └── audit.types.ts
│   │   │   │   # No documents.ts — document operations are candidate-scoped (in candidates.ts)
│   │   │   └── utils/
│   │   │       ├── formatDate.ts                # Intl.DateTimeFormat wrapper
│   │   │       ├── problemDetails.ts            # RFC 9457 error parser
│   │   │       └── problemDetails.test.ts       # Critical utility coverage
│   │   ├── mocks/                               # MSW mock handlers for testing
│   │   │   ├── handlers.ts                      # Shared MSW request handlers
│   │   │   ├── server.ts                        # MSW server setup for tests
│   │   │   └── fixtures/                        # Reusable mock data
│   │   │       ├── recruitments.ts
│   │   │       ├── candidates.ts
│   │   │       └── importSessions.ts
│   │   └── routes/
│   │       └── index.tsx                        # React Router v7 route definitions
│   └── e2e/
│       ├── playwright.config.ts
│       └── specs/
│           ├── auth.spec.ts
│           ├── recruitment-crud.spec.ts
│           ├── import-flow.spec.ts
│           ├── screening-flow.spec.ts
│           └── team-management.spec.ts          # Invite/remove + access control
└── _bmad/                                       # BMAD workflow artifacts (existing)
└── _bmad-output/                                # BMAD output artifacts (existing)
```

## Architectural Boundaries

**API Boundaries:**

| Boundary | Enforced By | Pattern |
|----------|------------|---------|
| External → API | Entra ID OIDC + TLS | JWT bearer tokens on every request |
| API → Feature handlers | MediatR pipeline | Validation → Logging → Audit → Handler |
| Feature handlers → Data | EF Core via `IApplicationDbContext` | `ITenantContext` global query filters |
| API → Blob Storage | `IBlobStorageService` | SAS tokens generated server-side, never direct frontend access |
| API → Entra ID Directory | `EntraIdDirectoryService` | Graph API for member search only |
| User identity | `ICurrentUserService` | Application-layer interface, Infrastructure implementation extracts from JWT |

**Component Boundaries (Frontend):**

| Boundary | Isolation Method | Communication |
|----------|-----------------|---------------|
| Feature modules | Self-contained folders | Features never import from other features |
| Server state | TanStack Query cache | Features read/write via query keys, not shared state objects |
| URL state | React Router params | `recruitmentId`, `candidateId` in URL — source of truth |
| Auth state | React Context | `AuthContext` provides user identity, consumed by any feature |
| Screening session | Component-local + Context | Three panels coordinate via `useScreeningSession` hook, isolated re-renders |
| Test isolation | MSW mock server | `mocks/handlers.ts` provides consistent API mocking across all component tests |

**Cross-feature rule:** Features communicate only through URL params (navigation), TanStack Query cache (data), and shared contexts (auth). No direct imports between feature folders.

**Data Boundaries:**

| Boundary | Storage | Access Pattern |
|----------|---------|----------------|
| Structured data | Azure SQL | EF Core, global query filters via `ITenantContext` |
| Documents (PDFs) | Azure Blob Storage | Server-side SAS token generation, react-pdf rendering |
| Audit trail | Azure SQL (append-only) | Insert-only from `AuditBehaviour`, read via `GetAuditTrail` query |
| Session/cache | None (stateless API) | TanStack Query client-side cache; no server-side session state |

**Document Access Boundary:**

Batch SAS URLs are embedded in the `GetCandidates` query response (Decision #6 — no separate endpoint needed for screening). The `GetCandidateById` query includes a single SAS URL for the candidate detail view. Document upload is exposed via `CandidateEndpoints.cs` as `POST /api/recruitments/{id}/candidates/{candidateId}/document`. No standalone document endpoints — all document operations are scoped to a candidate within a recruitment.

## Requirements to Structure Mapping

**FR Category → Backend + Frontend Locations:**

| FR Category | Backend Location | Frontend Location |
|-------------|-----------------|-------------------|
| Authentication (FR1-3) | `Web/Middleware/`, `Infrastructure/Identity/` | `features/auth/` |
| Recruitment Lifecycle (FR4-13) | `Features/Recruitments/` | `features/recruitments/` |
| Team Management (FR56-61) | `Features/Team/` | `features/team/` |
| Candidate Import (FR14-26) | `Features/Import/`, `Infrastructure/Services/ImportPipelineHostedService.cs`, `XlsxParser*`, `CandidateMatching*` | `features/candidates/ImportFlow/` |
| CV Document Mgmt (FR28-35) | `Features/Candidates/Commands/UploadDocument/`, `Infrastructure/Services/PdfSplitter*`, `BlobStorage*` | `features/screening/PdfViewer.tsx`, `features/candidates/CandidateDetail.tsx` |
| Workflow & Outcomes (FR36-40) | `Features/Screening/`, `Domain/Entities/WorkflowStep.cs`, `CandidateOutcome.cs` | `features/screening/OutcomeForm.tsx` |
| Batch Screening (FR41-46) | `Features/Candidates/Queries/GetCandidates/` (batch SAS URLs), `Features/Screening/` | `features/screening/` (entire folder) |
| Overview & Monitoring (FR47-51) | `Features/Recruitments/Queries/GetRecruitmentOverview/` | `features/overview/` |
| Audit & Compliance (FR52-55) | `Features/Audit/`, `Application/Common/Behaviours/AuditBehaviour.cs` | `features/audit/`, `features/candidates/ImportFlow/WorkdayGuide.tsx` |
| Workflow Defaults (FR62-63) | `Domain/Entities/Recruitment.cs` (default step placement), `appsettings.json` (GDPR config) | N/A (server-side only) |

**Cross-Cutting Concerns → Locations:**

| Concern | Backend Location | Frontend Location |
|---------|-----------------|-------------------|
| Per-recruitment access | `Infrastructure/Identity/TenantContext.cs`, `Web/Middleware/TenantContextMiddleware.cs`, EF global query filters | React Router guards + TanStack Query scoped by `recruitmentId` |
| Audit trail | `Application/Common/Behaviours/AuditBehaviour.cs` (MediatR pipeline) | `features/audit/AuditTrail.tsx` (read-only display) |
| GDPR retention | `Infrastructure/Services/GdprRetentionService.cs` | N/A (background job only) |
| Error handling | `Web/Middleware/ExceptionHandlingMiddleware.cs` → Problem Details | `lib/utils/problemDetails.ts`, TanStack Query `onError`, `components/Toast/` |
| Accessibility | N/A | All `components/` (ARIA, keyboard, focus), `features/screening/hooks/useKeyboardNavigation.ts` |
| Input validation | `Application/Common/Behaviours/ValidationBehaviour.cs` (FluentValidation) | On-blur + on-submit field validation (frontend is UX convenience only) |
| Test infrastructure | `CustomWebApplicationFactory.cs` | `test-utils.tsx` (provider wrappers), `mocks/` (MSW handlers + fixtures) |

## Integration Points

**Internal Communication:**

```
Browser → (HTTPS) → Vite dev proxy → ASP.NET Core Minimal API
                                        ↓
                              MediatR Pipeline (Validation → Logging → Audit → Handler)
                                        ↓
                              EF Core (ITenantContext filtered) → Azure SQL
                              IBlobStorageService → Azure Blob Storage
```

**Background Processing:**

```
ImportEndpoints (POST /api/recruitments/{id}/import)
  → Returns 202 + import session ID
  → Writes to Channel<T>
  → ImportPipelineHostedService (IHostedService) consumes from channel
      → IXlsxParser → ICandidateMatchingEngine → EF Core upsert
      → IPdfSplitter → IBlobStorageService → name-based matching
      → Updates ImportSession entity (progress, results)

GdprRetentionService (IHostedService, daily timer)
  → Queries expired recruitments (ITenantContext.IsServiceContext = true)
  → Anonymizes PII, preserves aggregate metrics
  → Deletes blob documents
```

**External Integrations:**

| Integration | Direction | Protocol | Location |
|-------------|-----------|----------|----------|
| Microsoft Entra ID (auth) | Outbound | OIDC / OAuth 2.0 | `Web/Configuration/AuthenticationConfiguration.cs`, `features/auth/AuthContext.tsx` |
| Microsoft Entra ID (directory) | Outbound | Microsoft Graph API | `Infrastructure/Identity/EntraIdDirectoryService.cs` |
| Azure Blob Storage | Outbound | Azure SDK + SAS tokens | `Infrastructure/Services/BlobStorageService.cs` |
| Azure SQL | Outbound | EF Core / SQL | `Infrastructure/Data/ApplicationDbContext.cs` |
| Application Insights | Outbound | Aspire telemetry | `aspire/ServiceDefaults/Extensions.cs` |

**Data Flow — Import Pipeline (highest complexity):**

```
1. User uploads XLSX + PDF bundle
2. API validates files (type, size) → 400 if invalid
3. API creates ImportSession (Processing) → returns 202
4. API writes to Channel<T> → ImportPipelineHostedService picks up
5. XLSX parsing → candidate rows extracted (configurable column mapping)
6. Matching engine → email match (high confidence) or name+phone (low confidence)
7. Upsert candidates → created/updated/flagged counts tracked
8. PDF splitting → TOC parsed, individual PDFs extracted
9. PDF matching → normalized name comparison to candidates
10. Documents stored in Blob Storage → SAS URLs generated on read
11. ImportSession updated (Completed/Failed) with row-level detail
12. Frontend polls GET /api/import-sessions/{id} → renders summary
```

## File Organization Patterns

**Configuration Files:**
- Root: `.gitignore`, `.github/workflows/ci.yml`
- Backend: `appsettings.json` / `appsettings.Development.json` for non-secrets, Azure Key Vault for secrets
- Frontend: `.env.example` for documentation, `vite.config.ts` for build + API proxy
- Aspire: `AppHost/Program.cs` orchestrates Azure service connections in development

**Source Organization:**
- Backend follows Clean Architecture layers (Domain → Application → Infrastructure → Web), no cross-layer shortcuts
- Frontend follows feature-based organization, with shared components at the top level
- API client modules in `lib/api/` with co-located `.types.ts` files
- `httpClient.ts` is the single point for auth token attachment, Problem Details parsing, and 401 redirect

**Test Organization:**
- Backend: Separate test projects per layer (matches Jason Taylor template convention)
- Frontend: Co-located test files (`Component.test.tsx` next to `Component.tsx`)
- Frontend test utilities: `test-utils.tsx` provides custom render with all providers (QueryClient, Router, Auth)
- Frontend API mocks: `mocks/` directory with MSW handlers and reusable fixtures per feature
- E2E: Separate `e2e/` folder in `web/` with Playwright specs covering auth, CRUD, import, screening, and team management

## Development Workflow Integration

**Development Server Structure:**
- .NET API: `dotnet run --project api/src/Web` on `localhost:5001`
- Vite dev server: `npm run dev` in `web/` on `localhost:5173`
- Vite proxy: `/api/*` → `localhost:5001` configured in `vite.config.ts`
- Aspire (optional): orchestrates API + Azure SQL + Blob Storage emulation

**Build Process Structure:**
- Backend: `dotnet build api/api.sln` → `dotnet test` → `dotnet publish`
- Frontend: `npm run build` in `web/` → static files in `web/dist/`
- CI: Single GitHub Actions pipeline builds and tests both on every PR

**Deployment Structure:**
- API → Azure App Service (via `azd deploy` or GitHub Actions)
- Frontend → Azure Static Web Apps (built from `web/dist/`)
- Database → Azure SQL (EF Core migrations)
- Documents → Azure Blob Storage
- Secrets → Azure Key Vault (App Service native references)

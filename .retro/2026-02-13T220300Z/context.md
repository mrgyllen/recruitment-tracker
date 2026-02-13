# Evidence Bundle: Epic 1 Final Retrospective (Stories 1.4-1.5)

**Run ID:** 2026-02-13T220300Z
**Type:** Synthesis retro (covers stories 1.4-1.5, validates previous retro actions from 2026-02-13T002800Z)
**Scope:** Stories 1.4 (Shared UI Components & Design Tokens), 1.5 (App Shell & Empty State Landing)

---

## 1. Scope

### Story 1.4: Shared UI Components & Design Tokens
- **Acceptance Criteria:** 9 ACs — design tokens, shadcn/ui init, StatusBadge, ActionButton, EmptyState, Toast, SkeletonLoader, ErrorBoundary, all tested
- **Status:** Done (approved with minor notes)

### Story 1.5: App Shell & Empty State Landing
- **Acceptance Criteria:** 9 ACs — app shell header, React Router v7, ProtectedRoute, httpClient enhancement, TanStack Query, empty state landing, viewport guard, CSS Grid layout, skip-to-content link
- **Status:** Done (approved with minor notes)

---

## 1b. Previous Retro (2026-02-13T002800Z — Stories 1.1-1.3)

### Applied Actions
- A1 (security_hardening): Remove AddDefaultIdentity — applied
- A2 (docs_update): Document Fluent API for domain event collections — applied
- A3 (docs_update): Add template cleanup checklist — applied
- A4 (docs_update): Document middleware pipeline ordering — applied
- A5 (process_change): Enforce Dev Agent Record completion — applied
- A7 (guideline_gap): Promote anti-patterns to permanent — applied

### Deferred Items (still outstanding)
- **A6:** Consolidate IUser/ICurrentUserService — deferred (code change, 6 files). NOT addressed in stories 1.4-1.5 (frontend-only). Still outstanding for backend stories.
- **A8:** Fix Guid.ToString() type mismatch in query filter — NOT addressed (backend). Still outstanding.
- **A9:** Add domain event payload verification tests — NOT addressed (backend). Still outstanding.
- **A10:** Document CORS policy and same-origin assumption — NOT addressed. Still outstanding.

### Assessment
All deferred items are backend-focused. Stories 1.4-1.5 were frontend-only, so these items were not expected to be addressed. They remain valid for the next backend sprint.

---

## 2. Git Summary

**Commit range:** 34fd6d9..4a262fa (27 commits)
**Base:** 34fd6d9 (Add rule: no log entries in working files)
**Head:** 4a262fa (story 1.5 done: update sprint-status)

### Commits
```
4a262fa story 1.5 done: update sprint-status, capture anti-patterns from review
d307016 fix(1.5): ProtectedRoute calls login() instead of navigating to non-existent /login route
e433d4f feat(1.5): final verification, lint fixes, dev agent record
ad40d18 feat(1.5): update test-utils with QueryClient and MemoryRouter
85a30c2 feat(1.5): wire up React Router, QueryClient, and App shell
e1736f6 feat(1.5): add HomePage with onboarding empty state
09ff73a feat(1.5): add RootLayout with CSS Grid, skip link, ViewportGuard
395f89c feat(1.5): add AppHeader with user info and sign out
338e206 feat(1.5): add ViewportGuard with 1280px minimum and a11y
a51120d feat(1.5): add ProtectedRoute auth guard with tests
1d8bb40 feat(1.5): configure TanStack Query client with defaults
dc0c8e9 feat(1.5): install react-router v7 and tanstack query v5
475dbd7 story 1.4 done: update sprint-status, capture anti-patterns from review
33d18d0 fix(web): address review findings I1, I2, I3 for Story 1.4
7171bd7 docs: update Dev Agent Record for Story 1.4
f00c90b fix(web): resolve lint errors — import ordering and shadcn/ui eslint config
ac2545a feat(web): add Toaster provider to test-utils for component testing
527512f feat(web): add ErrorBoundary with default and custom fallback
8065911 feat(web): add SkeletonLoader with card/list-row/text-block variants
e7a88b0 feat(web): add toast system with success/error/info variants
2596365 feat(web): add EmptyState component with heading, description, and CTA
ac8dc50 feat(web): add ActionButton with primary/secondary/destructive variants
4039e5b feat(web): add StatusBadge component with all 5 variants and a11y tests
8d6cf34 feat(web): add vitest-axe for accessibility testing
f3bd8f6 feat(web): map shadcn/ui theme to If Insurance brand colors
9e5de53 feat(web): initialize shadcn/ui with all required base components
f971915 feat(web): add If Insurance brand design tokens via Tailwind v4 @theme
61fe3c0 feat(web): configure @/ path aliases for shadcn/ui
```

### Diffstat
- 64 files changed, 10,258 insertions, 248 deletions
- Story 1.4: ~30 files (design tokens, shadcn/ui components, custom components, tests)
- Story 1.5: ~20 files (routing, app shell, auth guard, viewport guard, homepage, tests)

### Changed Files (non-package-lock)
- Custom components: StatusBadge, ActionButton, EmptyState, ErrorBoundary, SkeletonLoader, Toast, AppHeader, ViewportGuard
- shadcn/ui components: 19 files in web/src/components/ui/
- Auth: ProtectedRoute, AuthContext update
- Routing: routes/index.tsx, RootLayout.tsx
- Feature pages: HomePage.tsx
- Infrastructure: queryClient.ts, utils.ts, test-utils.tsx, index.css, App.tsx
- Config: components.json, eslint.config.js, tsconfig changes, vite.config.ts

---

## 3. Quality Signals

### Test Results
- **16 test files, 90 tests, 90 passed, 0 failed**
- Duration: 3.06s
- Test files: StatusBadge (17 tests), ActionButton (11), EmptyState (8), Toast (6), SkeletonLoader (4), ErrorBoundary (4), ViewportGuard (4), AppHeader (4), ProtectedRoute (3), HomePage (4), routes (6), App (1+)

### Build Results
- `npm run build`: exit 0, built in 2.22s
- Output: index.html (0.50 kB), CSS (42.41 kB / 8.30 kB gzipped), JS (374.26 kB / 117.81 kB gzipped)

### Lint Results
- 0 errors, 6 warnings
- All warnings: import-x/order in test files with vi.mock (unavoidable — vi.mock must precede mocked imports)

### Code Coverage
- Not captured (no coverage tool configured)

---

## 4. Review Findings

### Story 1.4 Review
- **0 Critical, 3 Important, 4 Minor**
- I1: Toast auto-dismiss timing zero test coverage → FIXED (added duration spy tests)
- I2: ActionButton destructive "never solid red" assertion missing → FIXED (added bg-transparent assertion)
- I3: SkeletonLoader animate-pulse no prefers-reduced-motion → FIXED (added CSS rule)
- M1: useAppToast() vs useToast() naming deviation (accepted)
- M2: Dead @custom-variant dark in index.css (shadcn boilerplate)
- M3: Story task checkboxes still [ ]
- M4: StatusBadge icon tests generic (querySelector('svg'))

### Story 1.5 Review
- **1 Critical, 1 Important, 3 Minor**
- C1: ProtectedRoute redirected to non-existent /login route → FIXED (useEffect + login() from AuthContext)
- I1: Route test emitted 404 stderr (symptom of C1) → FIXED
- M1: AppHeader uses variant="secondary" instead of Ghost (Ghost deferred from 1.4)
- M2: Story task checkboxes still [ ]
- M3: 6 lint warnings in vi.mock test files

---

## 5. Anti-Patterns Discovered

### anti-patterns-pending.txt (current)
```
animate-pulse|web/src/**/*.tsx|animate-pulse must be disabled under prefers-reduced-motion — add CSS guard
duration|web/src/**/*.test.tsx|Toast/timer tests must assert exact duration values, not just render content
Navigate to="/login"|web/src/**/*.tsx|Do not use <Navigate> to auth routes — call login() from AuthContext instead
```

---

## 6. Guideline References

- `_bmad-output/planning-artifacts/architecture.md` — core decisions, enforcement guidelines
- `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` — UI consistency rules
- `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` — component structure, state management
- `_bmad-output/planning-artifacts/architecture/testing-standards.md` — test patterns
- `.claude/process/team-workflow.md` — development process
- `docs/testing-pragmatic-tdd.md` — testing policy

---

## 7. Sprint-Status Snapshot

```yaml
epic-1: in-progress  # All 5 stories done, needs manual transition to done
1-1-project-scaffolding-ci-pipeline: done
1-2-sso-authentication: done
1-3-core-data-model-tenant-isolation: done
1-4-shared-ui-components-design-tokens: done
1-5-app-shell-empty-state-landing: done
epic-1-retrospective: done  # Previous retro done, this is the final retro
```

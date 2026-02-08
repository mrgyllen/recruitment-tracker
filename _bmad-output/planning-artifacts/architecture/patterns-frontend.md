# Implementation Patterns — Frontend

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains core decisions and enforcement guidelines. See also [patterns-backend.md](./patterns-backend.md) for backend conventions and [frontend-architecture.md](./frontend-architecture.md) for architectural decisions._

_These patterns prevent AI agent implementation conflicts. All agents MUST follow these conventions._

## Naming Patterns

### React / TypeScript

| Element | Convention | Example |
|---------|-----------|---------|
| Components | PascalCase (file and export) | `CandidateList.tsx`, `OutcomeForm.tsx` |
| Hooks | `use` prefix, camelCase file | `useRecruitmentOverview.ts` |
| Utilities / helpers | camelCase file | `formatDate.ts`, `handleResponse.ts` |
| API modules | camelCase file | `recruitmentApi.ts`, `candidateApi.ts` |
| API types | camelCase `.types.ts` suffix | `recruitmentApi.types.ts`, `candidateApi.types.ts` |
| Types / interfaces | PascalCase, no `I` prefix | `Candidate`, `RecruitmentOverview` |
| Feature folders | kebab-case | `features/batch-screening/` |
| Constants | UPPER_SNAKE_CASE | `MAX_FILE_SIZE`, `STALE_THRESHOLD_DAYS` |

**Import statement ordering** (enforced via ESLint):

```
1. React/framework imports
2. Third-party libraries
3. Absolute imports (@/ alias)
4. Relative imports
5. Type-only imports
```

## Component Structure

```
web/src/
  features/
    auth/
    recruitments/
    candidates/
    screening/
    overview/
    team/
  components/              # Shared UI components (StatusBadge, ActionButton, Toast, etc.)
  hooks/                   # Shared hooks
  lib/
    api/                   # API client modules + co-located types
      recruitments.ts
      recruitments.types.ts
      candidates.ts
      candidates.types.ts
  routes/                  # Route definitions (thin layer)
```

**Rule: Frontend tests co-locate with source.** Test files sit next to the file they test (`Component.test.tsx`). Backend tests use separate projects (template convention).

**Rule: API types co-located with API modules.** Each API module has a corresponding `.types.ts` file defining request/response TypeScript types. When codegen replaces hand-typed clients, these files are the ones that get replaced.

## Loading States

| State | Pattern |
|-------|---------|
| Initial load | Skeleton placeholder matching final layout shape |
| Navigation | Instant (client-side routing, TanStack Query cache) |
| Mutation in progress | Disable submit button, show inline spinner |
| Optimistic update | Update TanStack Query cache immediately, rollback on error |
| Background refresh | No visible indicator (silent refetch on focus/interval) |

**Rule: Never show a full-page spinner.** Skeletons for initial loads, inline indicators for mutations, silent for background refreshes.

## Empty State Pattern

**Rule: Every list component has an empty state variant.** Empty states show:
- An illustration or icon (contextual)
- Explanatory text ("No candidates imported yet")
- A primary action ("Import from Workday" or "Add candidate")

Never a blank void, never "No data found," never a spinner for empty data.

**Special case — Onboarding (FR10):** The `RecruitmentList` empty state doubles as the first-time user experience. When no recruitments exist, this is the user's entry point to the application. The empty state here should include onboarding-quality guidance: what the app does, how to get started, and a prominent "Create your first recruitment" CTA. This is not a generic "No data" screen — it's the onboarding flow.

## Error Handling

| Scenario | Pattern |
|----------|---------|
| API errors | TanStack Query `onError`. Parse Problem Details. Toast for transient, inline for validation. |
| Render errors | React Error Boundary at feature level. Fallback UI + console log. |
| Network errors | TanStack Query retry (3 attempts for GET, no retry for mutations). Offline state if all fail. |

## Validation Timing

| Side | When | How |
|------|------|-----|
| Frontend | On blur + on submit | Field-level validation for immediate feedback |
| Backend | Before handler execution | FluentValidation pipeline behavior in MediatR |
| **Rule** | Backend is authoritative | Frontend validation is UX convenience, not security |

## UI Consistency Rules

### Status Indicators (Shared `StatusBadge` Component)

| Status | Color | Icon | Used For |
|--------|-------|------|----------|
| Not Started | Gray | Circle outline | Step/outcome not yet touched |
| In Progress | Blue | Half-filled circle | Step currently active |
| Approved/Pass | Green | Checkmark | Positive outcome |
| Declined/Fail | Red | X mark | Negative outcome |
| Hold | Amber | Pause icon | Deferred decision |
| Stale | Orange | Clock/warning | Time threshold exceeded (NFR27: shape+icon, not color only) |

**Rule: All status indicators use the shared `StatusBadge` component.** No feature creates its own status styling.

### Action Button Patterns (Shared `ActionButton` Component)

| Action Type | Style | Position |
|-------------|-------|----------|
| Primary (Create, Save, Import) | Filled, accent color | Bottom-right or top-right |
| Secondary (Cancel, Back) | Outlined | Left of primary |
| Destructive (Remove, Close Recruitment) | Red text or outlined red | Separated from primary actions |
| Navigation (View, Open) | Text link or ghost button | Inline |

### Toast Notifications (Shared `Toast` System)

| Type | Color | Duration | Use |
|------|-------|----------|-----|
| Success | Green | 3 seconds, auto-dismiss | Outcome saved, import started |
| Error | Red | Persistent until dismissed | API error, validation failure |
| Info | Blue | 5 seconds, auto-dismiss | Import complete, status change |

### Shared Components (Build First)

These shared components must exist before feature development begins:

```
components/
  StatusBadge.tsx           # Status indicator with color + icon
  StatusBadge.types.ts      # Status enum matching backend OutcomeStatus
  ActionButton.tsx          # Primary/Secondary/Destructive variants
  EmptyState.tsx            # Icon + text + action pattern
  Toast/
    ToastProvider.tsx
    useToast.ts
```

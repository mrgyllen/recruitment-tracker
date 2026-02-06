# Epic 5: Recruitment Overview & Monitoring

Erik and the team can see pipeline status at a glance — candidate counts per step, stale indicators, and pending actions — replacing status meetings.

## Story 5.1: Overview API & Data

As a **developer**,
I want a dedicated overview endpoint that returns pre-aggregated recruitment data including candidate counts per step, stale detection, and pending actions,
So that the frontend can render the overview dashboard without computing aggregations client-side.

**Acceptance Criteria:**

**Given** an active recruitment with candidates at various workflow steps
**When** the client calls `GET /api/recruitments/{id}/overview`
**Then** the response includes: total candidate count, candidate count per workflow step, outcome status breakdown per step (Not Started, Pass, Fail, Hold), pending action count (candidates with no outcome at their current step), and stale candidate count

**Given** the overview endpoint is called
**When** the server computes the response
**Then** data is aggregated via GROUP BY query (not pre-aggregated materialized views)
**And** the response returns within 500ms (NFR2)

**Given** a recruitment has candidates who have been at their current step for longer than the configurable stale threshold (default: 5 calendar days)
**When** the overview endpoint is called
**Then** the response includes per-step stale counts (number of candidates exceeding the threshold at each step)
**And** the stale threshold value is included in the response for display purposes

**Given** the overview endpoint is called
**When** the response is constructed
**Then** each workflow step entry includes: step name, step order, total candidates at step, candidates with no outcome (pending), candidates per outcome status, and stale count

**Given** a recruitment has no candidates
**When** the overview endpoint is called
**Then** the response returns zero counts for all metrics
**And** all workflow steps are still listed with zero counts

**Given** a user is not a member of the recruitment
**When** they call the overview endpoint
**Then** the API returns 403 Forbidden

**Given** the overview endpoint is called independently of the candidate list endpoint
**When** both are requested
**Then** each returns independently (no coupling between overview and candidate list queries)

**Technical notes:**
- Backend: `GetRecruitmentOverviewQuery` + handler, `RecruitmentOverviewDto`
- Computed via GROUP BY on candidates joined with workflow steps — no pre-aggregation at MVP scale (150 candidates, 7 steps = sub-100ms on Azure SQL)
- Stale threshold read from deployment configuration (appsettings.json, default: 5 days)
- FR47, FR48, FR49, FR50 fulfilled (data layer)

## Story 5.2: Overview Dashboard UI

As a **user (Erik)**,
I want a collapsible overview section at the top of the recruitment page showing KPI cards, a pipeline breakdown with per-step counts, stale indicators, and pending actions,
So that I can understand the pipeline status at a glance without clicking into individual candidates.

**Acceptance Criteria:**

**Given** the user navigates to a recruitment
**When** the page loads
**Then** the overview section is visible at the top of the page (unless previously collapsed)
**And** the overview data loads independently from the candidate list below (FR50)

**Given** the overview section is expanded
**When** the user views it
**Then** KPI cards display: total candidates, pending action count, and stale candidate count
**And** a pipeline breakdown shows candidate counts per workflow step as a horizontal bar or segmented display

**Given** the overview section is expanded
**When** the user clicks the collapse toggle
**Then** the overview collapses to an inline summary bar: "130 candidates - 47 screened - 3 stale"
**And** the collapse state is persisted to localStorage
**And** on next visit, the section loads in the persisted state

**Given** the overview section is collapsed
**When** the user clicks the summary bar or expand toggle
**Then** the full overview expands with KPI cards and pipeline breakdown

**Given** a workflow step has candidates exceeding the stale threshold
**When** the overview is displayed
**Then** the step shows a stale indicator using a clock icon + amber color (not color only, NFR27)
**And** the indicator shows the count: "5 candidates > 5 days"

**Given** the pipeline breakdown shows per-step counts
**When** the user clicks a step name or count
**Then** the candidate list below filters to show only candidates at that step
**And** the filter is applied without a page transition
**And** the active filter is visually indicated and clearable

**Given** the stale indicator is visible on a step
**When** the user clicks the stale indicator
**Then** the candidate list below filters to show only stale candidates at that step

**Given** the overview data is loaded
**When** an outcome is recorded in the screening flow below
**Then** the overview KPI cards and pipeline counts update via TanStack Query cache invalidation (background refetch, no loading state)

**Given** the overview section is rendered
**When** the user inspects it
**Then** KPI numbers use the 24px bold type scale
**And** all status indicators use the shared `StatusBadge` component
**And** the section respects the If Insurance brand palette (cream backgrounds, warm browns)

**Technical notes:**
- Frontend: `OverviewDashboard.tsx` (collapsible container), `StepSummaryCard.tsx` (per-step display with stale indicator), `PendingActionsPanel.tsx`
- Frontend: `useRecruitmentOverview.ts` hook (TanStack Query consuming overview API)
- Collapsible state via shadcn/ui Collapsible component + localStorage
- Overview refetches via TanStack Query cache invalidation when outcomes are recorded
- FR47, FR48, FR49, FR50 fulfilled (UI layer)

---

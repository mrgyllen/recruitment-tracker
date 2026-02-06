# Defining Experience

## The Core Interaction

**"Open a CV, record your assessment, next candidate -- without leaving the app."**

This is recruitment-tracker's defining interaction. The batch screening flow is where the app proves its value to the most skeptical user (the SME who didn't ask for this tool). If this flow is faster and simpler than downloading CVs from Teams and reporting back in chat, adoption succeeds. If it's not, the app fails regardless of how good the overview is.

The overview -- "open the app and know exactly where the recruitment stands" -- is the defining *outcome*. It's the continuous value that persists from day one through recruitment close, long after the initial screening burst is complete. The screening flow feeds the overview, and the overview gives the screening flow its purpose: every assessment Lina records makes the status picture more complete for everyone.

**The pair:** Screening is the engine, overview is the dashboard. Both are essential, but the screening flow is where adoption is won or lost.

## User Mental Model

**How users currently solve this:**
1. Erik downloads candidate data from Workday, organizes it in a spreadsheet or Teams channel
2. Erik shares CV files via Teams chat or a shared folder
3. Lina downloads CVs one at a time, reads them, then reports her assessment back via Teams message or email
4. Erik manually updates his tracking spreadsheet based on Lina's feedback
5. When someone asks "where do we stand?", Erik either checks his spreadsheet or schedules a meeting

**The mental model users bring:**
- Lina thinks of screening as "reviewing a stack of documents and making quick decisions." The mental model is a physical stack of papers -- pick one up, read it, put it in the Pass or Fail pile, pick up the next one.
- Erik thinks of status as "a board or spreadsheet with columns." Each column is a step, each row is a candidate, and cells show status.
- Anders thinks of status as "a summary someone tells me." He wants the conclusion, not the raw data.

**Where the app meets these mental models:**
- The screening flow mirrors "stack of papers" -- list of candidates on the left, document in the center, decision on the right. Pick one, read it, decide, move on.
- The overview mirrors "board with columns" -- steps as columns, candidate counts per step, status at a glance.
- The overview also serves Anders' "tell me the conclusion" model -- KPI cards at the top provide the summary without requiring him to interpret the pipeline breakdown.

**Where confusion might arise:**
- The concept of "steps" in a workflow is natural for engineers (they think in pipelines), but the terminology matters. "Step" is clearer than "stage" or "phase" for this audience.
- The relationship between recording an outcome (Pass/Fail/Hold) and the candidate moving to the next step -- this should be visually explicit. When Lina records "Pass," the candidate's position in the pipeline changes. The overview should reflect this immediately.

## Success Criteria

**The screening flow succeeds when:**
1. Lina can start screening within 10 seconds of opening the app (find the right recruitment, see the candidate list, click the first candidate, CV renders)
2. The CV renders inline without download -- this is the moment of "this is better"
3. Recording an outcome takes under 5 seconds of mechanical action (excluding CV reading time)
4. After recording, the next unscreened candidate loads with their CV pre-fetched -- no waiting
5. Lina never has to leave the app to complete her screening task -- no downloads, no separate reporting, no spreadsheet updates
6. At the end of a session, Lina's work is fully documented without any extra effort -- the outcomes and reasons ARE the documentation

**The overview succeeds when:**
1. Erik can answer "where does the recruitment stand?" in under 10 seconds without clicking into anything
2. Anders sees the same information as Erik and interprets it correctly without training
3. Counts on the overview exactly match filtered candidate lists -- zero trust-breaking discrepancies
4. Stale candidates (stuck at a step too long) are visually flagged without Erik needing to manually check timestamps
5. The overview updates in real-time as outcomes are recorded -- Erik sees progress during an active screening session (via TanStack Query cache invalidation on outcome mutations, background refetch, no visible loading state)

## Novel vs. Established Patterns

**The screening flow uses entirely established patterns in a focused combination:**
- Split-panel layout (VS Code, email clients) -- proven for "list + content + action"
- Inline document viewer (email attachment preview, Google Docs) -- proven for "read without downloading"
- Keyboard shortcuts for repeated actions (VS Code, terminal) -- proven for power-user throughput
- Optimistic UI with undo (Gmail "undo send," Google Docs) -- proven as faster than confirmation dialogs

**No novel interaction patterns are needed.** The innovation is in the combination and focus: all four patterns combined into a single, purpose-built flow for CV screening. Each pattern is individually familiar to engineering users. The app doesn't need to teach new interactions -- it needs to execute known patterns flawlessly.

**The overview uses established dashboard patterns:**
- KPI cards (Grafana, Azure Monitor, any analytics dashboard) -- proven for "summary at a glance"
- Pipeline/funnel visualization (sales dashboards, CI/CD dashboards) -- proven for "stage-based status"
- Collapsible sections (VS Code, any modern web app) -- proven for "show/hide based on current focus"

**The unique twist:** Combining screening and overview on a single page so that every action in the screening flow immediately updates the overview. There's no separate "dashboard" to navigate to -- the status is always visible (or one collapse-toggle away). This collapses the gap between "doing the work" and "seeing the result of the work."

## Experience Mechanics

### Screening Flow -- Step by Step

**1. Initiation: Getting to the screening flow**

The page always shows the three-panel layout structure. Before any candidate is selected, the center and right panels show a "Select a candidate to begin" empty state. This avoids a jarring layout shift when the first candidate is clicked -- the structure is always there, selection just populates it.

```
App opens → Single-page loads:
  - Overview section (top, collapsible)
  - Three-panel layout (always present):
    - Left: Candidate list (sidebar width)
    - Center: Empty state ("Select a candidate to review their CV")
    - Right: Empty state (outcome controls appear on selection)
→ Candidate list shows all candidates at the current step (or filtered)
→ Unscreened candidates are visually distinct from those with outcomes
→ Lina clicks any candidate (no forced order)
→ Center panel populates with CV, right panel shows outcome controls
```

**2. Interaction: The screening split-panel**

```
┌─────────────────────────────────────────────────────────────┐
│ [Recruitment Name] > [Step Name]            [Collapse ▲]    │
│ ┌──────────┐ ┌──────────────────────┐ ┌──────────────────┐  │
│ │Candidate │ │                      │ │ Candidate Name   │  │
│ │List      │ │    CV PDF Viewer     │ │ Status: New      │  │
│ │          │ │                      │ │                  │  │
│ │• Name A  │ │  [Page 1 of 3]       │ │ ┌──────────────┐ │  │
│ │  Name B ◄│ │                      │ │ │ Pass    (1)  │ │  │
│ │• Name C  │ │                      │ │ │ Fail    (2)  │ │  │
│ │• Name D  │ │                      │ │ │ Hold    (3)  │ │  │
│ │• Name E  │ │                      │ │ └──────────────┘ │  │
│ │          │ │                      │ │                  │  │
│ │          │ │                      │ │ Reason:          │  │
│ │          │ │                      │ │ ┌──────────────┐ │  │
│ │          │ │                      │ │ │              │ │  │
│ │ 47/130   │ │                      │ │ └──────────────┘ │  │
│ │ screened │ │                      │ │                  │  │
│ │ 12 this  │ │                      │ │ [Enter] Confirm  │  │
│ │ session  │ │                      │ │                  │  │
│ └──────────┘ └──────────────────────┘ └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

- **Left panel (candidate list, min 250px):** Scrollable list of candidates at this step. Current candidate highlighted. Unscreened candidates marked with a dot or indicator. Shows screening progress: total ("47 of 130 screened") and session ("12 this session" -- client-side `useState` counter, resets on page refresh). Sortable by name, import order, or status. Click any candidate to switch. Default sort: import order.
- **Center panel (CV viewer, flexible width):** PDF rendered inline via react-pdf. Page 1 renders immediately. Scroll to see additional pages. Scroll position preserved per-candidate if navigating back. Resizable divider between left and center panels (custom `useResizablePanel` hook, localStorage-persisted).
- **Right panel (outcome controls, fixed ~300px):** Candidate name and current status. Three outcome buttons with embedded shortcuts: "Pass (1)", "Fail (2)", "Hold (3)". Always-visible reason textarea. Confirm button with Enter label.

**Keyboard shortcut scoping:** `1`/`2`/`3` shortcuts are handled via a keydown listener on the outcome panel, filtered to ignore events when the active element is a text input or textarea. This means pressing `1` while typing in the reason field types the character `1`, not selecting Pass. Shortcuts are only active when focus is on the outcome panel (buttons or non-input elements).

**3. Feedback: Outcome recording and confirmation**

**Fastest path (outcome without reason):**
```
Focus on outcome panel → press 1 → Pass button highlighted
→ press Enter → outcome confirmed
→ Optimistic UI: outcome applied immediately
→ Bottom-right toast slides in: "✓ Pass recorded for [Name] · Undo"
→ Auto-advance to next unscreened candidate
→ Focus returns to outcome panel → 1/2/3 shortcuts immediately active
```

**Path with reason:**
```
Focus on outcome panel → press 1 → Pass button highlighted
→ Tab → focus moves to reason textarea
→ type reason (1/2/3 keys type normally in textarea)
→ Tab → focus moves to confirm button
→ Enter → outcome confirmed with reason
→ Same confirmation + auto-advance flow
```

**Confirmation and undo mechanics:**
```
Outcome confirmed →
→ Optimistic UI: outcome shown immediately in client
→ Bottom-right toast slides in from right (~150ms): "✓ Pass recorded for [Name] · Undo"
→ Toast auto-dismisses after 3 seconds
→ During those 3 seconds: clicking Undo reverses the action (no API call needed)
→ After 3 seconds: outcome persists to server via API call
→ Candidate list updates: candidate shows Pass status badge (green + checkmark)
→ Overview KPI cards update via TanStack Query cache invalidation (background refetch, no loading state)
→ Focus moves to outcome panel for next candidate → shortcuts immediately active
```

**Auto-advance logic:**
- Advances to the next unscreened candidate *below* the current one in the visible list order
- If no unscreened candidates below, wraps to the top of the list
- If all candidates in the current filter are screened, stays on the current candidate and shows a completion indicator
- The list order is determined by Lina's chosen sort (default: import order) -- auto-advance follows that order
- Lina can always override auto-advance by clicking any candidate in the list

**If Lina wants to pick a specific candidate (override auto-advance):**
```
After outcome confirmed → instead of waiting for auto-advance,
Lina clicks a different candidate in the left panel
→ Selected candidate loads instead of auto-advance target
→ Focus moves to outcome panel for the newly selected candidate
```

**4. Completion: End of screening session**

```
Lina finishes screening (all candidates assessed, or she's done for now)
→ Candidate list shows all outcomes recorded with status badges
→ Progress shows "130/130 screened" or "47/130 screened"
→ No explicit "end session" action needed
→ Closing the browser or navigating away is fine
→ All outcomes already persisted (optimistic UI committed after 3-second windows)
→ Overview section (if visible) already shows updated pipeline counts
```

### Overview Flow -- Step by Step

**1. Initiation: Seeing the status**

```
App opens → Overview section visible at top of page (unless previously collapsed)
→ KPI cards show: Total candidates, Pending action, Stale count
→ Pipeline breakdown shows per-step candidate counts
→ All data current as of page load (TanStack Query cache)
```

**2. Interaction: Drilling down**

```
Erik sees "23 at Technical Interview" in the pipeline breakdown
→ Clicks the step name or count
→ Candidate list below filters to show those 23 candidates
→ Each candidate shows name, status, outcome at previous step, time at current step
→ Erik can click any candidate to see detail (CV, outcome history)
```

**3. Stale detection:**

```
Overview pipeline shows a stale indicator (shape + icon, not color-only) next to "Screening"
→ Tooltip or inline text: "5 candidates have been at Screening for >7 days"
→ Erik clicks → candidate list filters to those 5 stale candidates
→ Erik can take action (remind Lina, reassign, record outcome himself)
```

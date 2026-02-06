# Core User Experience

## Defining Experience

The core experience of recruitment-tracker shifts across the recruitment lifecycle, but two interactions define its value:

**Primary loop (early recruitment): Batch CV screening**
Erik imports candidates. Lina opens the app, sees her screening queue, and enters a focused flow: candidate list on the left, CV viewer in the center, outcome controls on the right. She reads a CV, records Pass/Fail/Hold with a brief reason, and the next candidate loads with a brief visual confirmation transition (~300ms) before auto-advancing. No file downloads, no switching between Teams and Excel, no reporting back via chat. The screening session IS the documentation.

**Primary loop (ongoing recruitment): Status check and alignment**
Any team member -- Erik, Lina, or Anders -- opens the overview. Per-step candidate counts tell them where the pipeline stands. Stale indicators flag candidates stuck too long at a step. Everyone sees the same truth at the same time, eliminating the need for synchronous status meetings. The app replaces "let me schedule a meeting so everyone has the same information" with a shared, always-current status board accessible to all roles.

**The core action that everything depends on: outcome recording.** Every meaningful state change in the system flows through a single interaction -- selecting an outcome (Pass/Fail/Hold) and optionally writing a reason. If this interaction is fast and frictionless, screening sessions flow, status stays current, and the overview stays accurate. If it's slow or cumbersome, users fall back to Teams.

## Platform Strategy

- **Desktop-first web application** -- 1280px minimum viewport width, optimized for laptop/desktop screens
- **Mouse and keyboard interaction** -- keyboard shortcuts for power users (outcome recording, candidate navigation), mouse for everything else
- **Keyboard shortcut mapping** -- `1`/`2`/`3` for Pass/Fail/Hold, `Tab` to move focus to reason field, `Enter` to confirm and advance. Chosen to avoid conflicts with browser defaults and accessibility tools
- **Target browsers**: Microsoft Edge (primary, corporate standard), Chrome (secondary)
- **No offline requirement** -- users are always on corporate network
- **Authentication**: Microsoft Entra ID SSO -- zero-friction entry, no separate credentials
- **PDF viewing**: In-app PDF renderer for CV display -- eliminating the download-open-read-close cycle is the single biggest friction reduction over the current process
- **No mobile requirement** -- recruitment work happens at desks, not on phones

## Effortless Interactions

These interactions must feel instant and require zero cognitive overhead:

1. **Outcome recording** -- Select Pass/Fail/Hold, optionally type a reason, done. One interaction, candidate advances. The reason field is always visible (not hidden behind an "add note" button) -- visible fields get used, hidden fields get ignored. This is what makes "the app is the documentation" work. Keyboard shortcuts (`1`/`2`/`3`, `Tab`, `Enter`) enable power-user throughput.

2. **Next candidate in screening** -- After recording an outcome, a brief confirmation transition (~300ms) acknowledges the action before the next candidate loads with their CV pre-fetched. The system pre-fetches the next 2-3 candidate CVs via SAS-token URLs while the current CV is being reviewed. No navigation, no clicking, no waiting for PDF render.

3. **Status comprehension** -- Opening the overview answers "where does the pipeline stand?" in a single glance for any role. No clicking into details, no mental math, no scrolling. Candidate counts per step, stale indicators, and pipeline health are all visible on first load. Every team member sees the same information -- this is what replaces the sync meeting.

4. **CV access** -- Click a candidate, see their CV. No download dialog, no file explorer, no waiting. The PDF renders inline. Scroll position is preserved per-candidate if the user navigates back. The split-panel divider is resizable so users can give the CV viewer more space for dense documents, and the preference persists across the session.

5. **SSO login** -- Corporate credentials, one click, you're in. No registration, no password setup, no email verification.

## Critical Success Moments

**Make-or-break moment #1: Lina's first screening session**
Lina opens the app for the first time, sees candidates assigned to her step, clicks one, and the CV appears inline. She records Pass with a reason, and the next candidate loads after a brief confirmation. If she thinks "I didn't have to download anything, and my assessment is already documented" -- adoption succeeds. The speed gain isn't in reading CVs faster; it's in eliminating the overhead around the reading: no downloads, no app-switching, no reporting back via Teams.

**Make-or-break moment #2: Erik checks status without scheduling a meeting**
Erik opens the overview after Lina has screened a batch. He can see how many candidates passed, how many are waiting for the next step, who's been idle too long. He knows what to do next without asking anyone. This is the moment where the app proves it replaces the "let me get everyone on a call to align" pattern.

**Make-or-break moment #3: Everyone sees the same truth**
Anders opens the overview from a link Erik shared. Lina opens the app to start her next screening session and glances at the overview first. They all see the same candidate counts, the same pipeline state, the same stale indicators. No one needs to ask "where are we?" -- the app answers it for every role. This is the core value that eliminates the fragile, meeting-dependent alignment of the current process.

**First-time success: Erik's initial import**
Erik's first action is importing candidates. If the XLSX upload, PDF matching, and candidate creation flow takes under 5 minutes and he can verify the results make sense, he trusts the system. If matching is confusing or requires too many manual corrections, he questions whether the app is worth the setup effort.

## Experience Principles

1. **Screening flow eliminates overhead, not reading time** -- The SME reads a CV at the same speed regardless of tool. What the app eliminates is everything around the reading: downloading files, switching apps, reporting back via chat, updating spreadsheets. Every design decision for the screening flow minimizes this overhead: inline PDFs, pre-fetching, visible reason field, keyboard shortcuts, auto-advance with confirmation.

2. **Status should be self-evident to every role** -- No one should ever need to schedule a meeting to ask "where do we stand?" The overview exists so that question never gets asked, regardless of whether you're the recruiting leader, the SME, or the stakeholder. Same data, same view, same truth. This is the feature that replaces synchronous alignment meetings.

3. **The app is the documentation** -- Every outcome recorded, every reason written, every status change captured. The act of using the app IS the act of documenting the recruitment. There is no separate "update the tracker" step. The freeform reason field on outcomes is the MVP documentation mechanism -- if during testing it proves insufficient, that's the signal to promote richer notes to the next phase.

4. **Zero-friction entry, zero-friction action** -- SSO gets you in, one click shows the CV, one interaction records the outcome. The reason field is visible by default, not hidden. Every additional click, modal, or confirmation dialog is a failure of design. The default path should require the minimum possible interactions.

5. **Guide infrequent users, empower power users** -- Erik imports candidates twice a year (guided wizard). Lina screens 130 candidates in a session (keyboard shortcuts, auto-advance). The same app serves both usage patterns without either feeling patronized or neglected.

# Executive Summary

## Project Vision

recruitment-tracker is a focused execution companion for hiring teams, filling the gap between Workday (system of record) and the day-to-day work of running a recruitment. The core UX promise is "the team collaborates without the leader as bottleneck" -- replacing manual information relay via Teams/email/spreadsheets with a shared status board where every participant self-serves.

The application is desktop-first (1280px minimum viewport), targets Edge and Chrome, and uses Microsoft Entra ID SSO for zero-friction onboarding. It is not an HR system, not a project management tool, and not a Workday replacement. Every UX decision must pass the filter: "Does this help run the hiring?"

## Target Users

**Erik (Recruiting Leader)** -- Line manager running 2-3 recruitments per year alongside regular leadership duties. Comfortable with standard business tools but has zero patience for complex systems. If it takes more than a few clicks, he'll fall back to Teams. Design principle: "Every screen answers: what needs my attention?"

**Lina (SME/Collaborator)** -- Senior engineer pulled in for technical assessment. Recruitment is not her primary job. Very tech-savvy but demands efficiency -- the app must be faster and simpler than the Teams chat it replaces. Design principle: "Complete your task in one focused session."

**Anders (Viewer)** -- Stakeholder who wants pipeline visibility without asking anyone. Needs the full picture in under 10 seconds without clicking into details. Design principle: "Glance and know."

**Sara (HR Partner, future)** -- RBAC role defined, no features built. The goal is to build something so useful that Sara asks to be included. Design principle: "Build pull, don't push adoption."

## Key Design Challenges

1. **Batch screening at scale** -- The split-panel layout (candidate list + PDF viewer + outcome form) must keep Lina in flow across 130+ candidates with keyboard-first navigation, zero page reloads, and PDF pre-fetching. This is the hardest UX problem and the make-or-break adoption moment for SMEs.

2. **Import flow complexity** -- Multi-step process (XLSX + PDF bundle upload, matching review, manual assignment) that must feel simple despite significant backend complexity. Erik does this infrequently, so the UX must guide rather than assume learned behavior.

3. **Information density without overwhelm** -- The overview dashboard serves three different "glance and know" needs: Erik's "what needs my attention?", Lina's "what's assigned to me?" (Growth), and Anders' "where does the pipeline stand?". Balancing density with clarity across these use cases is critical.

4. **Empty state as onboarding** -- Erik's first experience is an empty application. The empty state must serve as onboarding-quality guidance, taking him from zero to a live recruitment with imported candidates in under 5 minutes.

## Design Opportunities

1. **Keyboard-first screening as power-user differentiator** -- If the batch screening flow lets Lina screen candidates faster than downloading CVs from Teams and reporting back in chat, the app becomes indispensable. Target: 4 candidates in under 10 minutes including CV review.

2. **Visual pipeline clarity for organic adoption** -- A well-designed overview with per-step counts, stale indicators (shape+icon, not color-only per WCAG), and health status makes recruitment status instantly shareable. This is the mechanism for Sara's organic adoption: Erik shares his screen, Sara sees everything in 10 seconds, and asks for access.

3. **Progressive disclosure in import flow** -- Wizard-style import with clear steps (upload, review matches, confirm) hides complexity while surfacing manual intervention points (low-confidence matches, unmatched CVs) only when needed.

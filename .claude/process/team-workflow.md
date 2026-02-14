# Team Workflow Process

This document defines HOW the team operates. The prompt says WHAT to build; this document says HOW.

## Getting Started

Before creating the team or assigning any work:

1. **Read `sprint-status.yaml`** to determine current state — which stories are done, in-progress, or ready-for-dev
2. **Read the epic file** referenced in the prompt to understand the full story list
3. **Determine scope:** If the prompt specifies stories (e.g., "stories 1.4 and 1.5"), implement only those. If the prompt says "Continue" or "Implement Epic X" without specific stories, implement all remaining stories that are `ready-for-dev`
4. **Check `.retro/`** for previous retro runs — note any deferred items that should be addressed
5. **Create the team** and begin the Story Cycle with the first in-scope story

## Team Structure

| Role | Agent | Responsibility | Rules |
|------|-------|---------------|-------|
| **Team Lead** | Orchestrator | Assigns work, coordinates handoffs, manages sprint-status | NEVER implements. NEVER reviews code. Delegates only. |
| **Dev Agent** | general-purpose | Plans and implements stories using Superpowers skills | See Dev Agent skills below |
| **Review Agent** | general-purpose | Structured code review with blocking authority | See Review Agent skills below |

### Dev Agent Skills

| Skill | When |
|-------|------|
| `writing-plans` | Before implementation — create implementation plan from story |
| `test-driven-development` | During implementation — write tests before code |
| `verification-before-completion` | Before declaring done — run tests, verify output |
| `requesting-code-review` | Before handoff to review — self-check for obvious issues |
| `receiving-code-review` | After receiving review findings — evaluate feedback critically before implementing fixes |

### Review Agent Skills

| Skill | When |
|-------|------|
| `bmad-bmm-code-review` | Performing the review — adversarial review that finds 3-10 specific problems |

**Additional rule:** Review Agent MUST run `git log --oneline -5` before starting any review to identify the latest commit.

## Story Cycle

Repeat this cycle for each story in the sprint:

### Step 1: Dev Agent Plans + Implements

1. Dev Agent reads the story file and all required architecture shards
2. Dev Agent uses `writing-plans` skill to create an implementation plan
3. Dev Agent implements using `test-driven-development` skill
4. Dev Agent runs `verification-before-completion` before declaring done
5. Dev Agent commits work and sends message to Team Lead

### Step 2: Review Agent Reviews

1. Review Agent MUST run `git log --oneline -5` first to identify the latest commit
2. Review Agent reads the story file for acceptance criteria
3. Review Agent reads relevant architecture shards
4. Review Agent performs structured review checking:
   - Architecture compliance (see Verification Checkpoints in patterns-backend.md)
   - Test coverage and quality
   - Security patterns (ITenantContext, global query filters)
   - **All command/query handlers verify ITenantContext membership before data access** (see patterns-backend.md Handler Authorization section)
   - Naming conventions and file placement
   - DDD aggregate rules
   - **Dev Agent Record section is non-empty** (must contain: testing mode rationale, key decisions, file list)
5. Review Agent categorizes findings:
   - **Critical:** Must fix before proceeding (security holes, broken invariants, missing tests)
   - **Important:** Should fix in this story (pattern violations, naming errors)
   - **Minor:** Note for future (style preferences, optimization opportunities)
6. Review Agent sends findings to Team Lead

**IMPORTANT:** Delivering findings is NOT approval. If there are Critical or Important findings, this begins the fix cycle — the story is NOT approved.

### Step 3: Fix Cycle (if needed)

- Critical or Important findings: Dev Agent fixes, commits, Review Agent re-verifies
- Minor findings only: Story proceeds to approval
- **Rule:** Never proceed with unresolved Critical or Important findings
- The fix cycle repeats until Review Agent has zero Critical/Important findings

### Step 4: Review Agent Approval

Review Agent sends an **explicit approval message** — one of:
- **"APPROVED"** — no remaining issues
- **"APPROVED WITH MINOR NOTES"** — only Minor findings remain (noted for retro)

This is a distinct event from delivering findings. Findings with Critical or Important items are NEVER approval.

**Team Lead MUST NOT consider the review complete until this explicit approval is received.** See Task Structure below for how to enforce this with task dependencies.

### Step 5: Story Completion (after Review Agent explicit approval)

After Review Agent explicitly approves the story, Team Lead runs the Story Completion Checklist:

- [ ] All Critical/Important findings resolved
- [ ] Dev Agent committed fixes
- [ ] Review Agent re-verified and sent explicit "APPROVED" or "APPROVED WITH MINOR NOTES"
- [ ] Sprint-status updated to `done`
- [ ] Mini-retro completed (anti-patterns captured)
- [ ] Only THEN create/assign next story's tasks

1. **Mini-retro:** Review any new anti-patterns discovered during this story
   - Add new entries to `.claude/hooks/anti-patterns-pending.txt`
   - Format: `REGEX|FILE_GLOB|MESSAGE`
2. **Update sprint-status:** Mark story status as `done` in `sprint-status.yaml`
3. **Commit:** Commit sprint-status update

## Task Structure for Story Cycle

Each story MUST be split into separate tasks with explicit dependencies. This prevents premature advancement.

```
Task A: "Implement Story X.Y"        (Dev Agent)
Task B: "Review Story X.Y"           (Review Agent) — blockedBy: [A]
Task C: "Approve Story X.Y"          (Review Agent) — blockedBy: [B]
Task D: "Implement Story X.Z"        (Dev Agent)    — blockedBy: [C]  <-- gates on APPROVAL, not findings
```

**Key rules:**
- Task B (Review) is completed when findings are delivered — this does NOT unblock the next story
- Task C (Approve) is completed ONLY when Review Agent sends explicit "APPROVED" — this unblocks the next story
- If findings have Critical/Important items, a fix task is created between B and C
- Task D (next story) is ALWAYS blocked by Task C (approval), never by Task B (review)

**Dev Agent guardrail:** NEVER start a new story task unless the Team Lead explicitly assigns it to you with a message. Do not self-assign based on task list availability alone.

## Autonomous Retrospective

Run after the **last story in the current prompt scope** is completed. Fully autonomous — no human interaction, no "please confirm". Every claim must cite evidence.

**When to run:**
- The prompt specifies which stories to implement (e.g., "stories 1.1, 1.2, and 1.3")
- Run the retro after the last story in that scope — even if the sprint has more stories
- This is a **mid-sprint retro** if the sprint continues with more stories later
- A **final retro** runs after the last story of the entire sprint

**Retro continuity (mid-sprint → final):**
- If a previous retro exists in `.retro/`, the new retro MUST read it in Phase 1 (evidence assembly)
- The new retro skips stories already covered by a previous retro
- The new retro checks whether deferred items from previous retros were addressed
- The final retro is a **synthesis retro** — covers new stories plus validates all previous retro actions were applied
- **Previous deferred items MUST be resolved** — a deferred item surviving two retros is a process failure. If the item is still valid, fix it NOW regardless of scope. If it's no longer relevant, explicitly close it with justification.

**CRITICAL: Retro scope ≠ fix scope.** The retro EVALUATES stories in scope, but it FIXES everything outstanding — including items from previous retros and items outside the current story domain. A frontend retro that finds outstanding backend doc updates must apply them. The only valid reason to skip a fix is if it would destabilize code that wasn't touched in this sprint.

### Phase 1: Evidence Assembly

Team Lead builds the evidence bundle at `.retro/<run_id>/context.md` where `<run_id>` is an ISO 8601 timestamp (e.g., `2026-02-12T120000Z`).

The evidence bundle MUST include:

1. **Scope:** Story identifiers, short titles, acceptance criteria
1b. **Previous retros (if any):** If `.retro/` contains previous retro runs, include a summary of their action items and deferred items. Note which were applied and which are still outstanding.
2. **Git summary:** Commit range (base..head), diffstat, changed files list, commit subjects
   ```bash
   git log --oneline <base>..<head>
   git diff --stat <base>..<head>
   git diff --name-only <base>..<head>
   ```
3. **Quality signals** (best-effort):
   - Test results (summary + any failures)
   - Lint/typecheck results if available
   - Build results if available
4. **Review findings:** Collected from Review Agent across all stories (Critical/Important/Minor, with file references)
5. **Anti-patterns discovered:** Contents of `.claude/hooks/anti-patterns-pending.txt`
6. **Guideline references:** Paths to architecture.md, relevant shards, testing-standards.md
7. **Sprint-status snapshot:** Current state of `sprint-status.yaml`

If any data is unavailable, include a section stating **"Not captured"** — the retro will propose instrumentation improvements.

### Phase 2: Parallel Lens Evaluation

Team Lead spawns lens subagents in parallel. Each lens reads ONLY the evidence bundle and produces a structured JSON report.

**Core lenses (always run):**

| Lens | Prompt File | Focus |
|------|------------|-------|
| Delivery Engineer | `.claude/retro/prompts/delivery-lens.md` | Code quality, complexity hotspots, maintainability, build friction |
| QA | `.claude/retro/prompts/qa-lens.md` | Test gaps, flaky tests, missing edge cases, quality gates |
| Architecture | `.claude/retro/prompts/arch-lens.md` | Boundary violations, coupling, layering drift, DDD compliance |
| Docs/Enablement | `.claude/retro/prompts/docs-lens.md` | Doc drift, guideline clarity, missing runbooks |
| Security | `.claude/retro/prompts/security-lens.md` | ITenantContext usage, data isolation, auth/authz, secrets hygiene |

**Situational lenses (add when relevant):**
- Product Lens — for feature stories with UX/product implications
- SRE/DevOps Lens — when CI/deployment is in scope

Each lens produces:
- wins (max 5)
- risks/problems (max 7)
- missing evidence (instrumentation gaps)
- candidate action items (tagged with type, evidence refs, acceptance criteria)

Lens outputs are saved to `.retro/<run_id>/lens/<lens-name>.json`.

### Phase 3: Synthesis + Output

Team Lead acts as Retro Lead and synthesizes lens outputs:

1. **Merge** lens outputs, resolve duplicates, prioritize by risk reduction
2. **Produce `retro.json`** at `.retro/<run_id>/retro.json` conforming to `.claude/retro/schemas/retro.schema.json`
3. **Render `retro.md`** at `.retro/<run_id>/retro.md` — concise human-readable summary
4. **Process action items** by type:

| Action Type | Feeds Into |
|------------|-----------|
| `hook_update` | Modify `.claude/hooks/` scripts or add to anti-patterns.txt |
| `docs_update` | Update architecture shards in `_bmad-output/planning-artifacts/architecture/` |
| `guideline_gap` | Add to `.claude/hooks/anti-patterns.txt` (permanent) |
| `test_gap` | Create story/task for next sprint |
| `quality_gate_gap` | CI pipeline updates (`.github/workflows/`) |
| `refactor` | Create story/task for next sprint |
| `architecture_alignment` | Update architecture shards or create ADR |
| `security_hardening` | Immediate fix or next-sprint story depending on severity |
| `observability` | Logging/monitoring improvements |
| `process_change` | Update this workflow doc or hook configuration |

5. **Process anti-patterns-pending.txt:**
   - Recurring (2+ stories) -> promote to `.claude/hooks/anti-patterns.txt`
   - One-off -> remove from pending
   - Architecture-level -> action item with type `docs_update`
   - After processing, leave ONLY the header comments — no log entries about what was promoted. Use `retro.md` for history, not working files.

6. **Save** `retro.json` and `retro.md` to `.retro/<run_id>/`

### Phase 4: Apply (Self-Healing)

This is where the retro becomes self-healing. Team Lead works through the action items and applies changes directly — not as future work, but NOW, before the retro is considered done.

**Immediate apply (Team Lead does these directly):**

| Action Type | What to Do |
|------------|-----------|
| `hook_update` | Edit `.claude/hooks/` scripts or add entries to `anti-patterns.txt`. Verify the hook works by testing against an example. |
| `guideline_gap` | Add regex + glob + message to `.claude/hooks/anti-patterns.txt`. Test that the anti-pattern is caught. |
| `docs_update` | Edit the architecture shard identified in `files_to_touch`. Keep changes minimal and factual. |
| `architecture_alignment` | Update the relevant architecture shard or create an ADR in `architecture/adrs/`. |
| `process_change` | Edit `.claude/process/team-workflow.md` or hook configuration in `settings.local.json`. |
| `observability` | Add logging/monitoring if it's a config-level change. |

**Delegate to Dev Agent (if the fix requires implementation):**

| Action Type | What to Do |
|------------|-----------|
| `security_hardening` (P0) | Assign to Dev Agent immediately. Must be fixed before retro completes. |
| `quality_gate_gap` | Assign to Dev Agent to update CI pipeline (`.github/workflows/`). |
| `test_gap` (P0/P1) | Assign to Dev Agent to write the missing tests now. |

**Fix immediately unless too large (P2 included):**

| Action Type | What to Do |
|------------|-----------|
| `refactor` (P2) | Fix now if ≤1 story point of effort. Otherwise add as a tracked task in sprint-status.yaml under the current epic. |
| `test_gap` (P2) | Assign to Dev Agent now — test gaps compound if left open. |
| `docs_update` (ANY priority) | Apply immediately — ALWAYS. Docs updates are zero-risk. A docs_update must never be deferred. |
| `security_hardening` (ANY priority) | Fix now. Security items must never be deferred. |
| `observability` (P2) | Fix now if config-level. Defer only if it requires new infrastructure. |

**Rules for Phase 4:**
- P0 actions are ALWAYS applied immediately — never deferred
- P1 actions are ALWAYS applied immediately — either by Team Lead (docs, hooks, process) or by Dev Agent (code changes)
- P2 `docs_update` and `security_hardening` are NEVER deferred — these are non-negotiable
- P2 `test_gap` is assigned to Dev Agent now — test gaps compound if left open
- P2 `refactor` may be deferred ONLY if effort exceeds 1 story point AND the refactor would destabilize code not touched this sprint
- If a P2 action IS deferred, it MUST be added as a task in `sprint-status.yaml` under the current epic with a clear key (e.g., `epic-1-deferred-description`) — NOT "deferred to next sprint"
- **An item carried from a previous retro cannot be deferred again.** Fix it or explicitly close it with justification.
- The retro is NOT done until all P0, P1, and applicable P2 actions are applied or assigned
- **Expected outcome: 0-2 deferred items per retro.** If more than 2 items are deferred, the retro is being too permissive.

### Phase 5: Finalize

After all actions are applied:

1. **Commit self-healing changes** (anti-pattern updates, doc edits, hook changes) as a separate commit with message: `retro: apply self-healing actions from <run_id>`
2. **Copy retro summary** to `_bmad-output/implementation-artifacts/epic-N-retro-YYYY-MM-DD.md`
3. **Update `sprint-status.yaml`:**
   - Mark retro as `done`
   - If any P2 items were deferred (only for substantial refactors), add them as tracked tasks under the current epic
4. **Commit** retro document + sprint-status update
5. **Deferred items tracking** — if anything was deferred, list it in retro.md under "Deferred (Tracked)" with the sprint-status.yaml task ID. The next retro will check if these were resolved.

### Mini-Retro Self-Healing (Per Story)

The mini-retro in Story Cycle Step 4 is not just evidence capture — it also applies immediate fixes:

1. **Anti-pattern capture:** Add new entries to `.claude/hooks/anti-patterns-pending.txt`
2. **Immediate doc fixes:** If a review finding revealed a doc that is *wrong* (not just incomplete), fix the doc NOW — don't wait for the full retro. Wrong docs cause repeated errors.
3. **Hook quick-fixes:** If a review finding revealed an anti-pattern that a hook SHOULD have caught but didn't, add it to `anti-patterns-pending.txt` immediately so it catches it for the next story in this sprint.

The full retro later decides whether pending items become permanent.

### Action Item Quality Rules

An action item is valid ONLY if it is:
- **Specific:** Not "improve testing" but "add contract test for endpoint X with cases A/B/C"
- **Evidence-linked:** Must reference a file, commit, or snippet from the evidence bundle
- **Verifiable:** Acceptance criteria are machine-checkable where possible
- **Automatable:** Include hints and likely files when feasible

### Retro Rules

- **No questions.** If missing info prevents certainty, mark as `Unknown` and propose an instrumentation action item.
- **Evidence-first.** Every claim must cite `evidence_refs` from the evidence bundle.
- **Blameless phrasing.** Focus on system/process improvements, not agent behavior.
- **Keep actions small.** 5-10 actions typical; fewer for small scopes. High-impact over comprehensive.

## Rules

1. **Story is NOT done until Review Agent sends explicit "APPROVED".** Delivering findings is NOT approval. Team Lead cannot override or shortcut this.
2. **Team Lead MUST NOT mark a review task as completed until explicit approval.** Findings with Critical or Important items begin the fix cycle — they do not complete the review.
3. **Never disband the team or work solo.** The three-agent structure exists for a reason.
4. **Never proceed with unresolved Critical/Important findings.** Fix first, then re-review.
5. **sprint-status.yaml MUST be updated per story.** The remind-sprint-status hook will remind you if you forget.
6. **Read architecture docs before writing code.** The gate-shard-reads hook will block you if you haven't.
7. **One story at a time.** Complete the full cycle (including approval) before starting the next story.
8. **Dev Agent MUST NOT self-assign story tasks.** Only start a new story when Team Lead explicitly assigns it with a message. Task list availability alone is not authorization.
9. **Next story tasks are blocked by the Approve task, not the Review task.** See Task Structure section.

## Communication Guidelines

Teammates go idle after every message -- this is completely normal. It does NOT mean they are done or stuck.

- **Send ONE clear message** with: task description + relevant context + expected deliverable
- **Do NOT send duplicate "please continue" messages** -- the teammate received your first message
- **Do NOT ping idle teammates** unless you have new work or information for them
- **Use TaskUpdate to track progress** -- mark tasks in_progress when starting, completed when done
- **Dev and Review agents communicate through Team Lead** -- no direct messages between them

## Architecture Doc Requirements

Before writing to any source directory, agents must have read the required architecture shards. The `gate-shard-reads` hook enforces this:

| Writing to | Must have read |
|-----------|---------------|
| `api/src/` or `api/tests/` | `team-workflow.md`, `architecture.md`, `patterns-backend.md`, `api-patterns.md` |
| `api/tests/` specifically | Also `testing-standards.md` |
| `web/src/` | `team-workflow.md`, `architecture.md`, `patterns-frontend.md`, `frontend-architecture.md` |
| `web/src/**/*test*` or `*spec*` | Also `testing-standards.md` |
| Non-source files | Not gated |

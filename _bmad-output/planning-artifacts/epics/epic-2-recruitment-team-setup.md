# Epic 2: Recruitment & Team Setup

Erik can create recruitments with configurable workflow steps, invite team members, and manage the full recruitment lifecycle including closing.

## Story 2.1: Create Recruitment with Workflow Steps

As a **recruiting leader (Erik)**,
I want to create a new recruitment with a title, description, and configurable workflow steps,
So that I can set up a structured hiring process for my team to follow.

**Acceptance Criteria:**

**Given** an authenticated user is on the home screen
**When** they click "Create Recruitment"
**Then** a dialog is displayed with fields for title (required), description (optional), and job requisition reference (optional)
**And** a default workflow template is shown with 7 steps: Screening, Technical Test, Technical Interview, Leader Interview, Personality Test, Offer/Contract, Negotiation

**Given** the recruitment creation dialog is open
**When** the user views the default workflow steps
**Then** they can rename any step, add new steps, remove steps, and reorder steps freely before saving

**Given** the user has entered a title and optionally customized the workflow
**When** they submit the creation form
**Then** a new recruitment is created with status "Active"
**And** the API returns 201 Created with a Location header
**And** the user who created it is automatically added as a permanent member (cannot be removed)
**And** a `RecruitmentCreatedEvent` is raised and recorded in the audit trail

**Given** a recruitment is created with workflow steps
**When** candidates are later added (imported or manually created)
**Then** new candidates are placed at the first workflow step with outcome status "Not Started"

**Given** the user submits the form with a missing title
**When** validation runs
**Then** a field-level validation error is shown ("Title is required")
**And** the form is not submitted

**Given** the user submits the form with valid data
**When** the API processes the request
**Then** workflow step names must be unique within the recruitment
**And** step order is contiguous (no gaps in sequence)

**Technical notes:**
- Backend: `CreateRecruitmentCommand` + handler + FluentValidation
- Frontend: `CreateRecruitmentForm.tsx` with react-hook-form + zod
- Domain: `Recruitment` aggregate root creates `WorkflowStep` children and `RecruitmentMember` for the creator
- FR4, FR5, FR59, FR62 fulfilled

## Story 2.2: Recruitment List & Navigation

As a **user**,
I want to see a list of all recruitments I have access to and navigate between them,
So that I can quickly find and switch to the recruitment I need to work on.

**Acceptance Criteria:**

**Given** an authenticated user has access to one or more recruitments
**When** the home screen loads
**Then** a list of recruitments is displayed showing each recruitment's title and current status (Active/Closed)
**And** only recruitments where the user is a member are shown

**Given** the user has no recruitments (not a member of any)
**When** the home screen loads
**Then** the empty state from Story 1.5 is displayed with the "Create Recruitment" CTA

**Given** the recruitment list is displayed
**When** the user clicks a recruitment
**Then** the user navigates to that recruitment's main view
**And** the URL updates to reflect the selected recruitment (e.g., `/recruitments/{id}`)
**And** navigation completes in under 300ms (client-side routing)

**Given** the user is viewing a recruitment
**When** they look at the app header
**Then** the recruitment name appears as a breadcrumb
**And** if the user has access to multiple recruitments, the breadcrumb includes a dropdown to switch between recruitments

**Given** the user has access to multiple recruitments
**When** they click the recruitment selector dropdown in the header
**Then** all accessible recruitments are listed with their status
**And** selecting one navigates to that recruitment without a full page reload

**Given** the user is a member of Recruitment A but not Recruitment B
**When** they attempt to access Recruitment B's URL directly
**Then** a 403 Forbidden response is returned
**And** the user sees an appropriate error message

**Technical notes:**
- Backend: `GetRecruitments` query (filtered by ITenantContext), `GetRecruitmentById` query
- Frontend: `RecruitmentList.tsx`, `RecruitmentSelector` breadcrumb component
- React Router params: `recruitmentId` in URL
- FR6, FR13, FR60 fulfilled

## Story 2.3: Edit Recruitment & Manage Workflow Steps

As a **recruiting leader (Erik)**,
I want to edit a recruitment's details and modify workflow steps on an active recruitment,
So that I can adapt the hiring process as requirements evolve mid-recruitment.

**Acceptance Criteria:**

**Given** an active recruitment exists
**When** the user edits the recruitment title or description
**Then** the changes are saved and a success toast is shown
**And** the updated values are reflected immediately in the UI

**Given** an active recruitment exists
**When** the user adds a new workflow step
**Then** the step is added at the specified position in the sequence
**And** the step order is adjusted to remain contiguous
**And** existing candidates see the new step as "Not Started"

**Given** an active recruitment has a workflow step with no recorded outcomes
**When** the user removes that step
**Then** the step is removed from the workflow
**And** any candidates currently at that step are moved to the next step with status "Not Started"
**And** step order is recompacted

**Given** an active recruitment has a workflow step with recorded outcomes
**When** the user attempts to remove that step
**Then** the removal is blocked with a clear message: "Cannot remove — outcomes recorded at this step"
**And** the step remains unchanged

**Given** an active recruitment has workflow steps
**When** the user reorders steps
**Then** the sequence is updated
**And** no candidate data is lost or corrupted

**Given** a closed recruitment exists
**When** the user attempts to edit the title, description, or workflow steps
**Then** all edit controls are disabled or hidden
**And** the recruitment is displayed in read-only mode

**Technical notes:**
- Backend: `UpdateRecruitmentCommand`, `AddWorkflowStepCommand`, `RemoveWorkflowStepCommand`
- Frontend: `EditRecruitmentForm.tsx`, `WorkflowStepEditor.tsx`
- Domain: `Recruitment.AddStep()`, `Recruitment.RemoveStep()` — child entities modified only through aggregate root
- FR11, FR12 fulfilled

## Story 2.4: Team Membership Management

As a **recruiting leader (Erik)**,
I want to invite team members to a recruitment and manage who has access,
So that the right people can collaborate on candidate screening and assessment.

**Acceptance Criteria:**

**Given** a user is viewing a recruitment they are a member of
**When** they click "Manage Team" or equivalent action
**Then** they see the current list of members with their names and roles

**Given** a user wants to invite a new member
**When** they open the invite dialog and start typing a name or email
**Then** the system searches the organizational directory (Microsoft Entra ID / Graph API)
**And** matching users are displayed as suggestions

**Given** the user selects a person from the directory search results
**When** they confirm the invitation
**Then** the selected person is added as a member of the recruitment
**And** a `MembershipChangedEvent` is raised and recorded in the audit trail
**And** a success toast confirms the addition

**Given** the user views the member list
**When** they look at the recruitment creator
**Then** the creator is visually marked as permanent (e.g., "Creator" badge)
**And** no remove action is available for the creator

**Given** the user wants to remove a non-creator member
**When** they click the remove action for that member
**Then** the member is removed from the recruitment
**And** that member can no longer see or access the recruitment
**And** a `MembershipChangedEvent` is raised and recorded in the audit trail

**Given** the user attempts to remove the recruitment creator
**When** the remove action is attempted
**Then** the action is blocked (button disabled or hidden)
**And** the creator remains a permanent member

**Given** a member is removed from a recruitment
**When** that removed user navigates to the recruitment list
**Then** the recruitment no longer appears in their list
**And** direct URL access returns 403 Forbidden

**Technical notes:**
- Backend: `AddMemberCommand`, `RemoveMemberCommand`, `GetMembersQuery`, `SearchDirectoryQuery`
- Frontend: `MemberList.tsx`, `InviteMemberDialog.tsx` with directory search
- Infrastructure: `EntraIdDirectoryService.cs` using Microsoft Graph API
- FR56, FR57, FR58, FR61 fulfilled

## Story 2.5: Close Recruitment & Read-Only View

As a **recruiting leader (Erik)**,
I want to close a completed recruitment so that it is locked from further changes and the GDPR retention timer begins,
So that the recruitment data is preserved for reference during the retention period and then properly cleaned up.

**Acceptance Criteria:**

**Given** an active recruitment exists
**When** the user clicks "Close Recruitment"
**Then** a confirmation dialog is shown explaining: the recruitment will be locked from edits, and data will be retained for the configured retention period before anonymization

**Given** the user confirms the close action
**When** the system processes the close
**Then** the recruitment status changes to "Closed"
**And** a `ClosedAt` timestamp is recorded (starts the GDPR retention timer)
**And** a `RecruitmentClosedEvent` is raised and recorded in the audit trail
**And** a success toast confirms the closure

**Given** a recruitment is closed
**When** any user views it
**Then** the recruitment is displayed in read-only mode
**And** all edit controls are disabled or hidden (no editing title, steps, outcomes, candidates)
**And** the recruitment is visually marked as "Closed" in the recruitment list

**Given** a recruitment is closed
**When** the user views the candidate list and details
**Then** all candidate data, documents, and outcome history remain visible
**And** no modifications can be made

**Given** a recruitment is closed
**When** the user attempts to import candidates or upload documents via API
**Then** the API returns 400 Bad Request with Problem Details: "Recruitment is closed"

**Given** a closed recruitment exists
**When** the recruitment list is displayed
**Then** the closed recruitment appears with a "Closed" status indicator
**And** closed recruitments are visually distinct from active ones

**Technical notes:**
- Backend: `CloseRecruitmentCommand` + handler, domain exception `RecruitmentClosedException` thrown on any mutation attempt after close
- Frontend: `CloseRecruitmentDialog.tsx`, read-only mode enforcement across all feature components
- Domain: `Recruitment.Close()` sets status and ClosedAt timestamp
- FR7, FR8 fulfilled

---

# Epic 6: Audit Trail & GDPR Compliance

Users can view the complete audit trail for a recruitment, and the system handles data retention and anonymization after recruitment close.

## Story 6.1: Audit Trail Viewing

As a **user (Erik)**,
I want to view the complete audit trail for a recruitment showing every state change with who performed it and when,
So that I have full accountability and traceability of all actions taken during the recruitment.

**Acceptance Criteria:**

**Given** an active or closed recruitment exists with recorded audit entries
**When** the user navigates to the audit trail view for that recruitment
**Then** a chronological list of audit entries is displayed, most recent first

**Given** audit entries are displayed
**When** the user views an entry
**Then** each entry shows: who performed the action (user name), when it occurred (timestamp), what action was taken (e.g., "Outcome recorded", "Candidate imported", "Document uploaded", "Recruitment created", "Recruitment closed", "Member added", "Member removed"), and the affected entity (e.g., candidate name, document name)

**Given** the audit trail has many entries
**When** the user scrolls or pages through the list
**Then** entries are paginated for performance
**And** the list loads efficiently without blocking the UI

**Given** the user views an audit entry
**When** they inspect the entry detail
**Then** no PII is exposed in the audit event context (IDs and metadata only — the display resolves entity names from current data, not from stored PII in the audit entry)

**Given** a recruitment has had no state changes beyond creation
**When** the user views the audit trail
**Then** the creation event is shown as the only entry

**Given** the audit trail is viewed for a closed recruitment
**When** the user inspects the entries
**Then** all historical entries remain visible in read-only mode
**And** the close event is included in the trail

**Technical notes:**
- Backend: `GetAuditTrailQuery` + handler (paginated, filtered by recruitment ID)
- Frontend: `AuditTrail.tsx`, `useAuditTrail.ts` hook
- AuditEntry entity + AuditBehaviour recording already in place from Epic 1 (Story 1.3)
- Display resolves entity IDs to names via current data — audit entries themselves contain no PII
- NFR19: immutable audit trail (who, what, when)
- FR52, FR53, FR54 fulfilled

## Story 6.2: GDPR Retention & Anonymization

As a **system administrator**,
I want the system to automatically anonymize recruitment data after the configured retention period expires following recruitment close,
So that candidate PII is properly cleaned up in compliance with GDPR while preserving aggregate metrics for historical analysis.

**Acceptance Criteria:**

**Given** the GDPR retention service is running
**When** the daily timer fires
**Then** the service queries all closed recruitments where `ClosedAt + retention period < now`
**And** the service uses `ITenantContext.IsServiceContext = true` to bypass global query filters

**Given** a closed recruitment has exceeded the retention period
**When** the anonymization job runs
**Then** the following PII is stripped: candidate names, emails, phones, locations, free-text outcome reasons, and all direct identifiers
**And** the following aggregate metrics are preserved: candidate counts per step, outcome counts (Pass/Fail/Hold per step), time-in-step durations, and recruitment duration

**Given** a closed recruitment has candidate documents in blob storage
**When** the anonymization job runs
**Then** all PDF documents are deleted from Azure Blob Storage
**And** the `CandidateDocument` records are removed or nullified

**Given** the anonymization job completes for a recruitment
**When** the recruitment is viewed
**Then** the recruitment shows anonymized data: candidate records display placeholder values (e.g., "Anonymized Candidate 1")
**And** aggregate metrics remain visible (counts per step, outcome distributions)
**And** the audit trail remains intact (audit entries reference IDs, not PII)

**Given** the anonymization job encounters an error on one recruitment
**When** the error occurs
**Then** the error is logged with the recruitment ID (no PII in logs)
**And** the job continues processing remaining expired recruitments
**And** the failed recruitment is retried on the next daily run

**Given** the retention period is configurable
**When** the deployment configuration is inspected
**Then** the retention period is read from `appsettings.json` (or environment variable)
**And** the default value is 12 months

**Given** a closed recruitment has NOT exceeded the retention period
**When** the daily timer fires
**Then** the recruitment is not modified
**And** all data remains fully accessible in read-only mode

**Given** the anonymization job runs
**When** it processes recruitments
**Then** each anonymization action is recorded in the audit trail (system-initiated, not user-initiated)

**Technical notes:**
- Infrastructure: `GdprRetentionService` (IHostedService with daily timer)
- Uses `ITenantContext.IsServiceContext = true` to query across all recruitments
- Anonymization logic in Application layer (testable without infrastructure)
- Domain value object: `AnonymizationResult` captures what was preserved and stripped
- Blob Storage: batch delete of all documents for the recruitment
- App Service "Always On" keeps the hosted service running
- FR9, FR63 fulfilled

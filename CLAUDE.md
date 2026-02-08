# recruitment-tracker

## Testing Policy: Pragmatic TDD

This project follows **Pragmatic TDD** as defined in `docs/testing-pragmatic-tdd.md`.

Before coding any task, declare which testing mode applies (test-first, spike, or characterization) and why. Before finishing, list tests added and what risk they cover.

Default to test-first for domain/business logic. Use spikes only when uncertainty is high — tests must be added before merge.

## Domain Modeling: Pragmatic DDD

This project uses pragmatic DDD with three aggregate roots: **Recruitment**, **Candidate**, and **ImportSession**. See `_bmad-output/planning-artifacts/architecture.md` — Aggregate Boundaries section.

Key rules:

- Modify child entities only through aggregate root methods (e.g., `recruitment.AddStep()`, not `dbContext.WorkflowSteps.Add()`)
- Cross-aggregate references use IDs only
- One aggregate per transaction; use domain events for cross-aggregate coordination
- Use ubiquitous language consistently (see architecture doc glossary)

## Tech Stack

Backend: ASP.NET Core (.NET 10), EF Core, MediatR, Azure SQL, Azure Blob Storage
Frontend: React 19, TypeScript, Vite 7, Tailwind CSS v4, TanStack Query

## Before Writing Code

Read `_bmad-output/planning-artifacts/architecture.md` for core rules (aggregates, decisions, security, enforcement). Then load relevant shards from `_bmad-output/planning-artifacts/architecture/` based on your task — see the routing table in `architecture.md` or `architecture/index.md` for which shards to load.

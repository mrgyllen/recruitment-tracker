# Story 1.1: Project Scaffolding & CI Pipeline

Status: ready-for-dev

## Story

As a **developer**,
I want a working monorepo with backend and frontend projects, test infrastructure, and CI pipeline,
so that the team has a verified foundation to build features on.

## Acceptance Criteria

1. **Backend scaffold:** `api/` directory contains a .NET 10 solution initialized from the Jason Taylor Clean Architecture template (`dotnet new ca-sln -o api --database SqlServer`), `dotnet build api/api.slnx` succeeds with zero errors, and `dotnet test` passes all template default tests.

2. **Frontend scaffold:** `web/` directory contains a Vite 7 + React 19 + TypeScript app (`npm create vite@latest web -- --template react-ts`), `npm run dev` starts the Vite dev server, and `npm run build` produces `dist/`.

3. **Tailwind CSS:** Tailwind CSS v4 is installed and configured via `@tailwindcss/vite` plugin in `web/`.

4. **Vite proxy:** `vite.config.ts` routes `/api/*` to the .NET backend (`localhost:5001`).

5. **Frontend test infrastructure:** Vitest + Testing Library + MSW are installed and configured. `web/src/test-utils.tsx` exists with a custom render function (providers added incrementally as dependencies are installed in later stories). `web/src/mocks/server.ts` and `web/src/mocks/handlers.ts` exist with MSW setup. A smoke test (`App.test.tsx`) passes.

6. **Frontend code quality:** ESLint + Prettier are configured with import ordering rules.

7. **Backend code quality:** `.editorconfig` is configured for C# code style enforcement in `api/`.

8. **CI pipeline:** `.github/workflows/ci.yml` builds and tests both `api/` and `web/` on every PR.

9. **Monorepo structure:** The folder structure matches the architecture document's project tree.

## Tasks / Subtasks

- [ ] **Task 1: Initialize .NET backend** (AC: 1, 7)
  - [ ] Install Jason Taylor Clean Architecture template v10.0.0: `dotnet new install Clean.Architecture.Solution.Template::10.0.0`
  - [ ] Generate solution: `dotnet new ca-sln -o api --database SqlServer`
  - [ ] Verify `dotnet build api/api.slnx` succeeds with zero errors
  - [ ] Verify `dotnet test` passes all template default tests
  - [ ] Confirm `.editorconfig` exists in `api/` (template provides it). Review its rules — template defaults may be too lenient for `dotnet format --verify-no-changes` to be meaningful. Tighten severity to `warning` or `error` for key rules (naming, braces, spacing) so CI enforcement catches real issues.
  - [ ] Update `.gitignore` if needed for .NET artifacts (bin/, obj/, etc.)
  - [ ] **Testing mode: Spike** — Template generates its own tests; verify they pass. No custom tests needed for scaffolding.

- [ ] **Task 2: Initialize React frontend** (AC: 2, 3)
  - [ ] Generate Vite app: `npm create vite@latest web -- --template react-ts`
  - [ ] Install Tailwind CSS v4: `cd web && npm install tailwindcss @tailwindcss/vite`
  - [ ] Configure `@tailwindcss/vite` plugin in `vite.config.ts`
  - [ ] Add Tailwind CSS import to `web/src/index.css`: `@import "tailwindcss";`
  - [ ] Verify `npm run dev` starts successfully
  - [ ] Verify `npm run build` produces `dist/`
  - [ ] **Testing mode: Spike** — Scaffolding only; verify builds succeed.

- [ ] **Task 3: Configure Vite proxy** (AC: 4)
  - [ ] Add proxy configuration in `vite.config.ts`: `/api` → `http://localhost:5001`
  - [ ] **Testing mode: Spike** — Manual verification during integration; proxy tested when first API endpoint exists.

- [ ] **Task 4: Set up frontend test infrastructure** (AC: 5) — *depends on Task 2*
  - [ ] Install Vitest + Testing Library: `npm install -D vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom`
  - [ ] Install MSW: `npm install -D msw`
  - [ ] Configure Vitest in `vite.config.ts` (test environment: jsdom). Add `/// <reference types="vitest" />` at top of `vite.config.ts` for TypeScript support.
  - [ ] Create `web/src/test-utils.tsx` with a custom render that re-exports Testing Library's render. No provider wrapping yet — providers are added incrementally as libraries are installed (story 1.4/1.5).
  - [ ] Create `web/src/mocks/server.ts` with MSW setupServer
  - [ ] Create `web/src/mocks/handlers.ts` with empty handlers array
  - [ ] Add Vitest setup file (`web/src/test-setup.ts`) that starts MSW server and imports @testing-library/jest-dom
  - [ ] Write a smoke test (`web/src/App.test.tsx`) to verify the test infrastructure works
  - [ ] Verify `npm run test` (or `npx vitest run`) passes
  - [ ] **Testing mode: Test-first** — Write the smoke test first to prove the test infrastructure works.

- [ ] **Task 5: Configure ESLint + Prettier** (AC: 6) — *depends on Task 2*
  - [ ] Install ESLint + Prettier + related plugins: `npm install -D eslint prettier eslint-config-prettier eslint-plugin-import-x typescript-eslint @eslint/js`
  - [ ] Create `eslint.config.js` (flat config — `.eslintrc.cjs` is legacy) with TypeScript + React + import ordering rules via `eslint-plugin-import-x` (flat-config-compatible fork of `eslint-plugin-import`)
  - [ ] Create `.prettierrc` with project formatting rules
  - [ ] Add `lint` and `format` scripts to `package.json`
  - [ ] Verify `npm run lint` passes on generated code (fix any template issues)
  - [ ] **Testing mode: Spike** — Linting configuration; verify it runs without errors.

- [ ] **Task 6: Create CI pipeline** (AC: 8) — *depends on Tasks 1, 2, 4, 5*
  - [ ] Create `.github/workflows/ci.yml`
  - [ ] Configure trigger: `on: [pull_request]` targeting main branch, plus push to main
  - [ ] Add .NET job: setup .NET 10, `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`
  - [ ] Add frontend job: setup Node.js LTS, `npm ci`, `npm run lint`, `npm run build`, `npm run test`
  - [ ] **Testing mode: Spike** — CI pipeline tested by pushing to a PR branch.

- [ ] **Task 7: Verify monorepo structure & end-to-end pipeline** (AC: 9) — *depends on Tasks 1–6*
  - [ ] Confirm folder structure aligns with architecture document's project tree
  - [ ] Verify root `.gitignore` covers both .NET and Node artifacts
  - [ ] Confirm no secrets or PII in committed files
  - [ ] Run full CI pipeline end-to-end (push a test branch or run all CI steps locally): `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `npm ci`, `npm run lint`, `npm run build`, `npm run test`. All must pass green.
  - [ ] **Testing mode: N/A** — Structural + pipeline verification.

## Dev Notes

- **Affected aggregate(s):** None — this is infrastructure scaffolding only. No domain entities created in this story.
- **Source tree:** Creates the entire `api/` and `web/` directory trees plus `.github/workflows/ci.yml`.

### Critical Architecture Constraints

**Backend (.NET):**
- **Template:** Jason Taylor Clean Architecture v10.0.0 — generates the entire `api/` structure including Domain, Application, Infrastructure, Web projects and test projects
- **Runtime:** .NET 10 (LTS, current stable v10.0.2)
- **API style:** Minimal API (built into ASP.NET Core — no additional dependency)
- **ORM:** Entity Framework Core with SQL Server (template configures this)
- **Test framework:** xUnit (template provides this)
- **Mocking:** NSubstitute is MANDATORY for all backend mocking — do NOT use Moq
- **Code style:** `dotnet format` enforced via `.editorconfig`; CI must run `dotnet format --verify-no-changes`. Review template `.editorconfig` — tighten severity on key rules (naming, braces, spacing) to `warning` or `error` so `dotnet format` actually catches violations (lenient defaults pass vacuously).

**Frontend (React):**
- **React 19** (latest stable: v19.2.4)
- **Vite 7** (latest stable: v7.3.1)
- **TypeScript:** Strict mode enabled
- **Tailwind CSS v4** via `@tailwindcss/vite` plugin — zero-config, automatic content detection. Just add `@import "tailwindcss";` to CSS entry.
- **Testing:** Vitest + @testing-library/react + MSW v2 (latest: v2.12.9)
- **Code quality:** ESLint flat config (`eslint.config.js`) + Prettier with import ordering via `eslint-plugin-import-x` (flat-config-compatible)
- **State management (later stories):** TanStack Query v5 for server state, React Router v7 for URL state, component-local useState — NO Redux/Zustand
- **Browser support:** Edge Chromium + Chrome only (no Firefox/Safari)
- **Minimum viewport:** 1280px (desktop-first)

**Monorepo Structure (root level):**
```
recruitment-tracker/
├── .github/
│   └── workflows/
│       └── ci.yml
├── api/                    # .NET Clean Architecture solution
│   ├── api.sln
│   ├── .editorconfig
│   ├── src/
│   │   ├── Domain/
│   │   ├── Application/
│   │   ├── Infrastructure/
│   │   └── Web/
│   └── tests/
│       ├── Domain.UnitTests/
│       ├── Application.UnitTests/
│       ├── Application.FunctionalTests/
│       └── Infrastructure.IntegrationTests/
├── web/                    # Vite React app
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   ├── eslint.config.js
│   ├── .prettierrc
│   └── src/
│       ├── main.tsx
│       ├── App.tsx
│       ├── index.css         # Tailwind entry
│       ├── test-utils.tsx    # Custom test render
│       └── mocks/
│           ├── server.ts
│           └── handlers.ts
├── docs/
├── CLAUDE.md
└── .gitignore
```

### Vite Configuration Reference

```typescript
/// <reference types="vitest" />
// web/vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5001',
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test-setup.ts'],
  },
})
```

### Import Ordering (ESLint Enforced)

```
1. React/framework imports
2. Third-party libraries
3. Absolute imports (@/ alias)
4. Relative imports
5. Type-only imports
```

### Test Utils Reference

```typescript
// web/src/test-utils.tsx
// Minimal for story 1.1 — no providers yet.
// Add QueryClientProvider (story 1.5), MemoryRouter (story 1.5), etc. as deps are installed.
import { render, RenderOptions } from '@testing-library/react'

const customRender = (ui: React.ReactElement, options?: RenderOptions) =>
  render(ui, { ...options })

export * from '@testing-library/react'
export { customRender as render }
```

### MSW Setup Reference

```typescript
// web/src/mocks/server.ts
import { setupServer } from 'msw/node'
import { handlers } from './handlers'

export const server = setupServer(...handlers)
```

```typescript
// web/src/mocks/handlers.ts
import { http, HttpResponse } from 'msw'

export const handlers = [
  // Add handlers as API endpoints are created
]
```

### CI Pipeline Reference

```yaml
# .github/workflows/ci.yml
name: CI
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]

jobs:
  api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore api/api.slnx
      - run: dotnet build api/api.slnx --no-restore
      - run: dotnet test api/api.slnx --no-build
      - run: dotnet format api/api.slnx --verify-no-changes

  web:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: web
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 'lts/*'
          cache: 'npm'
          cache-dependency-path: web/package-lock.json
      - run: npm ci
      - run: npm run lint
      - run: npm run build
      - run: npm run test -- --run
```

### Library Versions (Verified Feb 2026)

| Library | Version | Notes |
|---------|---------|-------|
| .NET | 10.0.2 | LTS, stable |
| Clean Architecture Template | 10.0.0 | Install via `dotnet new install Clean.Architecture.Solution.Template::10.0.0` |
| React | 19.2.4 | Stable |
| Vite | 7.3.1 | Stable |
| Tailwind CSS | 4.1.x | Via `@tailwindcss/vite`, zero-config |
| MSW | 2.12.x | v2 API (Fetch API primitives) |
| Vitest | Latest | Bundled test runner for Vite |
| @azure/msal-browser | 5.1.0 | NOT installed in this story (story 1.2) |
| @azure/msal-react | 5.0.3 | NOT installed in this story (story 1.2) |
| TanStack Query | 5.90.x | NOT installed in this story (story 1.5) |
| React Router | v7 | NOT installed in this story (story 1.5) |
| shadcn/ui | Latest | NOT installed in this story (story 1.4) |
| NSubstitute | Latest | Backend mocking - verify template includes it; add if not |

### What This Story Does NOT Include

- No domain entities (story 1.3)
- No authentication/SSO (story 1.2)
- No UI components or design tokens (story 1.4)
- No routing or app shell (story 1.5)
- No API endpoints
- No database schema or migrations
- No shadcn/ui installation
- No MSAL packages
- No TanStack Query or React Router

### Anti-Patterns to Avoid

- **Do NOT install libraries that belong to later stories** (MSAL, TanStack Query, React Router, shadcn/ui, react-pdf). Only install what's in the acceptance criteria.
- **Do NOT create domain entities or database migrations.** This is purely scaffolding.
- **Do NOT use Moq** for backend tests — NSubstitute is mandatory.
- **Do NOT use `tailwind.config.js`** — Tailwind CSS v4 uses zero-config with `@import "tailwindcss"` in CSS.
- **Do NOT create path-filtered CI pipelines.** Single unified pipeline for now.
- **Do NOT over-engineer the test-utils.tsx** with providers for libraries not yet installed.

### Testing: Pragmatic TDD

This is a scaffolding story. The overall testing mode is **Spike** — we're setting up infrastructure and verifying it works. Per-task modes are declared above in the task list.

**Tests added by this story:**
- Template default xUnit tests (backend — provided by Clean Architecture template)
- Smoke test `App.test.tsx` (frontend — proves Vitest + Testing Library + MSW pipeline works)

**Risk covered:** Build pipeline integrity. If CI passes, the foundation is verified and all subsequent stories can build on it.

### Project Structure Notes

- The monorepo uses two top-level folders (`api/`, `web/`) as specified in the architecture document
- CI pipeline lives at `.github/workflows/ci.yml` — a single pipeline for both stacks
- Existing `docs/`, `_bmad/`, `_bmad-output/`, `.vscode/`, `.claude/` directories are preserved
- `.gitignore` must be updated to cover both .NET artifacts (bin/, obj/, .vs/) and Node artifacts (node_modules/, dist/)

### References

- [Source: `_bmad-output/planning-artifacts/architecture.md` (core) — Tech Stack, Core Decisions]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` — Project Structure, Directory Tree]
- [Source: `_bmad-output/planning-artifacts/architecture/starter-templates.md` — Template Selection, CI/CD Strategy]
- [Source: `_bmad-output/planning-artifacts/epics/epic-1-project-foundation-user-access.md` — Story 1.1 definition]
- [Source: `_bmad-output/planning-artifacts/prd.md` — NFRs, tech constraints, browser support]
- [Source: `docs/testing-pragmatic-tdd.md` — Testing policy]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

# Starter Template Evaluation

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains project context, key decisions, and enforcement rules._

## Primary Technology Domain

Full-stack web application based on project requirements analysis. Backend API + SPA frontend, both deployed on Azure.

## Repository Structure

**Monorepo** — single repository with two top-level application folders:

```
recruitment-tracker/
  api/           # .NET Clean Architecture solution
  web/           # Vite React app
  docs/          # Existing project documentation
  .github/       # CI/CD workflows
```

Rationale: Solo developer project. One repo means one PR per feature (both API and UI changes together), one CI/CD pipeline configuration, zero context-switching between repos. A feature like "import candidates" touches both `api/` and `web/` — one PR, one review, one merge.

## Starter Options Considered

**Backend:**

| Template | Version | Framework | Key Feature | Why Not |
|----------|---------|-----------|-------------|---------|
| Jason Taylor CleanArchitecture | v10.0.0 | .NET 10 | Minimal API, azd, use case scaffolding | **Selected** |
| Ardalis CleanArchitecture | v11.0.0 | .NET 9 | FastEndpoints, DDD-focused | FastEndpoints adds dependency + learning curve |

**Frontend:**

| Approach | Key Feature | Why Not |
|----------|-------------|---------|
| Official Vite + react-ts template | Zero opinions, add what we need | **Selected** |
| Third-party feature-rich templates | Pre-bundled libraries | Opinionated choices to remove, not add |

## Selected Backend Starter: Jason Taylor Clean Architecture

**Rationale:**
- .NET 10 (current), most widely adopted Clean Architecture template (18.7K stars)
- Minimal API is built into ASP.NET Core — no additional library dependency
- Use case scaffolding (`dotnet new ca-usecase`) eliminates boilerplate for commands/queries (~40% of a typical story's backend code; domain entities and infrastructure services remain manual)
- Azure Developer CLI (azd) integration aligns with Azure deployment target
- Aspire integration provides Azure SQL + Blob Storage + Application Insights wiring

**Aspire trade-off:** Aspire adds cognitive overhead (another abstraction to debug) but provides genuine value for Azure service orchestration and telemetry. For this project's scale, the integration value outweighs the debugging complexity. Application code does not couple to Aspire — it can be removed later if painful.

**Initialization Command:**

```bash
dotnet new install Clean.Architecture.Solution.Template::10.0.0
dotnet new ca-sln -o api --database SqlServer
```

**Architectural Decisions Provided by Starter:**

- **Language & Runtime:** C# / .NET 10
- **API Style:** Minimal API endpoints
- **Project Structure:** Domain / Application / Infrastructure / Web (Clean Architecture)
- **ORM:** Entity Framework Core with SQL Server
- **Patterns:** CQRS via MediatR, FluentValidation for input validation
- **Testing:** xUnit with test project structure (Domain.UnitTests, Application.UnitTests, Application.FunctionalTests, Infrastructure.IntegrationTests)
- **Azure:** azd integration, Aspire orchestration

**Database Strategy:**
- **Development:** Template defaults to delete-and-recreate with seed data on startup. Acceptable for initial development only.
- **Production:** EF Core migrations from the first deployment onward. Switch to migrations in development once schema stabilizes. The anonymization schema (GDPR) requires careful migration planning — cannot casually recreate a database with retention timers and audit trails.

## Selected Frontend Starter: Official Vite + React + TypeScript

**Rationale:**
- Clean starting point — add exactly what the project needs, remove nothing
- Tailwind CSS v4 with first-party Vite plugin requires zero configuration
- No opinionated library choices to fight or remove

**Initialization Commands:**

```bash
npm create vite@latest web -- --template react-ts
cd web && npm install tailwindcss @tailwindcss/vite
```

**Current Versions:** Vite 7.x (stable), React 19, Tailwind CSS 4.1.x

**Architectural Decisions Provided by Starter:**

- **Language:** TypeScript (strict)
- **Build Tool:** Vite 7 (dev server + production build)
- **Styling:** Tailwind CSS v4 via `@tailwindcss/vite` plugin (zero-config, automatic content detection)
- **Development:** Hot module replacement, fast refresh

**Tailwind v4 Browser Support:** Requires Chrome 111+, Firefox 128+, Safari 16.4+. Non-issue — PRD constrains to Edge (Chromium) and Chrome only.

**Libraries to add incrementally (not upfront):**

| Library | When to Add | Purpose |
|---------|------------|---------|
| @azure/msal-browser + @azure/msal-react | Day one | Entra ID OIDC auth (Authorization Code + PKCE) |
| Vitest + Testing Library | Day one | Unit/component testing |
| MSW (Mock Service Worker) | Day one | API mocking for component tests (consistent with `mocks/` directory) |
| ESLint + Prettier (or Biome) | Day one | Code quality |
| React Router v7 | First route setup | Client-side routing |
| TanStack Query | First API call | Server state management |
| Playwright | First E2E test | End-to-end smoke tests |
| react-pdf | Screening feature (Epic 4) | PDF rendering with text layer accessibility and per-page lazy loading |
| Form library (TBD) | Outcome recording feature | Form state management |
| Typed API client (TBD) | First API integration | Typed HTTP calls matching backend DTOs |

### Architectural Decision: PDF Viewing

**MVP uses react-pdf (PDF.js wrapper) for inline PDF rendering with short-lived SAS URLs.**

SAS tokens (15-minute validity) make the URL self-authenticating. react-pdf provides a text layer for screen reader accessibility (WCAG 2.1 AA compliance) and per-page lazy loading for efficient screening of 130+ candidate sessions. Page 1 renders immediately; subsequent pages lazy-load on scroll intersection. SAS URL pre-fetching for the next 2-3 candidates ensures seamless screening flow.

## Development Workflow

- .NET API runs on `localhost:5001` (or similar)
- Vite dev server runs on `localhost:5173` with API proxy configuration
- Aspire can orchestrate the API + Azure SQL in development (does not manage the Vite dev server)
- Both apps start independently; proxy configuration in `vite.config.ts` routes `/api/*` to the .NET backend

## CI/CD Pipeline Strategy

**Start with a single pipeline** that builds and tests both `api/` and `web/` on every PR. Simple and correct.

Optimize to **path-filtered pipelines** (`api/**` triggers .NET, `web/**` triggers frontend, both trigger both) only if CI becomes a bottleneck. Premature pipeline optimization is real.

**Note:** Project initialization using these commands should be the first implementation story.

## Template Cleanup Checklist

After scaffolding from the Jason Taylor Clean Architecture template, the following cleanup is **mandatory** before any feature work begins. These items were the #1 source of review findings during Epic 1.

### Backend (api/)

- [ ] **Remove AutoMapper** — Delete package from `Application.csproj` and `Directory.Packages.props`, remove DI registration from `DependencyInjection.cs`, delete `global using AutoMapper;` from `GlobalUsings.cs`, delete `MappingTests.cs`. Architecture mandates manual DTO mapping.
- [ ] **Remove Shouldly** — Replace with FluentAssertions in all test `.csproj` files. Update assertion syntax (`ShouldBe` -> `Should().Be()`). FluentAssertions should already be in `Directory.Packages.props`.
- [ ] **Remove AddDefaultIdentity** — Delete `AddDefaultIdentity<ApplicationUser>()` from `Infrastructure/DependencyInjection.cs`. Architecture uses Entra ID exclusively, not ASP.NET Identity.
- [ ] **Remove or exclude Web.AcceptanceTests** — Template includes SpecFlow/Playwright tests that require browser install. Either remove from solution or add Playwright install to CI.
- [ ] **Remove template entities** — Delete `TodoItem`, `TodoList`, `WeatherForecast`, `Colour` and all related commands/queries/DTOs/tests.
- [ ] **Remove Angular ClientApp** — Template may include Angular SPA proxy and `ClientApp/` directory. Not needed with separate Vite frontend.
- [ ] **Remove RazorPages** — Delete `Pages/` directory from Web project (Error.cshtml, _LoginPartial.cshtml, _ViewImports.cshtml) unless needed.
- [ ] **Tighten .editorconfig** — Template defaults are too lenient (`:silent`/`:suggestion`). Raise key rules (braces, namespaces, naming) to `:warning` for CI enforcement via `dotnet format --verify-no-changes`.
- [ ] **Verify test framework** — Ensure NUnit, not xUnit. Check for `[Fact]`/`[Theory]` (should be `[Test]`/`[TestCase]`).
- [ ] **Verify NSubstitute** — Ensure NSubstitute, not Moq. Check for `using Moq;`.

### Frontend (web/)

- [ ] **Remove template demo files** — Delete `App.css` (Vite template styles), clean up default `App.tsx` content.
- [ ] **Type MSW handlers** — Ensure `handlers.ts` exports typed array: `export const handlers: HttpHandler[] = []`.

### Infrastructure

- [ ] **Review api/infra/ Bicep files** — Template includes 40+ Bicep files for services not used by this project (Cosmos DB, AKS, CDN, etc.). Note for future cleanup but don't block on this.
- [ ] **Review .gitignore** — Template may include patterns for irrelevant ecosystems (Python, Java, Go). Prune if desired but not blocking.

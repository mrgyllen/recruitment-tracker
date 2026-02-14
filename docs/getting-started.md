# Getting Started

## Prerequisites

| Component | Native Mode | Docker Mode |
|-----------|-------------|-------------|
| Node.js 22+ | Required | Required (frontend only) |
| .NET 10 SDK | Required | Not needed |
| Docker + Docker Compose | Not needed | Required |
| SQL Server | Required (local or remote) | Provided by compose |

## Option 1: Native Mode

Best for development with full IDE support.

### Backend (API)

```bash
# From repo root
dotnet build api/api.slnx
dotnet run --project api/src/Web
```

The API starts on `https://localhost:5001` (or `http://localhost:5000`).

**Database:** Requires SQL Server. Set the connection string in `api/src/Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "apiDb": "Server=localhost;Database=apiDb;User Id=sa;Password=Dev@Pass123!;TrustServerCertificate=True"
  }
}
```

EF Core migrations run automatically on startup in Development mode.

### Frontend

```bash
cd web
npm install
npm run dev
```

The frontend starts on `http://localhost:5173`.

### Running Tests

```bash
# Frontend tests
cd web && npx vitest run

# Backend tests
dotnet test api/api.slnx
```

## Option 2: Docker Compose Mode

Best when .NET SDK is not installed locally. Starts the API and SQL Server in containers.

```bash
# From repo root — starts API + SQL Server
docker compose up --build

# In a separate terminal — start frontend
cd web
npm install
npm run dev
```

| Service | URL |
|---------|-----|
| API | `http://localhost:5000` |
| SQL Server | `localhost:1433` (sa / Dev@Pass123!) |
| Frontend | `http://localhost:5173` |

### Running Backend Tests via Docker

```bash
# Build and run tests inside a container
docker compose run --rm api dotnet test
```

## Quality Checks

Run before starting work on a new epic (see team-workflow.md Getting Started):

```bash
# TypeScript — must return zero errors
cd web && npx tsc --noEmit

# ESLint — must return zero errors/warnings
cd web && npx eslint src/ --max-warnings 0

# Frontend tests
cd web && npx vitest run

# Backend build
dotnet build api/api.slnx
```

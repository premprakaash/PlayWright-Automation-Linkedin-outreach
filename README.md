# CodeInspector Leads

Production-ready lead generation and prospect management module for **CodeInspector**.

This repository was bootstrapped as a greenfield implementation (no pre-existing CodeInspector monorepo was present). The solution follows the requested architecture and can be merged into a larger CodeInspector codebase later.

## Architecture (Phase 1 findings)

| Layer | Project | Responsibility |
| --- | --- | --- |
| Domain | `CodeInspector.Leads.Core` | Entities, enums, scoring, URL builders, connector contracts |
| Infrastructure | `CodeInspector.Leads.Infrastructure` | EF Core + PostgreSQL (`leads` schema), CSV/manual connectors, deduplication, Playwright company research |
| API | `CodeInspector.Leads.Api` | REST endpoints, JWT auth, tenant isolation, validation, OpenAPI |
| Worker | `CodeInspector.Leads.Worker` | Quartz.NET daily processing (09:00 Asia/Kolkata), durable jobs |
| Browser launcher | `CodeInspector.Leads.BrowserLauncher` | Local visible Chromium opens LinkedIn **people search** URLs only |
| Frontend | `frontend/` | React + Vite dashboard |
| Tests | `CodeInspector.Leads.Tests` | Scoring, URL normalization, deduplication, tenant isolation |

### Compliance boundaries

- No LinkedIn scraping, profile extraction, CAPTCHA bypass, or automated messaging.
- LinkedIn usage is limited to generating trusted people-search URLs and opening them for **manual** review.
- Playwright automation is restricted to permitted public company websites in the worker.

## Quick start (local)

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- PostgreSQL 16+
- Playwright browsers (for worker/browser launcher): `pwsh bin/Debug/net10.0/playwright.ps1 install chromium`

### Database

```bash
createdb codeinspector
# connection string in src/CodeInspector.Leads.Api/appsettings.json
```

### API

```bash
dotnet run --project src/CodeInspector.Leads.Api
```

Migrations run automatically on startup.

### Worker

```bash
dotnet run --project src/CodeInspector.Leads.Worker
```

### Browser launcher (workstation)

```bash
dotnet run --project src/CodeInspector.Leads.BrowserLauncher -- "CTO .NET SaaS"
```

### Frontend

```bash
cd frontend
cp .env.example .env
npm run dev
```

Sign in with a tenant UUID (dev JWT via `POST /api/v1/auth/token`).

## Docker Compose

```bash
export JWT_SIGNING_KEY='replace-with-32+ char secret'
docker compose up --build
```

Services:

- `postgres` (internal)
- `leads-api` → http://localhost:8080 (`/health`, Swagger in Development)
- `leads-worker` (no public ports)
- `leads-web` → http://localhost:3000

## Environment variables

| Variable | Description |
| --- | --- |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `JWT_SIGNING_KEY` | JWT HMAC signing key (required in production) |
| `DAILY_PROCESSING_CRON` | Quartz cron (default `0 0 9 * * ?`) |
| `DAILY_PROCESSING_TZ` | Time zone id (default `Asia/Kolkata`) |
| `DAILY_PROCESSING_BATCH_SIZE` | Max companies claimed per run (default `100`) |
| `VITE_API_BASE_URL` | Frontend API base URL for builds |

## CSV import

Use `samples/leads-import-template.csv`.

## Tests

```bash
dotnet test
```

## API highlights

- `POST /api/v1/research-sessions` — generate LinkedIn people-search URL
- `POST /api/v1/leads`, `POST /api/v1/imports/csv`
- `POST /api/v1/leads/{id}/drafts`, approve/archive/copy workflow via UI
- `POST /api/v1/jobs/run-daily` — admin-only manual trigger (rate limited)

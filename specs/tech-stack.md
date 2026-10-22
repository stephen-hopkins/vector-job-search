# Tech Stack

Vector Job Search is a C# / .NET application with a React single-page frontend. The backend collects job listings, embeds them via a hosted API, and ranks them against a user-defined ideal job description; the browser presents a ranked list with similarity scores.

## Core

| Layer | Choice | Rationale |
|---|---|---|
| Language | **C# 14** | Type safety end-to-end; first-class Azure integration; contributor familiarity |
| Runtime | **.NET 10 (ASP.NET Core, LTS)** | Strong async I/O for fetching listings; solid HTTP/JSON tooling |
| Embeddings | OpenAI-compatible hosted API (nano-gpt) | Base URL + API key in configuration; any OpenAI-compatible provider (OpenAI, Azure OpenAI, nano-gpt) can be swapped via config alone. Target model class: `text-embedding-3-small` (~$0.02/M tokens — pennies at personal scale) |
| Vector store | Azure SQL Database (free offer), native `VECTOR` columns | Free indefinitely: 100,000 vCore-seconds/month, 32 GB storage + backup. Cosine similarity via `VECTOR_DISTANCE()`; works with EF Core |
| Frontend | **React 19 + TypeScript 5.9 (SPA)** | Rich, ranked-list presentation; calls the ASP.NET Core backend API |
| Backend hosting | Azure App Service F1 (free tier) | 60 CPU-minutes/day, sufficient for personal use a few times a day |
| Frontend hosting | Azure Static Web Apps (free plan) | Free tier: 100 GB bandwidth; suits a single-user app |
| Infrastructure | **Terraform** | All Azure resources defined as code; remote state in an Azure Storage account (negligible cost) |
| Deployment | GitHub Actions | CI/CD on push; OIDC (federated credentials) via `azure/login` — no stored client secrets |

## Frameworks & Core Libraries

- **ASP.NET Core (Minimal APIs)** — backend API surface for collection, ranking, and configuration
- **Microsoft.Extensions.AI** — provider-agnostic embedding abstraction (`IEmbeddingGenerator`); the OpenAI-compatible client plugs in behind it
- **Entity Framework Core** — typed data access to Azure SQL, including `VECTOR` columns
- **Microsoft.Data.SqlClient** — ADO.NET driver underpinning EF Core for Azure SQL
- **System.Net.Http** (`HttpClientFactory`) — fetching listings from source APIs
- **React 19 + TypeScript 5.9** (Vite) — frontend SPA, calling the backend API over HTTP

## Data

- **Azure SQL Database free offer** (serverless General Purpose SKU, free-offer flag) with native `VECTOR` columns for production
- Serverless auto-pause introduces a brief cold start when idle — acceptable for a personal tool

## User Interface

- **React + TypeScript web app (SPA)** — the user-facing interface, hosted on Azure Static Web Apps
- Terraform links the Static Web App to the App Service backend (CORS / `backendUri` configuration)

## Listing Sources

- **Adzuna Jobs API** first — free tier: instant key, no payment details, ~2,500 calls/day
- **Reed Jobseeker API** second — to follow once Adzuna is working
- Both behind the pluggable source architecture (per the `sources` config setting)

## Tooling

- `dotnet` CLI for build, run, and test
- `npm` + `vite` for frontend development and production builds
- `typescript` compiler in strict mode for frontend type checking
- `terraform` for infrastructure provisioning (`infra/` directory)
- `GitHub Actions` for CI/CD (OIDC federated credentials; no stored secrets)
- `azure/setup-terraform` and `azure/login` actions in the pipeline
- `editorconfig` + formatter (`dotnet format`) for consistent C# style

## Testing

- **xUnit** — backend unit tests for similarity ranking, source parsers, and configuration loading
- **xUnit integration tests** against a local SQL instance for vector storage and queries
- **Vitest** — frontend unit tests, run alongside the TypeScript strict-mode compile in CI
- **React Testing Library** — component tests for the ranked-results UI

## Quality Gates

- All PRs must pass build and tests before merge (GitHub Actions CI)
- Lint/format check (`dotnet format --verify-no-changes`) on every push
- TypeScript strict-mode compile and frontend type checks on every push
- Terraform `fmt` and `validate` on infrastructure changes
- No deployment without a green CI run

## Constraints & Anti-Patterns

- **Running costs must stay near zero** — free tiers only (App Service F1, Static Web Apps free plan, SQL free offer); any choice that adds cost needs explicit justification
- Keep the provider behind the abstraction — never call an embedding API directly from pipeline code; it must remain swappable via the `embedding_provider` config setting
- Keep sources pluggable — new sources are additional implementations behind the `sources` config setting, not code forks
- Avoid design dead ends — no in-memory-only storage, no single-user assumptions baked into the data model, no unbounded in-process work; growth headroom comes free where it costs nothing up front
# Vector Job Search

Vector Job Search is an application that automatically finds job listings for you and ranks them against your ideal job using vector similarity search.

Instead of sifting through hundreds of listings, you describe your ideal job once. The application embeds that description, embeds incoming job listings, and uses vector search to compare them — returning a ranked list of the best matches.

## Dual purpose

This project serves two goals at once:

1. **A hobby project** — a genuinely useful tool for finding and ranking job listings with vector similarity search.
2. **A learning vehicle** — a deliberate practice ground for human-on-the-loop (HOTL) agentic engineering: agents execute end-to-end in bounded domains while the human supervises via evidence, dashboards, and surgical intervention.

The learning side of the project is documented in two files in the [`learning/`](learning/) folder:

- [`contract.md`](learning/contract.md) — the learning contract: objective, success criteria, phase milestones, and rules
- [`plan.md`](learning/plan.md) — the execution plan: per-phase instructions, prompts, and resources

## Constitution

The project's constitution lives in the [`specs/`](specs/) folder:

- [`mission.md`](specs/mission.md) — Purpose, values, and non-goals
- [`tech-stack.md`](specs/tech-stack.md) — Stack decisions (made and open)
- [`roadmap.md`](specs/roadmap.md) — Phased development plan
- [`process.md`](specs/process.md) — Engineering-process invariants (merge gates, spec–test agreement)

## How It Works

1. **Define your ideal job** — Provide a description of your dream role (title, technologies, culture, salary, remote preferences, etc.).
2. **Embed it** — Your ideal job description is converted into a vector embedding.
3. **Collect listings** — The application gathers job listings from configured sources.
4. **Vector search** — Each listing is embedded and compared to your ideal job embedding using cosine similarity.
5. **Ranked results** — Browse a ranked list of job listings, best matches first.

## Features

- Embedding-based matching against an ideal job profile
- Aggregates job listings from multiple sources
- Ranked results with similarity scores
- Configurable ideal job description

## Getting Started

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) (the pinned version lives in [`global.json`](global.json))
- [Node.js](https://nodejs.org/) >= 24 (includes npm)

### Backend

<!-- gate: P0-BUILD-BACKEND -->
```bash
dotnet build
```

<!-- gate: P0-TEST-BACKEND -->
```bash
dotnet test
```

<!-- gate: P0-FORMAT-BACKEND -->
```bash
dotnet format --verify-no-changes
```

### Frontend

Install dependencies first — the frontend build does not work before this step:

<!-- gate: P0-FRONTEND-INSTALL-ORDER -->
```bash
npm install --prefix src/web
```

<!-- gate: P0-BUILD-FRONTEND -->
```bash
npm --prefix src/web run build
```

<!-- gate: P0-TYPECHECK-FRONTEND -->
```bash
npm --prefix src/web run typecheck
```

<!-- gate: P0-TEST-FRONTEND -->
```bash
npm --prefix src/web test
```

### Run the backend

```bash
dotnet run --project src/VectorJobSearch.Api
```

The API listens on the port configured in `src/VectorJobSearch.Api/Properties/launchSettings.json` (default `http://localhost:5046`). Once running, the health route is available at `http://localhost:5046/health`.

## License

This project is licensed under the [MIT License](LICENSE).

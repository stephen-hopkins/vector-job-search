# Tech Stack

> **Status: TBD.** The project is in early development. This document records the
> decisions that have been made and the open decisions that must be resolved,
> along with the candidates and criteria for each. Update this file as choices
> are made — it is the authoritative record of the stack.

## Guiding Constraints

The stack must support these core capabilities (see `mission.md`):

1. **Text embedding** — Convert the ideal job description and job listings into vectors.
2. **Vector similarity search** — Rank listings by cosine similarity to the ideal job.
3. **Source aggregation** — Collect listings from multiple configured sources (APIs, boards, scrapers).
4. **Presentation** — Present a ranked list with similarity scores.

## Decided

| Area       | Decision          | Notes                                    |
| ---------- | ----------------- | ---------------------------------------- |
| Repository | Vector Job Search | See `README.md`                          |
| License    | TBD               | To be chosen before first public release |

## Open Decisions

### 1. Primary Language / Runtime

Candidates:

- **C# / .NET** — Strong typing, good async I/O, first-class Azure integration, solid HTTP/JSON tooling for API-based work.
- **Python** — Rich ML and embedding ecosystem (e.g. sentence-transformers), strong scraping tooling.
- **Node.js / TypeScript** — Strong typing, good async I/O for fetching listings, easy embedding API integration.

Decision criteria: embedding library availability, source-scraping needs, contributor familiarity.

**Decision: TBD**

### 2. Embedding Provider

**Decision: Embeddings will be provided by a hosted API.**

- Options include OpenAI, Cohere, and Azure OpenAI, among others.
- No local model hosting; per-use API cost is accepted.
- The provider must remain behind an abstraction (per the `embedding_provider` config setting) so the specific API can be changed via configuration without code changes.

**Specific API: TBD**

### 3. Vector Store

**Decision: TBD — most likely an Azure-hosted option.**

The leading direction is Azure (which pairs naturally with a C#/.NET stack), with candidates including:

- **Azure AI Search** (vector search capability) — fully managed, built-in vector indexing.
- **Azure Database for PostgreSQL + pgvector** — durable, queryable, one system for metadata + vectors.
- **Azure Cosmos DB** (vector search) — if a NoSQL fit emerges.

Self-hosted or non-Azure options (Chroma, Qdrant, in-memory/file-based) remain fallbacks if Azure costs or complexity don't fit this personal-scale project.

Scale expectation is personal use (thousands of listings, not millions), which all candidates satisfy.

### 4. Listing Sources

**Decision: Start with the [Adzuna Jobs API](https://developer.adzuna.com/), followed by the [Reed Jobseeker API](https://www.reed.co.uk/developers/jobseeker).**

- **Adzuna Jobs API** — first source to integrate.
- **Reed Jobseeker API** — second source, to follow once Adzuna is working.

Both are behind the pluggable source architecture (per the `sources` config setting), so further sources can be added as additional implementations.

### 5. User Interface

Candidates: CLI, local web app, TUI.

**Decision: TBD**

## Decision Process

1. Raise a proposal (issue or PR editing this file).
2. Record the decision with rationale in **Decided** above.
3. Move the resolved item out of **Open Decisions**.

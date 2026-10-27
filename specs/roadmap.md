# Roadmap

> Phased plan for Vector Job Search. Each phase builds on the last and should
> end in a usable state. Dates are omitted intentionally — phases ship when done.
>
> Note: The HOTL/agentic learning goals for this project are tracked separately
> in [`learning/contract.md`](../learning/contract.md) and
> [`learning/plan.md`](../learning/plan.md); they don't map one-to-one onto
> these phases, but the phases double as practice ground for them.

## Phase 0 — Foundation

- [x] Finalize tech stack decisions (`tech-stack.md`)
- [ ] Scaffold repository (project structure, tooling, linting, tests) — [`001-project-scaffold/spec.md`](001-project-scaffold/spec.md)
- [x] Choose license

**Exit criteria:** A skeleton project builds and tests pass.

## Phase 1 — Core Matching Pipeline

The minimal end-to-end path: describe an ideal job → embed one hand-supplied listing → see a similarity score.

- [ ] Embedding provider abstraction + first provider implementation
- [ ] Embed the ideal job description into a reference vector
- [ ] Embed a job listing and compute cosine similarity
- [ ] Minimal CLI output of the similarity score

**Exit criteria:** Given two pieces of text, the app reports their similarity.

## Phase 2 — Listing Collection

- [ ] Pluggable source architecture (APIs, boards, scrapers)
- [ ] Implement first real job listing source
- [ ] Store collected listings (raw text + metadata + embeddings)

**Exit criteria:** Running the app collects real listings from at least one source.

## Phase 3 — Vector Search & Ranking

- [ ] Vector store integration (embeddings + listing metadata)
- [ ] Rank all collected listings against the ideal job embedding
- [ ] Return top-`k` results with similarity scores
- [ ] Present ranked results in a browser-based interface usable by a developer

**Exit criteria:** The app produces a ranked list of real job listings for a user-defined ideal job.

## Phase 4 — Infrastructure & Deployment

> Note: Some Future Considerations (e.g., scheduled automatic collection) imply
> long-running services — keep those in mind when choosing infrastructure here.

- [ ] Define target deployment environment (cloud, container, local service, etc.)
- [ ] Provision/automate infrastructure (infrastructure-as-code where applicable)
- [ ] Set up secrets management (API keys, credentials)
- [ ] Establish deployment pipeline (build, test, deploy automation)
- [ ] Monitoring/logging for deployed components

**Exit criteria:** The app is deployed to a target environment with an automated path from code to running deployment.

## Phase 5 — Hardening & Polish

- [ ] Additional sources
- [ ] Configurable embedding providers (swap without code changes)
- [ ] Incremental/deduplicated listing collection
- [ ] Documentation: setup, configuration, usage

**Exit criteria:** A user can configure and use the deployed web app without touching code.

## Future Considerations

- Scheduled automatic collection
- Multiple ideal job profiles
- Explainability: show which parts of a listing drove the match
- Feedback loop: liked/disliked results refining the ideal job embedding

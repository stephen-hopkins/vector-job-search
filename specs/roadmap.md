# Roadmap

> Phased plan for Vector Job Search. Each phase builds on the last and should
> end in a usable state. Dates are omitted intentionally — phases ship when done.

## Phase 0 — Foundation

- [ ] Finalize tech stack decisions (`tech-stack.md`)
- [ ] Scaffold repository (project structure, tooling, linting, tests)
- [ ] Choose license
- [ ] Define configuration schema (`ideal_job`, `sources`, `embedding_provider`, `top_k`)

**Exit criteria:** A skeleton project builds, tests pass, and config loads.

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
- [ ] Store collected listings (raw text + metadata)

**Exit criteria:** Running the app collects real listings from at least one source.

## Phase 3 — Vector Search & Ranking

- [ ] Vector store integration (embeddings + listing metadata)
- [ ] Rank all collected listings against the ideal job embedding
- [ ] Return top-`k` results with similarity scores
- [ ] Present ranked results in a usable interface

**Exit criteria:** The app produces a ranked list of real job listings for a user-defined ideal job.

## Phase 4 — Hardening & Polish

- [ ] Additional sources
- [ ] Configurable embedding providers (swap without code changes)
- [ ] Incremental/deduplicated listing collection
- [ ] Documentation: setup, configuration, usage
- [ ] Packaging/distribution for non-developer use

**Exit criteria:** A user can install, configure, and run the app without touching code.

## Future Considerations

- Scheduled automatic collection
- Multiple ideal job profiles
- Explainability: show which parts of a listing drove the match
- Feedback loop: liked/disliked results refining the ideal job embedding

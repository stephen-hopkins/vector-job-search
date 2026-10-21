# Mission

## Purpose

Vector Job Search exists to eliminate the most tedious part of a job hunt: manually sifting through hundreds of irrelevant listings. The application automatically collects job listings, embeds them, and ranks them against a user-defined "ideal job" description using vector similarity search — surfacing the best matches first.

## Problem Statement

- Job boards are noisy. Relevant roles are buried among poor matches.
- Keyword search and filters miss roles described in unfamiliar terms.
- Job seekers spend hours reading listings that were never a good fit.

## Solution

Let similarity, not keywords, drive discovery:

1. The user describes their ideal job once (title, technologies, culture, salary, remote preferences).
2. That description is converted into a vector embedding.
3. Job listings are collected from configured sources and embedded.
4. Cosine similarity ranks every listing against the ideal job profile.
5. The user reviews a short, ranked list of the best matches with similarity scores.

## Core Values

- **Relevance over volume** — Return a short, ranked list, not a firehose.
- **User-defined matching** — The user's own words define what "ideal" means. No hidden scoring.
- **Transparency** — Similarity scores are visible so users can understand and trust rankings.
- **Simplicity** — A single, focused pipeline: describe → collect → embed → rank.

## Cost Constraints

- **Running costs must be kept as low as possible.** This is a small personal project with no budget for expensive infrastructure.
- Prefer serverless or consumption-based pricing over always-on provisioned services; free/low tiers are the default target.
- Third-party API usage (embedding APIs, job board APIs) should be metered carefully — cache embeddings, avoid re-embedding unchanged listings, and stay within free tiers where available.
- If a design choice meaningfully increases running costs, it needs explicit justification.

## Audience & Scale

- **Expected audience:** A single user, based in the UK. Job listing sources and defaults should reflect this (e.g. UK-focused boards, GBP salary context).
- **Expected scale:** Personal use — thousands of listings, not millions.
- **Design constraint:** Despite the small initial scale, the architecture should be designed so it could potentially scale to 1 million users. Avoid design choices that are dead ends at scale (e.g. in-memory-only storage, single-user assumptions baked into the data model, unbounded in-process work). Simplicity comes first, but growth headroom comes free where it costs nothing up front.

## Success Criteria

- Users can define an ideal job description without technical effort.
- Listings from multiple sources flow into a single ranked view.
- Rankings visibly reflect the user's stated preferences.
- Irrelevant listings are effectively filtered out of the top results.

## Non-Goals

- Automated job applications.
- Resume building or editing.
- Recruiter-side tooling.
- Replacing job boards as a source of listings — the application aggregates them.

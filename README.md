# Vector Job Search

Vector Job Search is an application that automatically finds job listings for you and ranks them against your ideal job using vector similarity search.

Instead of sifting through hundreds of listings, you describe your ideal job once. The application embeds that description, embeds incoming job listings, and uses vector search to compare them — returning a ranked list of the best matches.

## Constitution

The project's constitution lives in the [`specs/`](specs/) folder:

- [`mission.md`](specs/mission.md) — Purpose, values, and non-goals
- [`tech-stack.md`](specs/tech-stack.md) — Stack decisions (made and open)
- [`roadmap.md`](specs/roadmap.md) — Phased development plan

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

> **Note:** This project is in early development. Instructions below will be finalized as the project takes shape.

### Prerequisites

- TBC

### Installation

```bash
git clone <repository-url>
cd vector-job-search
# Installation steps coming soon
```

### Usage

```bash
# Usage instructions coming soon
```

## Configuration

| Setting              | Description                                                             |
| -------------------- | ----------------------------------------------------------------------- |
| `ideal_job`          | Description of your ideal job, used to generate the reference embedding |
| `sources`            | Job listing sources to search (APIs, boards, scrapers)                  |
| `embedding_provider` | Provider and model used for embeddings                                  |
| `top_k`              | Number of ranked results to return                                      |

## License

TBD

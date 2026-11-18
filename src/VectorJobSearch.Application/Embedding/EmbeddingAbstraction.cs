// Pipeline-facing embedding usage surface — spec 002, B1 (AC-1.1).
//
// Pipeline code consumes ONLY the provider-agnostic abstraction from
// Microsoft.Extensions.AI:
//
//     IEmbeddingGenerator<string, Embedding<float>>
//
// and never a concrete provider type (tech-stack constraint: pipeline code never
// calls an embedding API directly). The concrete provider lives in Infrastructure
// and is wired exclusively in the composition root (src/VectorJobSearch.Api/Program.cs).
//
// The batch method GenerateAsync(IReadOnlyList<string>, ...) is the primary path
// (spec scale-headroom constraint: no single-use assumptions); the single-text
// helper GenerateEmbeddingAsync delegates to it with a one-element batch (AC-2.3).

using EmbeddingVector = Microsoft.Extensions.AI.Embedding<float>;

namespace VectorJobSearch.Application.Embedding;

/// <summary>
/// Marker/documentation type for the pipeline-facing embedding usage surface. The
/// seam itself is <c>IEmbeddingGenerator&lt;string, EmbeddingVector&gt;</c>; no concrete
/// provider type is ever exposed from this project (AC-1.1 mechanical scan).
/// </summary>
public static class EmbeddingAbstraction;

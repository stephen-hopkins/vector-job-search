namespace VectorJobSearch.Infrastructure.Embedding;

/// <summary>
/// Startup validation failure for the <c>embedding_provider</c> configuration section
/// (AC-5.2): the message always names the offending setting — and for an unknown
/// provider name, the accepted values. The composition root logs it and exits 1
/// before any network call.
/// </summary>
public sealed class EmbeddingProviderConfigurationException(string message)
    : Exception($"embedding_provider configuration invalid: {message}");
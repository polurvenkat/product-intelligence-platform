using ProductIntelligence.Infrastructure.AI.Models;

namespace ProductIntelligence.Infrastructure.AI;

/// <summary>
/// AI-powered agent for detecting duplicate and similar feature requests using vector similarity and LLM analysis.
/// </summary>
public interface IFeatureDeduplicationAgent
{
    /// <summary>
    /// Analyzes a feature request to find potential duplicates or similar requests.
    /// </summary>
    /// <param name="request">The feature request to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deduplication analysis results with confidence scores and reasoning</returns>
    Task<DeduplicationResult> AnalyzeAsync(
        DeduplicationRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding vector for a feature request without performing analysis.
    /// </summary>
    /// <param name="title">Feature request title</param>
    /// <param name="description">Feature request description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>1536-dimensional embedding vector</returns>
    Task<float[]> GenerateEmbeddingAsync(
        string title, 
        string description, 
        CancellationToken cancellationToken = default);
}

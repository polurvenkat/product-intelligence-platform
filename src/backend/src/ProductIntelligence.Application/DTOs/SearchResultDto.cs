namespace ProductIntelligence.Application.DTOs;

/// <summary>
/// Search result with relevance score.
/// </summary>
/// <typeparam name="T">Type of result item</typeparam>
public record SearchResult<T>
{
    public T Item { get; init; } = default!;
    public double RelevanceScore { get; init; }
    public double? SimilarityScore { get; init; }
}

/// <summary>
/// Feature search result with domain context.
/// </summary>
public record FeatureSearchResult
{
    public Guid Id { get; init; }
    public Guid DomainId { get; init; }
    public string DomainName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public decimal AiPriorityScore { get; init; }
    public double SimilarityScore { get; init; }
    public long VoteCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Feature request search result.
/// </summary>
public record FeatureRequestSearchResult
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string? RequesterCompany { get; init; }
    public string Status { get; init; } = string.Empty;
    public double SimilarityScore { get; init; }
    public DateTime SubmittedAt { get; init; }
}

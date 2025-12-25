using MediatR;
using Microsoft.Extensions.Logging;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Queries.Search;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Application.Interfaces.AI;

namespace ProductIntelligence.Infrastructure.Handlers;

/// <summary>
/// Handler for semantic search on features using AI embeddings.
/// Located in Infrastructure layer to access AI services.
/// </summary>
public class SearchFeaturesQueryHandler : IRequestHandler<SearchFeaturesQuery, IEnumerable<FeatureSearchResult>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IDomainRepository _domainRepository;
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<SearchFeaturesQueryHandler> _logger;

    public SearchFeaturesQueryHandler(
        IFeatureRepository featureRepository,
        IDomainRepository domainRepository,
        IAzureOpenAIService aiService,
        ILogger<SearchFeaturesQueryHandler> logger)
    {
        _featureRepository = featureRepository;
        _domainRepository = domainRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<IEnumerable<FeatureSearchResult>> Handle(SearchFeaturesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchText))
        {
            return Enumerable.Empty<FeatureSearchResult>();
        }

        _logger.LogInformation("Performing semantic search for features: {SearchText}", request.SearchText);

        // Generate embedding for search text
        var searchEmbedding = await _aiService.GenerateEmbeddingAsync(request.SearchText, cancellationToken);

        // Find similar features using vector search
        var similarFeatures = await _featureRepository.FindSimilarAsync(
            searchEmbedding, 
            request.Threshold, 
            request.Limit, 
            cancellationToken);

        if (!similarFeatures.Any())
        {
            _logger.LogInformation("No similar features found for: {SearchText}", request.SearchText);
            return Enumerable.Empty<FeatureSearchResult>();
        }

        // Get domain information for all features
        var domainIds = similarFeatures.Select(f => f.DomainId).Distinct().ToList();
        var domainTasks = domainIds.Select(id => _domainRepository.GetByIdAsync(id, cancellationToken));
        var domains = await Task.WhenAll(domainTasks);
        var domainDict = domains.Where(d => d != null).ToDictionary(d => d!.Id, d => d!.Name);

        // Calculate similarity scores and map to DTOs
        var results = similarFeatures.Select((feature, index) => new FeatureSearchResult
        {
            Id = feature.Id,
            DomainId = feature.DomainId,
            DomainName = domainDict.TryGetValue(feature.DomainId, out var domainName) ? domainName : "Unknown",
            Title = feature.Title,
            Description = feature.Description,
            Status = feature.Status.ToString(),
            Priority = feature.Priority.ToString(),
            AiPriorityScore = feature.AiPriorityScore,
            SimilarityScore = 1.0 - (index * 0.05), // Approximate - actual score from vector distance
            VoteCount = 0, // Would need to query votes separately
            CreatedAt = feature.CreatedAt,
            UpdatedAt = feature.UpdatedAt
        }).ToList();

        _logger.LogInformation("Found {Count} similar features", results.Count);

        return results;
    }
}

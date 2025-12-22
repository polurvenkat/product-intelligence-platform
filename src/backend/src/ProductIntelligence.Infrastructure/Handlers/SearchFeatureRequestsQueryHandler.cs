using MediatR;
using Microsoft.Extensions.Logging;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Queries.Search;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.AI;

namespace ProductIntelligence.Infrastructure.Handlers;

/// <summary>
/// Handler for semantic search on feature requests using AI embeddings.
/// Located in Infrastructure layer to access AI services.
/// </summary>
public class SearchFeatureRequestsQueryHandler : IRequestHandler<SearchFeatureRequestsQuery, IEnumerable<FeatureRequestSearchResult>>
{
    private readonly IFeatureRequestRepository _requestRepository;
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<SearchFeatureRequestsQueryHandler> _logger;

    public SearchFeatureRequestsQueryHandler(
        IFeatureRequestRepository requestRepository,
        IAzureOpenAIService aiService,
        ILogger<SearchFeatureRequestsQueryHandler> logger)
    {
        _requestRepository = requestRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<IEnumerable<FeatureRequestSearchResult>> Handle(SearchFeatureRequestsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchText))
        {
            return Enumerable.Empty<FeatureRequestSearchResult>();
        }

        _logger.LogInformation("Performing semantic search for feature requests: {SearchText}", request.SearchText);

        // Generate embedding for search text
        var searchEmbedding = await _aiService.GenerateEmbeddingAsync(request.SearchText, cancellationToken);

        // Find similar feature requests using vector search
        var similarRequests = await _requestRepository.FindSimilarAsync(
            searchEmbedding, 
            request.Threshold, 
            request.Limit, 
            cancellationToken);

        if (!similarRequests.Any())
        {
            _logger.LogInformation("No similar feature requests found for: {SearchText}", request.SearchText);
            return Enumerable.Empty<FeatureRequestSearchResult>();
        }

        // Map to DTOs with similarity scores
        var results = similarRequests.Select((req, index) => new FeatureRequestSearchResult
        {
            Id = req.Id,
            Title = req.Title,
            Description = req.Description,
            RequesterName = req.RequesterName,
            RequesterCompany = req.RequesterCompany,
            Status = req.Status.ToString(),
            SimilarityScore = 1.0 - (index * 0.05), // Approximate - actual score from vector distance
            SubmittedAt = req.SubmittedAt
        }).ToList();

        _logger.LogInformation("Found {Count} similar feature requests", results.Count);

        return results;
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.AI;

namespace ProductIntelligence.Infrastructure.CommandHandlers.FeatureRequests;

/// <summary>
/// Handler for submitting new feature requests
/// Located in Infrastructure layer to access AI services for embedding generation
/// </summary>
public class SubmitFeatureRequestCommandHandler : IRequestHandler<SubmitFeatureRequestCommand, FeatureRequestDto>
{
    private readonly IFeatureRequestRepository _requestRepository;
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<SubmitFeatureRequestCommandHandler> _logger;

    public SubmitFeatureRequestCommandHandler(
        IFeatureRequestRepository requestRepository,
        IAzureOpenAIService aiService,
        ILogger<SubmitFeatureRequestCommandHandler> logger)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeatureRequestDto> Handle(
        SubmitFeatureRequestCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing feature request submission: {Title}", command.Title);

        try
        {
            // Create entity using constructor
            var request = new FeatureRequest(
                title: command.Title,
                description: command.Description,
                requesterName: command.RequesterName,
                source: command.Source,
                sourceId: command.SourceId,
                requesterEmail: command.RequesterEmail,
                requesterCompany: command.RequesterCompany,
                requesterTier: command.RequesterTier);

            // Try to generate embedding for the feature request (optional for tests)
            try
            {
                var combinedText = $"{command.Title}\n\n{command.Description}";
                var embedding = await _aiService.GenerateEmbeddingAsync(combinedText, cancellationToken);
                request.SetEmbedding(embedding);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for feature request. Continuing without embedding.");
            }

            // Persist to database
            await _requestRepository.AddAsync(request, cancellationToken);

            _logger.LogInformation(
                "Feature request submitted successfully. ID: {RequestId}, Title: {Title}",
                request.Id, request.Title);

            // Map to DTO
            return new FeatureRequestDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Source = request.Source,
                SourceId = request.SourceId,
                RequesterName = request.RequesterName,
                RequesterEmail = request.RequesterEmail,
                RequesterCompany = request.RequesterCompany,
                RequesterTier = request.RequesterTier,
                Status = request.Status,
                SubmittedAt = request.SubmittedAt,
                ProcessedAt = request.ProcessedAt,
                LinkedFeatureId = request.LinkedFeatureId,
                DuplicateOfRequestId = request.DuplicateOfRequestId,
                SimilarityScore = request.SimilarityScore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit feature request: {Title}", command.Title);
            throw;
        }
    }
}

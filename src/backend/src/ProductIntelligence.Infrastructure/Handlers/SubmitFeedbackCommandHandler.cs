using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ProductIntelligence.Application.Feedback.Commands;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.AI;

namespace ProductIntelligence.Infrastructure.Handlers;

/// <summary>
/// Handler for submitting feedback with AI sentiment analysis.
/// </summary>
public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, Guid>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<SubmitFeedbackCommandHandler> _logger;

    public SubmitFeedbackCommandHandler(
        IFeedbackRepository feedbackRepository,
        IAzureOpenAIService aiService,
        ILogger<SubmitFeedbackCommandHandler> logger)
    {
        _feedbackRepository = feedbackRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<Guid> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        // Analyze sentiment using AI
        Sentiment sentiment;
        try
        {
            sentiment = await AnalyzeSentimentAsync(request.Content, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze sentiment for feedback, defaulting to Neutral");
            sentiment = Sentiment.Neutral;
        }

        // Create feedback entity
        var feedback = new Core.Entities.Feedback(
            request.Content,
            request.Source,
            request.FeatureId,
            request.FeatureRequestId,
            request.CustomerId,
            request.CustomerTier,
            sentiment);

        // Generate embedding vector
        try
        {
            var embedding = await _aiService.GenerateEmbeddingAsync(request.Content, cancellationToken);
            feedback.SetEmbedding(embedding);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding for feedback");
        }

        // Save to database
        var feedbackId = await _feedbackRepository.AddAsync(feedback, cancellationToken);

        _logger.LogInformation("Feedback submitted successfully: {FeedbackId} with sentiment {Sentiment}", 
            feedbackId, sentiment);

        return feedbackId;
    }

    private async Task<Sentiment> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken)
    {
        var messages = new ChatMessage[]
        {
            new SystemChatMessage("You are a sentiment analysis expert. Respond with ONLY one word: Positive, Negative, or Neutral."),
            new UserChatMessage($"Analyze the sentiment of the following customer feedback:\n\n{content}\n\nSentiment:")
        };

        var response = await _aiService.CompleteChatAsync(
            messages,
            temperature: 0.1,
            maxTokens: 10,
            cancellationToken: cancellationToken);

        var sentimentText = response.Trim();

        return sentimentText.ToLowerInvariant() switch
        {
            "positive" => Sentiment.Positive,
            "negative" => Sentiment.Negative,
            _ => Sentiment.Neutral
        };
    }
}

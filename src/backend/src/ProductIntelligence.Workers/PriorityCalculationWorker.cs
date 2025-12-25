using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Application.Interfaces.AI;

namespace ProductIntelligence.Workers;

/// <summary>
/// Background worker that periodically recalculates AI priority scores for features.
/// Runs on a schedule to ensure priorities stay up-to-date based on latest data.
/// </summary>
public class PriorityCalculationWorker : BackgroundService
{
    private readonly ILogger<PriorityCalculationWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _calculationInterval = TimeSpan.FromHours(6); // Run every 6 hours
    private readonly int _batchSize = 20;

    public PriorityCalculationWorker(
        ILogger<PriorityCalculationWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Priority Calculation Worker starting at: {Time}", DateTimeOffset.Now);

        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecalculatePrioritiesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating priorities");
            }

            // Wait before next calculation cycle
            try
            {
                await Task.Delay(_calculationInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation("Priority Calculation Worker stopping at: {Time}", DateTimeOffset.Now);
    }

    private async Task RecalculatePrioritiesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var featureRepository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAzureOpenAIService>();

        // Get features that are in progress or accepted (skip shipped/on-hold/rejected)
        var allFeatures = await featureRepository.GetAllAsync(cancellationToken);
        var activeFeatures = allFeatures
            .Where(f => f.Status == FeatureStatus.Accepted || f.Status == FeatureStatus.InProgress)
            .OrderBy(f => f.UpdatedAt) // Process oldest first
            .Take(_batchSize)
            .ToList();

        if (!activeFeatures.Any())
        {
            _logger.LogDebug("No active features to recalculate priorities");
            return;
        }

        _logger.LogInformation("Recalculating priorities for {Count} features", activeFeatures.Count);

        var updatedCount = 0;
        var errorCount = 0;

        foreach (var feature in activeFeatures)
        {
            try
            {
                var result = await CalculateAIPriorityAsync(feature, aiService, cancellationToken);
                var score = result.score;
                var reasoning = result.reasoning;

                // Only update if the score has changed significantly (> 0.1 difference)
                if (Math.Abs(score - feature.AiPriorityScore) > 0.1m)
                {
                    await featureRepository.UpdatePriorityAsync(
                        feature.Id, 
                        score, 
                        reasoning, 
                        cancellationToken);

                    updatedCount++;
                    _logger.LogInformation(
                        "Updated priority for feature {FeatureId}: {Title} - Score: {OldScore:F2} → {NewScore:F2}",
                        feature.Id, feature.Title, feature.AiPriorityScore, score);
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, 
                    "Failed to calculate priority for feature {FeatureId}: {Title}",
                    feature.Id, feature.Title);
            }
        }

        if (updatedCount > 0 || errorCount > 0)
        {
            _logger.LogInformation(
                "Priority calculation completed: {Updated} updated, {Errors} errors",
                updatedCount, errorCount);
        }
    }

    private async Task<(decimal score, string reasoning)> CalculateAIPriorityAsync(
        Core.Entities.Feature feature,
        IAzureOpenAIService aiService,
        CancellationToken cancellationToken)
    {
        var prompt = $@"Analyze this feature and provide a priority score (0.00 to 1.00) based on:
- Business value
- Estimated effort
- Customer impact
- Strategic alignment

Feature Details:
Title: {feature.Title}
Description: {feature.Description}
Current Priority: {feature.Priority}
Estimated Effort: {feature.EstimatedEffortPoints ?? 0} points
Business Value: {feature.BusinessValueScore ?? 0:F2}
Status: {feature.Status}

Respond in JSON format:
{{
  ""score"": 0.75,
  ""reasoning"": ""Brief explanation of the priority score""
}}";

        var messages = new ChatMessage[]
        {
            new SystemChatMessage("You are an expert product manager who calculates feature priorities. Always respond with valid JSON only."),
            new UserChatMessage(prompt)
        };

        var response = await aiService.CompleteChatAsync(
            messages,
            temperature: 0.3,
            maxTokens: 500,
            cancellationToken: cancellationToken);

        // Parse JSON response
        var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<PriorityResponse>(response);
        
        if (jsonResponse == null)
        {
            _logger.LogWarning("Failed to parse AI response for feature {FeatureId}, using default", feature.Id);
            return (0.5m, "Unable to calculate priority at this time");
        }

        // Ensure score is within valid range
        var score = Math.Clamp(jsonResponse.Score, 0m, 1m);
        
        return (score, jsonResponse.Reasoning ?? "Priority calculated based on multiple factors");
    }

    private class PriorityResponse
    {
        public decimal Score { get; set; }
        public string? Reasoning { get; set; }
    }
}

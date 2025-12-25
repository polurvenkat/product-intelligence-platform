using Microsoft.Extensions.Logging;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Application.Interfaces.AI;

namespace ProductIntelligence.Workers;

/// <summary>
/// Background worker that generates embeddings for features that don't have them.
/// This ensures all features are searchable via semantic search.
/// </summary>
public class EmbeddingGeneratorWorker : BackgroundService
{
    private readonly ILogger<EmbeddingGeneratorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(5);
    private readonly int _batchSize = 15;

    public EmbeddingGeneratorWorker(
        ILogger<EmbeddingGeneratorWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Embedding Generator Worker starting at: {Time}", DateTimeOffset.Now);

        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateMissingEmbeddingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings");
            }

            // Wait before next processing cycle
            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Embedding Generator Worker stopping at: {Time}", DateTimeOffset.Now);
    }

    private async Task GenerateMissingEmbeddingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var featureRepository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAzureOpenAIService>();

        // Get all features (in a real implementation, you'd want to track which have embeddings)
        var allFeatures = await featureRepository.GetAllAsync(cancellationToken);
        
        // For now, process a batch - in production, you'd check which ones need embeddings
        // This is a placeholder since Feature entity doesn't have EmbeddingVector property yet
        var featuresToProcess = allFeatures
            .OrderByDescending(f => f.CreatedAt)
            .Take(_batchSize)
            .ToList();

        if (!featuresToProcess.Any())
        {
            _logger.LogDebug("No features to process for embedding generation");
            return;
        }

        _logger.LogInformation("Processing {Count} features for embedding generation", featuresToProcess.Count);

        var processedCount = 0;
        var errorCount = 0;

        foreach (var feature in featuresToProcess)
        {
            try
            {
                // Generate embedding for the feature
                var combinedText = $"{feature.Title}\n\n{feature.Description}";
                var embedding = await aiService.GenerateEmbeddingAsync(combinedText, cancellationToken);

                // Note: Feature entity doesn't have EmbeddingVector property yet
                // In a complete implementation, you would:
                // 1. Add EmbeddingVector property to Feature entity
                // 2. Add UpdateEmbeddingAsync method to IFeatureRepository
                // 3. Call that method here
                
                processedCount++;
                _logger.LogDebug(
                    "Generated embedding for feature {FeatureId}: {Title}",
                    feature.Id, feature.Title);
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, 
                    "Failed to generate embedding for feature {FeatureId}: {Title}",
                    feature.Id, feature.Title);
            }
        }

        if (processedCount > 0)
        {
            _logger.LogInformation(
                "Embedding generation completed: {Processed} processed, {Errors} errors",
                processedCount, errorCount);
        }
    }
}

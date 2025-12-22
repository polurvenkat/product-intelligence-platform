using Microsoft.Extensions.Logging;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.AI;

namespace ProductIntelligence.Workers;

/// <summary>
/// Background worker that processes feature requests to generate embeddings.
/// Runs continuously to process requests that don't have embeddings yet.
/// </summary>
public class FeatureRequestProcessorWorker : BackgroundService
{
    private readonly ILogger<FeatureRequestProcessorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30);
    private readonly int _batchSize = 10;

    public FeatureRequestProcessorWorker(
        ILogger<FeatureRequestProcessorWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Feature Request Processor Worker starting at: {Time}", DateTimeOffset.Now);

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRequestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing feature requests");
            }

            // Wait before next processing cycle
            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Feature Request Processor Worker stopping at: {Time}", DateTimeOffset.Now);
    }

    private async Task ProcessPendingRequestsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var requestRepository = scope.ServiceProvider.GetRequiredService<IFeatureRequestRepository>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAzureOpenAIService>();

        // Get pending requests that need processing (those without embeddings)
        var allPendingRequests = await requestRepository.GetPendingAsync(cancellationToken);
        var requestsWithoutEmbeddings = allPendingRequests
            .Where(r => r.EmbeddingVector == null || r.EmbeddingVector.Length == 0)
            .Take(_batchSize)
            .ToList();

        if (!requestsWithoutEmbeddings.Any())
        {
            _logger.LogDebug("No feature requests pending embedding generation");
            return;
        }

        _logger.LogInformation("Processing {Count} feature requests for embedding generation", requestsWithoutEmbeddings.Count);

        var processedCount = 0;
        var errorCount = 0;

        foreach (var request in requestsWithoutEmbeddings)
        {
            try
            {
                // Generate embedding for the request
                var combinedText = $"{request.Title}\n\n{request.Description}";
                var embedding = await aiService.GenerateEmbeddingAsync(combinedText, cancellationToken);

                // Update the request with the embedding
                await requestRepository.UpdateEmbeddingAsync(request.Id, embedding, cancellationToken);

                processedCount++;
                _logger.LogInformation(
                    "Generated embedding for feature request {RequestId}: {Title}",
                    request.Id, request.Title);
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, 
                    "Failed to generate embedding for feature request {RequestId}: {Title}",
                    request.Id, request.Title);
            }
        }

        if (processedCount > 0 || errorCount > 0)
        {
            _logger.LogInformation(
                "Completed batch: {Processed} processed, {Errors} errors",
                processedCount, errorCount);
        }
    }
}

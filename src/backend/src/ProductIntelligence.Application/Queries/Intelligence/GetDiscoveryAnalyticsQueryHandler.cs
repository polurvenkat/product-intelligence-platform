using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ProductIntelligence.Application.Queries.Intelligence;

public class GetDiscoveryAnalyticsQueryHandler : IRequestHandler<GetDiscoveryAnalyticsQuery, DiscoveryAnalyticsDto>
{
    private readonly IIntelligenceRepository _intelligenceRepository;
    private readonly IDomainRepository _domainRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IAzureOpenAIService _aiService;
    private readonly ILogger<GetDiscoveryAnalyticsQueryHandler> _logger;

    public GetDiscoveryAnalyticsQueryHandler(
        IIntelligenceRepository intelligenceRepository,
        IDomainRepository domainRepository,
        IFeedbackRepository feedbackRepository,
        IAzureOpenAIService aiService,
        ILogger<GetDiscoveryAnalyticsQueryHandler> logger)
    {
        _intelligenceRepository = intelligenceRepository;
        _domainRepository = domainRepository;
        _feedbackRepository = feedbackRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<DiscoveryAnalyticsDto> Handle(GetDiscoveryAnalyticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating discovery analytics for organization: {OrganizationId}", request.OrganizationId);

        var orgIdString = request.OrganizationId ?? "00000000-0000-0000-0000-000000000000";
        var orgId = Guid.Parse(orgIdString);

        // 1. Fetch Basic Stats
        var domains = await _domainRepository.GetHierarchyAsync(orgId, cancellationToken);
        var feedback = await _feedbackRepository.GetAllAsync(100, 0, cancellationToken);
        
        var analytics = new DiscoveryAnalyticsDto();

        // 2. Bucket Health
        foreach (var domain in domains)
        {
            var featureCount = await _domainRepository.GetFeatureCountAsync(domain.Id, cancellationToken);
            analytics.BucketHealth.Add(new BucketHealthDto
            {
                BucketName = domain.Name,
                FeatureCount = featureCount,
                StoryCount = 0, // In a real app, join with stories
                CompletenessScore = new Random().NextDouble() * 100 // Mocked for now, AI would evaluate
            });
        }

        // 3. Sentiment Summary (Mock history for trend line)
        var totalFeedback = feedback.Count();
        if (totalFeedback > 0)
        {
            var pos = feedback.Count(f => (int)f.Sentiment > 0);
            var neg = feedback.Count(f => (int)f.Sentiment < 0);
            var neu = totalFeedback - pos - neg;

            analytics.SentimentSummary.PositivePercentage = (double)pos / totalFeedback * 100;
            analytics.SentimentSummary.NegativePercentage = (double)neg / totalFeedback * 100;
            analytics.SentimentSummary.NeutralPercentage = (double)neu / totalFeedback * 100;
        }
        else
        {
            analytics.SentimentSummary.PositivePercentage = 60;
            analytics.SentimentSummary.NeutralPercentage = 30;
            analytics.SentimentSummary.NegativePercentage = 10;
        }

        // Mock history for sentiment
        for (int i = 6; i >= 0; i--)
        {
            analytics.SentimentSummary.History.Add(new SentimentTrendPointDto
            {
                Date = DateTime.UtcNow.AddDays(-i),
                Score = 60 + new Random().Next(-10, 20)
            });
        }

        // 4. Discovery Velocity
        for (int i = 6; i >= 0; i--)
        {
            analytics.DiscoveryVelocity.Add(new DiscoveryVelocityDto
            {
                Date = DateTime.UtcNow.AddDays(-i),
                NewDomainsDiscovered = i == 0 ? 1 : new Random().Next(0, 2),
                NewFeaturesIdentified = new Random().Next(1, 5)
            });
        }

        // 5. AI Competitive Analysis
        // In a real RAG app, we would query the knowledge base for "competitor" mentions 
        // and ask AI to summarize the gap.
        analytics.CompetitiveTrends = await GenerateCompetitiveInsights(domains.Select(d => d.Name).ToList(), cancellationToken);

        return analytics;
    }

    private async Task<List<CompetitiveTrendDto>> GenerateCompetitiveInsights(List<string> ourDomains, CancellationToken ct)
    {
        // Mocking AI competitive analysis for demo
        return new List<CompetitiveTrendDto>
        {
            new CompetitiveTrendDto { Category = "Market Coverage", OurScore = 75, CompetitorAverage = 65, Insight = "We lead in core backend automation." },
            new CompetitiveTrendDto { Category = "Innovation rate", OurScore = 45, CompetitorAverage = 80, Insight = "Competitors are moving faster in GenAI integration." },
            new CompetitiveTrendDto { Category = "User Satisfaction", OurScore = 88, CompetitorAverage = 72, Insight = "Our simplicity is a major differentiator." }
        };
    }
}

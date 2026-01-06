using System;
using System.Collections.Generic;

namespace ProductIntelligence.Application.DTOs;

public class DiscoveryAnalyticsDto
{
    public List<BucketHealthDto> BucketHealth { get; set; } = new();
    public List<CompetitiveTrendDto> CompetitiveTrends { get; set; } = new();
    public SentimentSummaryDto SentimentSummary { get; set; } = new();
    public List<DiscoveryVelocityDto> DiscoveryVelocity { get; set; } = new();
}

public class BucketHealthDto
{
    public string BucketName { get; set; } = string.Empty;
    public int FeatureCount { get; set; }
    public int StoryCount { get; set; }
    public double CompletenessScore { get; set; } // 0-1 based on AI assessment of requirements
}

public class CompetitiveTrendDto
{
    public string Category { get; set; } = string.Empty;
    public double OurScore { get; set; }
    public double CompetitorAverage { get; set; }
    public string Insight { get; set; } = string.Empty;
}

public class SentimentSummaryDto
{
    public double PositivePercentage { get; set; }
    public double NeutralPercentage { get; set; }
    public double NegativePercentage { get; set; }
    public List<SentimentTrendPointDto> History { get; set; } = new();
}

public class SentimentTrendPointDto
{
    public DateTime Date { get; set; }
    public double Score { get; set; }
}

public class DiscoveryVelocityDto
{
    public DateTime Date { get; set; }
    public int NewDomainsDiscovered { get; set; }
    public int NewFeaturesIdentified { get; set; }
}

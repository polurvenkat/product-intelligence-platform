using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Queries.Search;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.Search;

public class FilterFeaturesQueryHandler : IRequestHandler<FilterFeaturesQuery, IEnumerable<FeatureDto>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IDomainRepository _domainRepository;

    public FilterFeaturesQueryHandler(
        IFeatureRepository featureRepository,
        IDomainRepository domainRepository)
    {
        _featureRepository = featureRepository;
        _domainRepository = domainRepository;
    }

    public async Task<IEnumerable<FeatureDto>> Handle(FilterFeaturesQuery request, CancellationToken cancellationToken)
    {
        // Start with all features (or by domain if specified)
        IEnumerable<Core.Entities.Feature> features;
        
        if (request.DomainId.HasValue)
        {
            features = await _featureRepository.GetByDomainIdAsync(request.DomainId.Value, cancellationToken);
        }
        else
        {
            features = await _featureRepository.GetAllAsync(cancellationToken);
        }

        // Apply filters
        if (request.Status.HasValue)
        {
            features = features.Where(f => f.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            features = features.Where(f => f.Priority == request.Priority.Value);
        }

        if (request.CreatedAfter.HasValue)
        {
            features = features.Where(f => f.CreatedAt >= request.CreatedAfter.Value);
        }

        if (request.CreatedBefore.HasValue)
        {
            features = features.Where(f => f.CreatedAt <= request.CreatedBefore.Value);
        }

        if (request.MinPriorityScore.HasValue)
        {
            features = features.Where(f => f.AiPriorityScore >= request.MinPriorityScore.Value);
        }

        if (request.MaxPriorityScore.HasValue)
        {
            features = features.Where(f => f.AiPriorityScore <= request.MaxPriorityScore.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetRelease))
        {
            features = features.Where(f => f.TargetRelease == request.TargetRelease);
        }

        // Apply pagination
        var pagedFeatures = features
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToList();

        // Get domain names for all features
        var domainIds = pagedFeatures.Select(f => f.DomainId).Distinct().ToList();
        var domainTasks = domainIds.Select(id => _domainRepository.GetByIdAsync(id, cancellationToken));
        var domains = await Task.WhenAll(domainTasks);
        var domainDict = domains.Where(d => d != null).ToDictionary(d => d!.Id, d => d!.Name);

        // Map to DTOs
        return pagedFeatures.Select(f => new FeatureDto
        {
            Id = f.Id,
            DomainId = f.DomainId,
            DomainName = domainDict.TryGetValue(f.DomainId, out var domainName) ? domainName : "Unknown",
            ParentFeatureId = f.ParentFeatureId,
            Title = f.Title,
            Description = f.Description,
            Status = f.Status,
            Priority = f.Priority,
            AiPriorityScore = f.AiPriorityScore,
            AiPriorityReasoning = f.AiPriorityReasoning,
            EstimatedEffortPoints = f.EstimatedEffortPoints,
            BusinessValueScore = f.BusinessValueScore,
            CreatedBy = f.CreatedBy,
            TargetRelease = f.TargetRelease,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        });
    }
}

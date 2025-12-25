using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.Features;

public class GetAllFeaturesQueryHandler : IRequestHandler<GetAllFeaturesQuery, IEnumerable<FeatureDto>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IDomainRepository _domainRepository;

    public GetAllFeaturesQueryHandler(
        IFeatureRepository featureRepository,
        IDomainRepository domainRepository)
    {
        _featureRepository = featureRepository ?? throw new ArgumentNullException(nameof(featureRepository));
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<IEnumerable<FeatureDto>> Handle(GetAllFeaturesQuery request, CancellationToken cancellationToken)
    {
        var features = await _featureRepository.GetAllAsync(cancellationToken);
        
        // Get unique domain IDs and load domains
        var domainIds = features.Select(f => f.DomainId).Distinct().ToList();
        var domainTasks = domainIds.Select(id => _domainRepository.GetByIdAsync(id, cancellationToken));
        var domains = await Task.WhenAll(domainTasks);
        var domainDict = domains.Where(d => d != null).ToDictionary(d => d!.Id, d => d!.Name);

        return features.Select(feature => new FeatureDto
        {
            Id = feature.Id,
            DomainId = feature.DomainId,
            DomainName = domainDict.GetValueOrDefault(feature.DomainId, "Unknown"),
            ParentFeatureId = feature.ParentFeatureId,
            Title = feature.Title,
            Description = feature.Description,
            Status = feature.Status,
            Priority = feature.Priority,
            AiPriorityScore = feature.AiPriorityScore,
            AiPriorityReasoning = feature.AiPriorityReasoning,
            EstimatedEffortPoints = feature.EstimatedEffortPoints,
            BusinessValueScore = feature.BusinessValueScore,
            CreatedBy = feature.CreatedBy,
            TargetRelease = feature.TargetRelease,
            CreatedAt = feature.CreatedAt,
            UpdatedAt = feature.UpdatedAt
        }).ToList();
    }
}

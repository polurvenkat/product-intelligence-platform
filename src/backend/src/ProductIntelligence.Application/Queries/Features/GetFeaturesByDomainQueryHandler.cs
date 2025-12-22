using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.Features;

public class GetFeaturesByDomainQueryHandler : IRequestHandler<GetFeaturesByDomainQuery, IEnumerable<FeatureDto>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IDomainRepository _domainRepository;

    public GetFeaturesByDomainQueryHandler(
        IFeatureRepository featureRepository,
        IDomainRepository domainRepository)
    {
        _featureRepository = featureRepository ?? throw new ArgumentNullException(nameof(featureRepository));
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<IEnumerable<FeatureDto>> Handle(GetFeaturesByDomainQuery request, CancellationToken cancellationToken)
    {
        var features = await _featureRepository.GetByDomainIdAsync(request.DomainId, cancellationToken);
        var domain = await _domainRepository.GetByIdAsync(request.DomainId, cancellationToken);
        var domainName = domain?.Name ?? string.Empty;

        return features.Select(feature => new FeatureDto
        {
            Id = feature.Id,
            DomainId = feature.DomainId,
            DomainName = domainName,
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

using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.Features;

public class UpdateFeatureStatusCommandHandler : IRequestHandler<UpdateFeatureStatusCommand, FeatureDto>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IDomainRepository _domainRepository;

    public UpdateFeatureStatusCommandHandler(
        IFeatureRepository featureRepository,
        IDomainRepository domainRepository)
    {
        _featureRepository = featureRepository ?? throw new ArgumentNullException(nameof(featureRepository));
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<FeatureDto> Handle(UpdateFeatureStatusCommand request, CancellationToken cancellationToken)
    {
        var feature = await _featureRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (feature == null)
        {
            throw new KeyNotFoundException($"Feature with ID {request.Id} not found");
        }

        feature.UpdateStatus(request.Status);
        await _featureRepository.UpdateAsync(feature, cancellationToken);

        var domain = await _domainRepository.GetByIdAsync(feature.DomainId, cancellationToken);

        return new FeatureDto
        {
            Id = feature.Id,
            DomainId = feature.DomainId,
            DomainName = domain?.Name ?? string.Empty,
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
        };
    }
}

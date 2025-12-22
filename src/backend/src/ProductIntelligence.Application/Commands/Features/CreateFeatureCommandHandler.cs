using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.Features;

public class CreateFeatureCommandHandler : IRequestHandler<CreateFeatureCommand, FeatureDto>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IDomainRepository _domainRepository;

    public CreateFeatureCommandHandler(
        IFeatureRepository featureRepository,
        IDomainRepository domainRepository)
    {
        _featureRepository = featureRepository ?? throw new ArgumentNullException(nameof(featureRepository));
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<FeatureDto> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        // Validate domain exists
        var domain = await _domainRepository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domain == null)
        {
            throw new KeyNotFoundException($"Domain with ID {request.DomainId} not found");
        }

        // Validate parent feature if specified
        if (request.ParentFeatureId.HasValue)
        {
            var parentFeature = await _featureRepository.GetByIdAsync(request.ParentFeatureId.Value, cancellationToken);
            if (parentFeature == null)
            {
                throw new KeyNotFoundException($"Parent feature with ID {request.ParentFeatureId} not found");
            }
        }

        var feature = new Feature(
            domainId: request.DomainId,
            title: request.Title,
            description: request.Description,
            createdBy: request.CreatedBy,
            parentFeatureId: request.ParentFeatureId,
            priority: request.Priority);

        // Set additional properties using entity methods
        if (request.EstimatedEffortPoints.HasValue || request.BusinessValueScore.HasValue)
        {
            feature.UpdateDetails(
                request.Title,
                request.Description,
                request.EstimatedEffortPoints,
                request.BusinessValueScore);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetRelease))
        {
            feature.SetTargetRelease(request.TargetRelease);
        }

        await _featureRepository.AddAsync(feature, cancellationToken);

        return new FeatureDto
        {
            Id = feature.Id,
            DomainId = feature.DomainId,
            DomainName = domain.Name,
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

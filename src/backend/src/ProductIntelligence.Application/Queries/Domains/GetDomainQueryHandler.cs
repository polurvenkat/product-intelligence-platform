using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.Domains;

/// <summary>
/// Handler for retrieving a single domain by ID
/// </summary>
public class GetDomainQueryHandler : IRequestHandler<GetDomainQuery, DomainDto?>
{
    private readonly IDomainRepository _domainRepository;

    public GetDomainQueryHandler(IDomainRepository domainRepository)
    {
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<DomainDto?> Handle(GetDomainQuery request, CancellationToken cancellationToken)
    {
        var domain = await _domainRepository.GetByIdAsync(request.Id, cancellationToken);

        if (domain == null)
        {
            return null;
        }

        // Get additional metadata
        var featureCount = await _domainRepository.GetFeatureCountAsync(domain.Id, cancellationToken);
        var hasChildren = await _domainRepository.HasChildrenAsync(domain.Id, cancellationToken);

        return new DomainDto
        {
            Id = domain.Id,
            OrganizationId = domain.OrganizationId,
            Name = domain.Name,
            Description = domain.Description,
            Path = domain.Path,
            ParentId = domain.ParentDomainId,
            FeatureCount = featureCount,
            HasChildren = hasChildren,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }
}
